using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// Bonefissure's splinter. It ricochets twice, which is what makes the sword worth
	/// carrying in a corridor: the dungeon is all corridors, and a splinter that comes
	/// back off a wall covers ground the swing cannot.
	/// </summary>
	public class BoneSplinter : ModProjectile
	{
		private const int Bounces = 2;

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 6;
			ProjectileID.Sets.TrailingMode[Type] = 3;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// bone is not energy: this is motion blur, not a glow. If every weapon in the tier
			// lights up the same way they stop reading as different weapons.
			// a ref parameter cannot be captured by a lambda, so copy it first
			Color lit = lightColor;
			RiftDraw.Trail(Projectile, f => lit * (f * 0.32f), f => 0.70f + 0.30f * f);
			RiftDraw.Head(Projectile, lightColor);
			return false;
		}

		public override void SetDefaults()
		{
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = Bounces + 1;
			Projectile.timeLeft = 160;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = false;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
		}

		public override void AI()
		{
			Projectile.rotation += Projectile.velocity.X * 0.035f;

			// it drops only once it has spent its speed, so the ricochets stay flat
			if (Projectile.timeLeft < 120)
				Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.16f, 12f);

			Lighting.AddLight(Projectile.Center, 0.14f, 0.10f, 0.06f);
			if (Main.rand.NextBool(6))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Bone,
					Projectile.velocity * -0.1f, 140, default, 0.8f);
				d.noGravity = true;
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			Projectile.penetrate--;
			if (Projectile.penetrate <= 0)
				return true;

			if (Math.Abs(oldVelocity.X - Projectile.velocity.X) > 0.1f)
				Projectile.velocity.X = -oldVelocity.X * 0.85f;
			if (Math.Abs(oldVelocity.Y - Projectile.velocity.Y) > 0.1f)
				Projectile.velocity.Y = -oldVelocity.Y * 0.85f;

			Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.4f, Pitch = 0.6f }, Projectile.Center);
			// chips off the wall, so a ricochet is visible even when it happens offscreen-ish
			for (int i = 0; i < 5; i++)
			{
				Dust.NewDustPerfect(Projectile.Center, DustID.Bone,
					Projectile.velocity.RotatedByRandom(1.2f) * 0.35f, 120, default, 0.85f);
			}
			return false;
		}

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 6; i++)
			{
				Dust.NewDustPerfect(Projectile.Center, DustID.Bone,
					Main.rand.NextVector2Circular(2.5f, 2.5f), 130, default, 0.9f);
			}
		}
	}
}
