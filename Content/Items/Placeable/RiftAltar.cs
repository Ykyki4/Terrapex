using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Placeable
{
	/// <summary>Plan item #51. Cheap on purpose: it gates the tier, it should not gate itself.</summary>
	public class RiftAltar : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.DefaultToPlaceableTile(ModContent.TileType<RiftAltarTile>());
			Item.width = 34;
			Item.height = 26;
			Item.value = Item.sellPrice(silver: 40);
			Item.rare = ItemRarityID.Green;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<CrackedStone>(20)
				.AddIngredient<Riftglass>(10)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
