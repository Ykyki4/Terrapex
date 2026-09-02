using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Placeable
{
	/// <summary>Plan item #130.</summary>
	public class FirstKeeperTrophy : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 32;
			Item.maxStack = Item.CommonMaxStack;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.useAnimation = 15;
			Item.useTime = 10;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.consumable = true;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Red;
			Item.createTile = ModContent.TileType<FirstKeeperTrophyTile>();
		}
	}
}
