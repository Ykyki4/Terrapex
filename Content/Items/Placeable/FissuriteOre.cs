using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Placeable
{
	public class FissuriteOre : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 100;
			ItemID.Sets.SortingPriorityMaterials[Type] = 58;
		}

		public override void SetDefaults()
		{
			Item.DefaultToPlaceableTile(ModContent.TileType<FissuriteOreTile>());
			Item.width = 20;
			Item.height = 20;
			Item.value = Item.sellPrice(copper: 45);
			Item.rare = ItemRarityID.White;
		}
	}
}
