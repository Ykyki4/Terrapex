using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Accessories.Trees
{
	/// <summary>
	/// Node 1 of the boots line: Dusty Boots, the three biome boots and a pair of Hermes-class
	/// boots in one - run 6.0 and +8% on top of everything they did.
	/// Effects live in <see cref="TreeEffects"/>; see CRAFTING_TREES.md.
	/// </summary>
	public class WayfarersBoots : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 38;
			Item.height = 32;
			Item.value = Item.sellPrice(gold: 3);
			Item.rare = ItemRarityID.Orange;
			Item.accessory = true;
			Item.defense = 1;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			TreeEffects.Wayfarer(player, hideVisual);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<DustyBoots>()
				.AddIngredient<TidewalkerSandals>()
				.AddIngredient<SandstriderWraps>()
				.AddIngredient<RimetreadBoots>()
				.AddRecipeGroup(TreeRecipeGroups.AnyHermesBoots)
				.AddTile(TileID.TinkerersWorkbench)
				.Register();
		}
	}
}
