using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Accessories.Trees
{
	/// <summary>
	/// T1 boots. Sand and desert wind do not hold you, the desert is faster, and the run kicks
	/// blinding sand into whatever is behind you.
	/// Effects live in <see cref="TreeEffects"/>; see CRAFTING_TREES.md.
	/// </summary>
	public class SandstriderWraps : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 30;
			Item.value = Item.sellPrice(silver: 60);
			Item.rare = ItemRarityID.Green;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			TreeEffects.Sandstrider(player, hideVisual);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<GildedChitin>(6)
				.AddIngredient<Sunsand>(6)
				.AddIngredient(ItemID.Silk, 5)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
