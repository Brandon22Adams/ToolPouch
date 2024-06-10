using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SpaceCore.UI;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System.Reflection.Metadata.Ecma335;

namespace ToolPouch
{
    internal class PouchMenu : IClickableMenu
    {
        private class SlotUserData
        {
            public Func<Item, bool> Filter;
            public bool IsMainInventory { get; set; } = true;
            public int Slot { get; set; }
        }

        private Pouch pouch;

        private RootElement ui;
        private InventoryMenu invMenu;

        private ItemSlot slotClicked = null;

        private List<ItemSlot> slots = new();

        public PouchMenu(Pouch pouch)
        : base(Game1.uiViewport.Width / 2 - 64 * 6 - IClickableMenu.borderWidth, Game1.uiViewport.Height / 2 - (64 * (pouch.Inventory.Count / 9 + 3)) / 2 - IClickableMenu.borderWidth, 64 * 12, 72 * (pouch.Inventory.Count / 9 + 3) + IClickableMenu.borderWidth * 2)
        {
            this.pouch = pouch;
            pouch.isOpen.Value = true;
            var data = PouchDataDefinition.GetSpecificData(pouch.ItemId);

            invMenu = new(Game1.uiViewport.Width / 2 - 72 * 6 + 8, yPositionOnScreen + height - 64 * 3 - 24, true);

            for (int ii = 0, ic = 0; ii < Game1.player.Items.Count; ++ii, ++ic)
            {
                if (Game1.player.Items[ii] is Pouch s && s.isOpen.Value)
                {
                    // TODO: Gamepad support
                    invMenu.inventory[ic].visible = false;
                }
            }

            ui = new();
            StaticContainer container = new()
            {
                LocalPosition = new(xPositionOnScreen, yPositionOnScreen),
                Size = new(width, height),
            };
            ui.AddChild(container);

            for (int iy = 0; iy < pouch.Inventory.Count / 9; ++iy)
            {
                for (int ix = 0; ix < 9; ++ix)
                {
                    int i = ix + iy * 9;
                    if (i >= pouch.Inventory.Count) continue;

                    var slot = new ItemSlot()
                    {
                        LocalPosition = new(ix * 64 + (width - 64 * 9) / 2, iy * 64 + IClickableMenu.borderWidth),
                        Item = pouch.Inventory[i],
                        BoxIsThin = true,
                    };
                    slot.Callback = (elem) =>
                    {
                        //slotClicked = (elem as ItemSlot);
                    };
                    slot.SecondaryCallback = slot.Callback;
                    slot.UserData = new SlotUserData() { Slot = i, Filter = (item) =>
                    {
                        if(item is Pouch)
                        {
                            Game1.addHUDMessage(new HUDMessage(I18n.Error_Pouch(), HUDMessage.error_type));
                            return false;
                        }
                        return true;
                    }
                    };
                    container.AddChild(slot);
                    slots.Add(slot);
                }
            }

            for (int i = 0; i < pouch.Upgrades.Count; ++i)
            {
                int ix = -4;
                int iy = i * 88 + IClickableMenu.borderWidth / 2;

                var slot = new ItemSlot()
                {
                    LocalPosition = new(ix, iy),
                    Item = pouch.Upgrades[i],
                };
                slot.Callback = (elem) =>
                {
                    slotClicked = (elem as ItemSlot);
                };
                slot.SecondaryCallback = (elem) =>
                {
                    slotClicked = (elem as ItemSlot);

                    var theMenu = Game1.activeClickableMenu;
                    while (theMenu.GetChildMenu() != null)
                    {
                        theMenu = theMenu.GetChildMenu();
                    }
                    // TODO upgrades?
                    //theMenu.SetChildMenu(ModEntry.GetPouchUpgradeMenu(pouch, (elem as ItemSlot).Item));
                };
                slot.UserData = new SlotUserData()
                {
                    IsMainInventory = false,
                    Slot = i,
                    // TODO upgrades?
                    Filter = (item) =>
                    {
                        return item == null;
                    }
                };
                container.AddChild(slot);
                slots.Add(slot);
            }

        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            foreach (var item in invMenu.inventory) // From inventory to Pouch
            {
                if (!item.containsPoint(x, y))
                    continue;
                int slotNum = Convert.ToInt32(item.name);
                if (invMenu.actualInventory.Count <= slotNum)
                    continue;
                if (invMenu.actualInventory[slotNum] == null)
                    break;
                if (invMenu.actualInventory[slotNum] is Pouch)
                    break;

                invMenu.actualInventory[slotNum] = pouch.Inventory.DepositItem(invMenu.actualInventory[slotNum]);
                Game1.playSound("dwop");
                break;
            }

            if (ItemWithBorder.HoveredElement is ItemSlot slot) // From Pouch to Inventory
            {
                if (!(slot.UserData as SlotUserData).Filter(Game1.player.CursorSlotItem))
                {
                    return;
                }
                slotClicked = slot;

                if (slot.Item != null && Game1.player.CursorSlotItem == null)
                {
                    Game1.playSound("stoneStep");
                    slot.Item = Game1.player.Items.DepositItem(slot.Item);
                }
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            base.receiveRightClick(x, y, playSound);

            foreach (var item in invMenu.inventory) // From Inventory to Pouch
            {
                if (!item.containsPoint(x, y))
                    continue;
                int slotNum = Convert.ToInt32(item.name);
                if (invMenu.actualInventory.Count <= slotNum)
                    continue;
                if (invMenu.actualInventory[slotNum] == null)
                    break;

                if (invMenu.actualInventory[slotNum].Stack > 1 && pouch.Inventory.Last() == null) // Stack of items
                {
                    Item grabHalf = invMenu.actualInventory[slotNum].getOne();
                    if (Keyboard.GetState().IsKeyDown(Keys.LeftShift)) // Move half
                    {
                        grabHalf.Stack = invMenu.actualInventory[slotNum].Stack / 2;
                        pouch.Inventory.DepositItem(grabHalf);
                        invMenu.actualInventory[slotNum].Stack = (int)Math.Ceiling(invMenu.actualInventory[slotNum].Stack / 2f);
                    } else // Move 1
                    {
                        pouch.Inventory.DepositItem(grabHalf);
                        invMenu.actualInventory[slotNum].Stack = invMenu.actualInventory[slotNum].Stack - 1;
                    }
                }
                else // Only 1 item
                {
                    invMenu.actualInventory[slotNum] = pouch.Inventory.DepositItem(invMenu.actualInventory[slotNum]);
                }
                Game1.playSound("dwop");
                break;
            }

            if (ItemWithBorder.HoveredElement is ItemSlot slot) // From Pouch to Inventory
            {
                if (!(slot.UserData as SlotUserData).Filter(Game1.player.CursorSlotItem))
                {
                    return;
                }
                slotClicked = slot;

                if (slot.Item != null && Game1.player.CursorSlotItem == null && Game1.player.couldInventoryAcceptThisItem(slot.Item))
                {
                    Game1.playSound("stoneStep");

                    Item grabHalf = slot.Item.getOne();
                    if (Keyboard.GetState().IsKeyDown(Keys.LeftShift))
                    {
                        grabHalf.Stack = slot.Item.Stack / 2;
                        Game1.player.addItemToInventory(grabHalf);

                        for (int i = 0; i < pouch.Inventory.Count; i++)
                        {
                            Item item2 = pouch.Inventory[i];
                            if (grabHalf.canStackWith(item2))
                            {
                                if (item2.Stack <= 1)
                                {
                                    slot.Item = Game1.player.Items.DepositItem(slot.Item);
                                }
                                else
                                {
                                    grabHalf.Stack = slot.Item.Stack / 2;
                                    item2.Stack = item2.Stack - grabHalf.Stack;
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < pouch.Inventory.Count; i++)
                        {
                            Item item2 = pouch.Inventory[i];
                            if (grabHalf.canStackWith(item2))
                            {
                                if(item2.Stack <= 1)
                                {
                                    slot.Item = Game1.player.Items.DepositItem(slot.Item);
                                } 
                                else
                                {
                                    Game1.player.addItemToInventory(grabHalf);
                                    item2.Stack = item2.Stack - 1;
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void update(GameTime time)
        {
            base.update(time);

            if (slotClicked != null)
            {
                var data = (slotClicked.UserData as SlotUserData);
                if (data.IsMainInventory)
                    pouch.Inventory[data.Slot] = slotClicked.Item;
                else
                    pouch.Upgrades[data.Slot] = slotClicked.Item;
                slotClicked = null;
            }

            foreach (var slot in slots)
            {
                var data = (slot.UserData as SlotUserData);
                if (data.IsMainInventory)
                    slot.Item = pouch.Inventory[data.Slot];
                else
                    slot.Item = pouch.Upgrades[data.Slot];
            }

            ui.Update();

            invMenu.update(time);
        }

        public override void draw(SpriteBatch b)
        {
            IClickableMenu.drawTextureBox(b, xPositionOnScreen - IClickableMenu.borderWidth / 2, yPositionOnScreen, width + IClickableMenu.borderWidth, height, Color.White);

            ui.Draw(b);
            invMenu.draw(b);

            Game1.player.CursorSlotItem?.drawInMenu(b, Game1.getMousePosition().ToVector2(), 1);

            if (ItemWithBorder.HoveredElement != null)
            {
                if (ItemWithBorder.HoveredElement is ItemSlot slot && slot.Item != null)
                {
                    drawToolTip(b, slot.Item.getDescription(), slot.Item.DisplayName, slot.Item);
                }
                else if (ItemWithBorder.HoveredElement.ItemDisplay != null)
                {
                    drawToolTip(b, ItemWithBorder.HoveredElement.ItemDisplay.getDescription(), ItemWithBorder.HoveredElement.ItemDisplay.DisplayName, ItemWithBorder.HoveredElement.ItemDisplay);
                }
            }
            else
            {
                var hover = invMenu.hover(Game1.getMouseX(), Game1.getMouseY(), null);
                if (hover != null)
                {
                    drawToolTip(b, invMenu.hoverText, invMenu.hoverTitle, hover);
                }
            }

            drawMouse(b);
        }

        protected override void cleanupBeforeExit()
        {
            pouch.Inventory.Sort();
            base.cleanupBeforeExit();
            pouch.isOpen.Value = false;
        }

        public override void emergencyShutDown()
        {
            pouch.Inventory.Sort();
            base.emergencyShutDown();
            pouch.isOpen.Value = false;
        }
    }
}
