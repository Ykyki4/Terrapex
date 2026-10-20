using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Accessories.Trees
{
	/// <summary>
	/// Node 4: everything above plus Terraspark Boots - its rocket, its skates, its +8% - and run 8.5.
	/// Effects live in <see cref="TreeEffects"/>; see CRAFTING_TREES.md.
	/// </summary>
	public class WorldwalkerBoots : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 38;
			Item.height = 36;
			Item.value = Item.sellPrice(gold: 15);
			Item.rare = ItemRarityID.Red;
			Item.accessory = true;
			Item.defense = 4;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			TreeEffects.Worldwalker(player, hideVisual);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<ThreadstepBoots>()
				.AddIngredient(ItemID.TerrasparkBoots)
				.AddIngredient(ItemID.LunarBar, 10)
				.AddTile(TileID.TinkerersWorkbench)
				.Register();
		}
	}
}
