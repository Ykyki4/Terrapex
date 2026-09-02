using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// Plan item #96. Its shot does not stop at the first thing it hits: it stitches on to the
	/// next, and the next, losing a fifth each jump. Damage per shot is low for the tier
	/// because the weapon is paid in targets.
	/// </summary>
	public class Weft : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 52;
			Item.height = 24;
			Item.damage = 58;
			Item.DamageType = DamageClass.Ranged;
			Item.knockBack = 2f;
			Item.useTime = 14;
			Item.useAnimation = 14;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item11;
			Item.value = Item.sellPrice(gold: 24);
			Item.rare = ItemRarityID.Lime;
			Item.useAmmo = AmmoID.Bullet;
			Item.shoot = ModContent.ProjectileType<WeftBolt>();
			Item.shootSpeed = 11f;
		}

		public override Vector2? HoldoutOffset() => new Vector2(-8f, 0f);

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			Projectile.NewProjectile(source, position, velocity.RotatedByRandom(0.02f),
				ModContent.ProjectileType<WeftBolt>(), damage, knockback, player.whoAmI);
			return false;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<EchoAlloy>(14)
				.AddIngredient(ItemID.ChlorophyteBar, 8)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
