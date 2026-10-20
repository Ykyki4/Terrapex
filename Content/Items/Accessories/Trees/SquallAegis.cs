using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Accessories.Trees
{
	/// <summary>
	/// Node 1 of the dash line: the Shield of Cthulhu's dash and the Obsidian Shield's immunities,
	/// with a gust pushed ahead of every dash.
	/// Effects live in <see cref="TreeEffects"/>; see CRAFTING_TREES.md.
	/// </summary>
	public class SquallAegis : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 34;
			Item.height = 36;
			Item.value = Item.sellPrice(gold: 3);
			Item.rare = ItemRarityID.Orange;
			Item.accessory = true;
			Item.defense = 4;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			TreeEffects.Squall(player);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<GullFeather>()
				.AddIngredient(ItemID.EoCShield)
				.AddIngredient(ItemID.ObsidianShield)
				.AddTile(TileID.TinkerersWorkbench)
				.Register();
		}
	}
}
