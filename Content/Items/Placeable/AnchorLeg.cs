using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Placeable
{
	/// <summary>
	/// The Weaver leaves a leg behind. It is the tier's crafting station — the design doc has
	/// it staying planted in the arena, but dropping it as a placeable is the same thing
	/// without the failure mode of trying to plant a tile in mid-air.
	/// </summary>
	public class AnchorLeg : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 34;
			Item.maxStack = Item.CommonMaxStack;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.useAnimation = 15;
			Item.useTime = 10;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.consumable = true;
			Item.value = Item.sellPrice(gold: 5);
			Item.rare = ItemRarityID.Lime;
			Item.createTile = ModContent.TileType<AnchorLegTile>();
		}
	}
}
