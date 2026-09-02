using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// Plan item #63. It eats bullets and spits the Keeper's shards, so ammo choice stops
	/// mattering and the gun has exactly one behaviour to learn.
	/// </summary>
	public class ShardCaster : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 48;
			Item.height = 24;
			Item.damage = 26;
			Item.DamageType = DamageClass.Ranged;
			Item.knockBack = 3f;
			Item.useTime = 14;
			Item.useAnimation = 14;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item11;
			Item.value = Item.sellPrice(gold: 6);
			Item.rare = ItemRarityID.Pink;
			Item.useAmmo = AmmoID.Bullet;
			Item.shoot = ModContent.ProjectileType<CasterShard>();
			Item.shootSpeed = 7.5f;
		}

		public override Vector2? HoldoutOffset() => new Vector2(-8f, 0f);

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			Projectile.NewProjectile(source, position, velocity.RotatedByRandom(0.045f),
				ModContent.ProjectileType<CasterShard>(), damage, knockback, player.whoAmI);
			return false;
		}
	}
}
