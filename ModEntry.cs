using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using ToolPouch.api;
using SpaceShared.APIs;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Shops;
using StardewValley.Menus;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Inventories;


namespace ToolPouch
{
    public class ModEntry : Mod
    {
        public static ModEntry instance;

        private ToolPouchMenu toolPouchMenu;

        ModConfig Config;


        private static Pouch toOpen;
        public static void QueueOpeningPouch(Pouch pouch)
        {
            if (!pouch.isOpen.Value)
                toOpen = pouch;
        }


        public override void Entry(IModHelper helper)
        {
            instance = this;
            I18n.Init(Helper.Translation);

            Helper.Events.GameLoop.UpdateTicked += onUpdate;
            Helper.Events.Content.AssetRequested += assetRequested;
            Helper.Events.Input.ButtonPressed += OnButtonPressed;
            Helper.Events.Input.ButtonReleased += OnButtonReleased;
            //support for generic mod menu
            Helper.Events.GameLoop.GameLaunched += onLaunched;
            Helper.Events.GameLoop.SaveLoaded += reinit;

            Config = Helper.ReadConfig<ModConfig>();
            toolPouchMenu = new ToolPouchMenu(Helper, this, Config, Monitor);

            var def = new PouchDataDefinition();
            ItemRegistry.ItemTypes.Add(def);
            Helper.Reflection.GetField<Dictionary<string, IItemDataDefinition>>(typeof(ItemRegistry), "IdentifierLookup").GetValue()[def.Identifier] = def;
        }

        private void onUpdate(object sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
        {
            if (toOpen != null)
            {
                var menu = new PouchMenu(toOpen);

                if (Game1.activeClickableMenu == null)
                    Game1.activeClickableMenu = menu;
                else
                {
                    var theMenu = Game1.activeClickableMenu;
                    while (theMenu.GetChildMenu() != null)
                    {
                        theMenu = theMenu.GetChildMenu();
                    }
                    theMenu.SetChildMenu(menu);
                }

                toOpen = null;
            }
        }

        private void reinit(object sender, SaveLoadedEventArgs e)
        {
            Config = Helper.ReadConfig<ModConfig>();
            toolPouchMenu = new ToolPouchMenu(Helper, this, Config, Monitor);
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if(e.Button == SButton.MouseRight) // Open pouch or deposit in pouch
            {
                if (Game1.activeClickableMenu is ShopMenu shop)
                {
                    foreach (var slot in shop.inventory.inventory)
                    {
                        if (slot.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
                        {
                            int i = shop.inventory.inventory.IndexOf(slot);
                            if (shop.inventory.actualInventory[i] is Pouch pouch)
                            {
                                if (shop.heldItem is Item item)
                                {
                                    shop.heldItem = pouch.quickDeposit(item);
                                }
                                else
                                {
                                    Game1.activeClickableMenu.SetChildMenu(new PouchMenu(pouch));
                                }
                                Helper.Input.Suppress(e.Button);
                                break;
                            }
                        }
                    }
                }
            }

            if (canOpenMenu(e.Button))
            {
                if (Game1.activeClickableMenu != null)
                {
                    if (Game1.activeClickableMenu == toolPouchMenu)
                    {
                        Monitor.Log("closing menu", LogLevel.Trace);
                        toolPouchMenu.closeAndReturnSelected();
                        return;
                    }
                    return;
                }

                //open ToolPouch menu
                Farmer farmer = Game1.player;
                List<Item> inventory = new List<Item>();
                for (int i = 0; i < Game1.player.Items.Count; ++i)
                {
                    if (Game1.player.Items[i] is Pouch pouch)
                    {
                        inventory.AddRange(pouch.Inventory);
                        if(inventory.Any(i => i != null))
                        {
                            Monitor.Log($"{farmer.Name} opening ToolPouch menu with {e.Button}.", LogLevel.Trace);
                            toolPouchMenu.updateToolList(getToolMap(inventory));
                            Game1.activeClickableMenu = toolPouchMenu;
                        }
                    }
                }
            }
        }

        private void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!(canOpenMenu(e.Button) && Game1.activeClickableMenu == toolPouchMenu)) return;
            //placeholder for hold config
            if (Config.HoverSelects && e.Button != SButton.LeftStick) swapItem();
        }

        public void swapItem()
        {
            int swapIndex = toolPouchMenu.closeAndReturnSelected();
            Monitor.Log($"selected index is {swapIndex}", LogLevel.Trace);
            if (swapIndex == -1) return;
            swapItem(swapIndex);
        }

