using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Placeable
{
	public class RiftLantern : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.DefaultToPlaceableTile(ModContent.TileType<RiftLanternTile>());
			Item.width = 18;
			Item.height = 28;
			Item.value = Item.sellPrice(silver: 12);
			Item.rare = ItemRarityID.Orange;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Riftglass>(4)
				.AddIngredient(ItemID.Chain)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
