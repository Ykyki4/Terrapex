using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Placeable
{
	public class RiftglassBlock : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 100;

		public override void SetDefaults()
		{
			Item.DefaultToPlaceableTile(ModContent.TileType<RiftglassBlockTile>());
			Item.width = 16;
			Item.height = 16;
			Item.value = Item.sellPrice(copper: 20);
			Item.rare = ItemRarityID.Blue;
		}

		public override void AddRecipes()
		{
			CreateRecipe(4)
				.AddIngredient<Riftglass>()
				.AddTile(TileID.Furnaces)
				.Register();
		}
	}
}
