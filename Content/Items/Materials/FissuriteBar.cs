using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Placeable;

namespace Terrapex.Content.Items.Materials
{
	public class FissuriteBar : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 25;
			ItemID.Sets.SortingPriorityMaterials[Type] = 59;
		}

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 18;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 2);
			Item.rare = ItemRarityID.Blue;
			Item.material = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<FissuriteOre>(), 3)
				.AddTile(TileID.Furnaces)
				.Register();
		}
	}
}
