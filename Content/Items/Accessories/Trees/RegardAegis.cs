using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Accessories.Trees
{
	/// <summary>
	/// Node 4: the dash goes where the cursor is, on its own key, and for a second after it everything
	/// it passed through takes x1.3 damage from you.
	/// Effects live in <see cref="TreeEffects"/>; see CRAFTING_TREES.md.
	/// </summary>
	public class RegardAegis : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 36;
			Item.height = 44;
			Item.value = Item.sellPrice(gold: 16);
			Item.rare = ItemRarityID.Red;
			Item.accessory = true;
			Item.defense = 20;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			TreeEffects.Regard(player);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<WeaversAegis>()
				.AddIngredient(ItemID.LunarBar, 10)
				.AddIngredient<PrimordiumBar>(8)
				.AddTile(TileID.TinkerersWorkbench)
				.Register();
		}
	}
}
