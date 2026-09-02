using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Placeable
{
	public class KeeperTrophy : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.DefaultToPlaceableTile(ModContent.TileType<KeeperTrophyTile>());
			Item.width = 32;
			Item.height = 32;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Pink;
		}
	}
}
