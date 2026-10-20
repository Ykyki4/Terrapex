using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Weapons.Shellback
{
	/// <summary>
	/// The Old Shellback's ranged drop: a ship's blunderbuss with a flared bell, fished out of the
	/// wreck. Four bullets in a fixed spread - a close-range gun, the one drop of the five that
	/// wants the target at arm's length.
	/// </summary>
	public class BarnacleBlunderbuss : ModItem
	{
		public const int Pellets = 4;
		public const float Spread = 0.30f;

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 50;
			Item.height = 22;
			Item.damage = 11;
			Item.DamageType = DamageClass.Ranged;
			Item.knockBack = 4f;
			Item.useTime = 40;
			Item.useAnimation = 40;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item36;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Blue;
			Item.useAmmo = AmmoID.Bullet;
			Item.shoot = ProjectileID.Bullet;
			Item.shootSpeed = 8f;
		}

		public override Vector2? HoldoutOffset() => new Vector2(-6f, 0f);

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			Vector2 muzzle = Vector2.Normalize(velocity) * 40f;
			if (Collision.CanHit(position, 0, 0, position + muzzle, 0, 0))
				position += muzzle;
			for (int i = 0; i < Pellets; i++)
			{
				float t = i / (Pellets - 1f) - 0.5f;
				Vector2 v = velocity.RotatedBy(t * Spread + Main.rand.NextFloat(-0.04f, 0.04f)) * Main.rand.NextFloat(0.9f, 1.08f);
				Projectile.NewProjectile(source, position, v, type, damage, knockback, player.whoAmI);
			}
			return false;
		}
	}
}
