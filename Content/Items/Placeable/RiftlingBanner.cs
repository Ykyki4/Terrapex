using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Placeable
{
	/// <summary>Plan item #75. The tier's only common drop, so the grind has a floor.</summary>
	public class RiftlingBanner : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.DefaultToPlaceableTile(ModContent.TileType<RiftlingBannerTile>());
			Item.width = 12;
			Item.height = 28;
			Item.value = Item.sellPrice(silver: 10);
			Item.rare = ItemRarityID.Blue;
		}
	}
}
