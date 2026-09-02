using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Placeable
{
	public class DormantEyeTrophy : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.DefaultToPlaceableTile(ModContent.TileType<DormantEyeTrophyTile>());
			Item.width = 32;
			Item.height = 32;
			Item.value = Item.sellPrice(silver: 50);
			Item.rare = ItemRarityID.Blue;
		}
	}
}
