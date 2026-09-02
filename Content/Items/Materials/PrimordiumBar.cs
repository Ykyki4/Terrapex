using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Materials
{
	/// <summary>
	/// Plan item #113. Primordium will not hold a shape on its own, so it is carried in
	/// luminite the way a pigment is carried in a binder.
	/// </summary>
	public class PrimordiumBar : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 25;

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 22;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(gold: 3);
			Item.rare = ItemRarityID.Red;
		}

		public override void AddRecipes()
		{
			CreateRecipe(4)
				.AddIngredient<Primordium>(4)
				.AddIngredient(ItemID.LunarBar, 4)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
