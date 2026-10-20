using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Accessories.Trees
{
	/// <summary>
	/// T0 boots. Flippers, water walking at full speed, +25% speed in water.
	/// Effects live in <see cref="TreeEffects"/>; see CRAFTING_TREES.md.
	/// </summary>
	public class TidewalkerSandals : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 34;
			Item.height = 26;
			Item.value = Item.sellPrice(silver: 40);
			Item.rare = ItemRarityID.Blue;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			TreeEffects.Tidewalker(player, hideVisual);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<BarnacleCluster>(8)
				.AddIngredient(ItemID.Seashell, 4)
				.AddIngredient(ItemID.Silk, 5)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
