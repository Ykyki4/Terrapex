using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Materials
{
	/// <summary>
	/// Plan item #30. Cornea fired with sand: the tier where the crack stops being rock
	/// and starts bending light.
	/// </summary>
	public class Riftglass : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 25;
			ItemID.Sets.SortingPriorityMaterials[Type] = 62;
		}

		public override void SetDefaults()
		{
			Item.width = 20;
			Item.height = 20;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 3);
			Item.rare = ItemRarityID.Blue;
			Item.material = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe(4)
				.AddIngredient<DormantCornea>()
				.AddIngredient(ItemID.SandBlock, 6)
				.AddTile(TileID.Furnaces)
				.Register();
		}
	}
}
