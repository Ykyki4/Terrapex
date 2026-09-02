using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Materials
{
	/// <summary>
	/// Plan item #77. Woven at a plain loom rather than the Rift Altar — it is still cloth,
	/// and making the vanilla station matter once keeps the altar from being the answer to
	/// every recipe in the mod.
	/// </summary>
	public class VoidCloth : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 25;

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 20;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 90);
			Item.rare = ItemRarityID.LightPurple;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<VoidThread>(5)
				.AddTile(TileID.Loom)
				.Register();
		}
	}
}
