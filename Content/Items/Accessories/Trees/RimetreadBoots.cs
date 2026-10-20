using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Accessories.Trees
{
	/// <summary>
	/// T2 boots. Ice Skates, and a quicker start on snow and ice.
	/// Effects live in <see cref="TreeEffects"/>; see CRAFTING_TREES.md.
	/// </summary>
	public class RimetreadBoots : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 28;
			Item.value = Item.sellPrice(silver: 80);
			Item.rare = ItemRarityID.Orange;
			Item.accessory = true;
			Item.defense = 1;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			TreeEffects.Rimetread(player, hideVisual);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<FrostPelt>(6)
				.AddIngredient<RimeCrystal>(4)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
