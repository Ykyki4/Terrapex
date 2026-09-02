using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Placeable
{
	public class CrackedStone : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 100;

		public override void SetDefaults()
		{
			Item.DefaultToPlaceableTile(ModContent.TileType<CrackedStoneTile>());
			Item.width = 18;
			Item.height = 18;
			Item.value = Item.sellPrice(copper: 3);
			Item.rare = ItemRarityID.White;
		}

		public override void AddRecipes()
		{
			CreateRecipe(10)
				.AddIngredient(ItemID.StoneBlock, 10)
				.AddIngredient(ModContent.ItemType<RiftDust>())
				.AddTile(TileID.WorkBenches)
				.Register();
		}
	}
}
