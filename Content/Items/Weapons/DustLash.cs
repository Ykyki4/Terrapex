using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Items.Weapons
{
	// The tier's summoner entry. There is no T0 minion yet, so the lash is bought
	// for its tag damage — it is the reason a summoner has anything to hold before
	// the first boss.
	public class DustLash : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			// spelled out rather than via Item.DefaultToWhip so the numbers are
			// visible next to every other T0 weapon
			Item.width = 32;
			Item.height = 32;
			Item.damage = 11;
			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.knockBack = 1.4f;
			Item.useTime = 32;
			Item.useAnimation = 32;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.shoot = ModContent.ProjectileType<DustLashWhip>();
			Item.shootSpeed = 4f;
			Item.value = Item.sellPrice(silver: 34);
			Item.rare = ItemRarityID.Blue;
			Item.UseSound = SoundID.Item152;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<FissuriteBar>(), 6)
				.AddIngredient(ModContent.ItemType<RiftDust>(), 15)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
