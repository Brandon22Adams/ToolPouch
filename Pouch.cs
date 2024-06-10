using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Internal;
using StardewValley.Inventories;
using StardewValley.Tools;

namespace ToolPouch
{
    [XmlType("Mods_CWZ_ToolPouch")]
    public class Pouch : GenericTool
    {
        public override string DisplayName => GetDisplayName();
        public string Description { get; set; }

        public override string TypeDefinitionId => "(CWZ)";

        public Inventory Inventory => netInventory.Value;
        public Inventory Upgrades => netUpgrades.Value;

        public readonly NetString pouchName = new();
        public readonly NetRef<Inventory> netInventory = new(new());
        public readonly NetRef<Inventory> netUpgrades = new(new());

        [XmlIgnore]
        public readonly NetBool isOpen = new(false);

        public Pouch()
        {
            NetFields.AddField(pouchName)
                .AddField(netInventory)
                .AddField(netUpgrades)
                .AddField(isOpen);

            InstantUse = true;
        }
        public Pouch(string type)
        : this()
        {
            ItemId = type;
            ReloadData();
            while (Inventory.Count < PouchDataDefinition.GetSpecificData(ItemId).Capacity)
                Inventory.Add(null);
            while (Upgrades.Count < PouchDataDefinition.GetSpecificData(ItemId).MaxUpgrades)
                Upgrades.Add(null);
        }

        private string GetDisplayName()
        {
            try
            {
                if (!string.IsNullOrEmpty(pouchName.Value))
                    return pouchName.Value;

                var data = Game1.content.Load<Dictionary<string, PouchData>>("CodeWordZ.ToolPouch/Pouches");
                return data[ItemId].DisplayName;
            }
            catch (Exception e)
            {
                return "Error Item";
            }
        }

        public void ReloadData()
        {
            var data = Game1.content.Load<Dictionary<string, PouchData>>("CodeWordZ.ToolPouch/Pouches");
            Name = ItemId;
            Description = data[ItemId].Description + "^^" + I18n.Pouch_Description();
        }

        public override bool canThisBeAttached(StardewValley.Object o)
        {
            return true;
        }

        public override StardewValley.Object attach(StardewValley.Object o)
        {
            if (o == null)
            {
                ModEntry.QueueOpeningPouch(this);
                return null;
            }
            else
                return (StardewValley.Object)quickDeposit(o);
        }

        public Item quickDeposit(Item item)
        {
            for (int i = 0; i < Inventory.Count; ++i)
            {
                if (Inventory[i]?.canStackWith(item) ?? false)
                {
                    int left = Inventory[i].addToStack(item);
                    if (left <= 0)
                        return null;
                    item.Stack = left;
                }
            }

            for (int i = 0; i < Inventory.Count; ++i)
            {
                if (Inventory[i] == null)
                {
                    Inventory[i] = item;
                    return null;
                }
            }

            return item;
        }

        public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            //spriteBatch.Draw( Assets.Necklaces, location + new Vector2( 32, 32 ), new Rectangle( ( ( int ) necklaceType.Value ) % 4 * 16, ( ( int ) necklaceType.Value ) / 4 * 16, 16, 16 ), color * transparency, 0, new Vector2( 8, 8 ) * scaleSize, scaleSize * Game1.pixelZoom, SpriteEffects.None, layerDepth );
            var data = ItemRegistry.GetDataOrErrorItem(QualifiedItemId);
            Rectangle rect = data.GetSourceRect(0);
            spriteBatch.Draw(data.GetTexture(), location + new Vector2(32, 32) * scaleSize, rect, color * transparency, 0, new Vector2(8, 8) * scaleSize, scaleSize * 4, SpriteEffects.None, layerDepth);
        }

        public override string getDescription()
        {
            Description = loadDescription() + "^^" + I18n.Pouch_Description();
            return Game1.parseText(Description.Replace('^', '\n'), Game1.smallFont, getDescriptionWidth());
        }

        public override void DoFunction(GameLocation location, int x, int y, int power, Farmer who)
        {
            ModEntry.QueueOpeningPouch(this);
            //who.forceCanMove();
        }

        protected override Item GetOneNew()
        {
            return new Pouch();
        }

        protected override void GetOneCopyFrom(Item source)
        {
            var pouch = source as Pouch;
            base.GetOneCopyFrom(source);

            ItemId = source.ItemId;
            ReloadData();

            while (Inventory.Count < PouchDataDefinition.GetSpecificData(ItemId).Capacity)
                Inventory.Add(null);
            while (Upgrades.Count < PouchDataDefinition.GetSpecificData(ItemId).MaxUpgrades)
                Upgrades.Add(null);

            for (int i = 0; i < Inventory.Count; ++i)
            {
                Inventory[i] = pouch.Inventory[i]?.getOne();
            }
            for (int i = 0; i < Upgrades.Count; ++i)
            {
                Upgrades[i] = pouch.Upgrades[i]?.getOne();
            }
        }

        public override bool canBeTrashed()
        {
            return false;
        }

        public override bool canBeGivenAsGift()
        {
            return false;
        }

        public override bool canBeDropped()
        {
            return false;
        }

        public override bool ForEachItem(ForEachItemDelegate handler)
        {
            if (!base.ForEachItem(handler))
                return false;

            if (!ForEachItemHelper.ApplyToList(Inventory, handler, true))
                return false;

            if (!ForEachItemHelper.ApplyToList(Upgrades, handler, true))
                return false;

            return true;
        }
    }
}
