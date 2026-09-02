using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>Plan item #86. Thirty-two tiles of thread with a needle on the end.</summary>
	public class ThreadAnchor : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.CloneDefaults(ItemID.GrapplingHook);
			Item.width = 24;
			Item.height = 24;
			Item.value = Item.sellPrice(gold: 5);
			Item.rare = ItemRarityID.Yellow;
			Item.shoot = ModContent.ProjectileType<ThreadAnchorHook>();
			Item.shootSpeed = 18f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<VoidCloth>(5)
				.AddIngredient(ItemID.HallowedBar, 6)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