        public void swapItem(int swapIndex)
        {
            Farmer farmer = Game1.player;
            List<Item> newInventory = new List<Item>();
            int currentIndex = farmer.CurrentToolIndex;
            if (swapIndex == -1) return;
            newInventory.AddRange(farmer.Items);

            if(newInventory[currentIndex] != null && newInventory[currentIndex].ItemId == "Pouch")
            {
                Game1.showRedMessage("Pouch can't be placed inside the Pouch", true);
                return;
            }

            Pouch newPouch = null;
            List<Item> pouchInventory = new List<Item>();
            for (int i = 0; i < Game1.player.Items.Count; ++i)
            {
                if (Game1.player.Items[i] is Pouch pouch)
                {
                    pouchInventory.AddRange(pouch.Inventory);
                    newPouch = pouch;
                }
            }

            Item temp = newInventory[currentIndex];
            newInventory[currentIndex] = pouchInventory[swapIndex];
            farmer.setInventory(newInventory);
            newPouch.Inventory[swapIndex] = temp;
            if(temp == null || swapIndex == Config.BagCapacity - 1) //Sort pouch after filling with empty slot or adding to the end of the pouch
            {
                newPouch.Inventory.Sort();
            }
        }

        private SortedDictionary<Item, int> getToolMap(List<Item> inventory)
        {
            SortedDictionary<Item, int> toolMap = new SortedDictionary<Item, int>(new DuplicateKeyComparer<Item>());
            int count = 0;
            foreach (Item item in inventory)
            {
                if(item != null)
                {
                    toolMap.Add(item, count);
                    count++;
                }
            }
            return toolMap;
        }

        private bool canOpenMenu(SButton b)
        {
            bool using_tool = Game1.player.UsingTool;
            return (Context.IsWorldReady && !using_tool && (b.Equals(Config.ToggleKey) | b.Equals(SButton.LeftStick)));
        }

        private void onLaunched(object sender, GameLaunchedEventArgs e)
        {
            var sc = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
            sc.RegisterSerializerType(typeof(Pouch));

            if (!Helper.ModRegistry.IsLoaded("spacechase0.GenericModConfigMenu")) return;
            var api = Helper.ModRegistry.GetApi<GenericModConfigMenuAPI>("spacechase0.GenericModConfigMenu");
            api.RegisterModConfig(ModManifest, () => Config = new ModConfig(), () => Helper.WriteConfig(Config));
            api.SetDefaultIngameOptinValue(ModManifest, true);
            api.RegisterLabel(ModManifest, "ToolPouch Options", "Configure to your Liking");
            api.RegisterSimpleOption(ModManifest, "Use Backdrop", "Use fancy Backdrop for better Visibilaty", () => Config.UseBackdrop, (bool val) => Config.UseBackdrop = val);
            api.RegisterClampedOption(ModManifest, "Animation Time (Milliseconds)", "Duration off the opening Animation", () => Config.AnimationMilliseconds, (int val) => Config.AnimationMilliseconds = val, 0, 500);
            api.RegisterSimpleOption(ModManifest, "Select with Leftstick", "Use left Stick of Gamepad to select Tool", () => Config.LeftStickSelection, (bool val) => Config.LeftStickSelection = val);
            api.RegisterSimpleOption(ModManifest, "Hover Tool selection", $"hold {Config.ToggleKey} and hover over item, stop holding to select", () => Config.HoverSelects, (bool val) => Config.HoverSelects = val);
            api.RegisterSimpleOption(ModManifest, "MenuKey", "Key for opening the ToolPouch Menu (Keyboard)", () => Config.ToggleKey, (SButton val) => Config.ToggleKey = val);

            api.RegisterLabel(ModManifest, "", "");
        }

        private void assetRequested(object sender, StardewModdingAPI.Events.AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo($"{ModManifest.UniqueID}/Pouches"))
            {
                e.LoadFrom(() =>
                {
                    Dictionary<string, PouchData> ret = new();
                    for (int i = 0; i < 1; ++i)
                    {
                        ret.Add($"Pouch",
                            new PouchData()
                            {
                                TextureIndex = i, // TODO: tmp
                                DisplayName = I18n.Pouch_Name(),
                                Description = I18n.Pouch_Description(),
                                Capacity = Config.BagCapacity * (i + 1),
                                MaxUpgrades = i,
                            });
                    }
                    return ret;
                }, StardewModdingAPI.Events.AssetLoadPriority.Exclusive);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo($"{ModManifest.UniqueID}/pouch.png"))
            {
                e.LoadFromModFile<Texture2D>("assets/pouch.png", StardewModdingAPI.Events.AssetLoadPriority.Low);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo($"{ModManifest.UniqueID}/upgrades.png"))
            {
                e.LoadFromModFile<Texture2D>("assets/upgrades.png", StardewModdingAPI.Events.AssetLoadPriority.Low);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops"))
            {
                e.Edit((asset) =>
                {
                    var data = asset.AsDictionary<string, ShopData>().Data;
                    data["Blacksmith"].Items.Add(new()
                    {
                        Id = "Pouch",
                        Price = 1,
                        TradeItemId = null,
                        TradeItemAmount = 0,
                        ItemId = "(CWZ)Pouch",
                    });
                });
            }
        }
    }
}