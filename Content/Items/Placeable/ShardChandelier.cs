using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Placeable
{
	public class ShardChandelier : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.DefaultToPlaceableTile(ModContent.TileType<ShardChandelierTile>());
			Item.width = 32;
			Item.height = 32;
			Item.value = Item.sellPrice(silver: 45);
			Item.rare = ItemRarityID.Orange;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Riftglass>(12)
				.AddIngredient(ItemID.Chain, 4)
				.AddIngredient(ModContent.ItemType<RiftTorch>(), 4)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
