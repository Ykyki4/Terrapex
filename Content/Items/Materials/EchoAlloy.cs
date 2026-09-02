using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Materials
{
	/// <summary>
	/// Plan item #93. The heart drawn out into bar stock. One heart makes eight, so the tier
	/// is gated on beating the boss once rather than on farming it.
	/// </summary>
	public class EchoAlloy : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 25;

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 22;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Lime;
		}

		public override void AddRecipes()
		{
			CreateRecipe(8)
				.AddIngredient<WeaverHeart>(1)
				.AddIngredient(ItemID.ChlorophyteBar, 8)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
