using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// The Stone Eye's arrow. It bounces off tiles twice before dying, which is what
	/// makes the bow worth carrying in a cave: shots keep working after they miss.
	/// </summary>
	public class StoneEyeArrow : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WoodenArrowFriendly;

		private ref float Bounces => ref Projectile.ai[0];

		public override void SetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 3;
			Projectile.timeLeft = 420;
			Projectile.aiStyle = -1;
			Projectile.arrow = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 10;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			Projectile.velocity.Y = MathHelper.Min(Projectile.velocity.Y + 0.06f, 16f);

			if (Main.rand.NextBool(4))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Stone,
					-Projectile.velocity * 0.08f, 140, default, 0.8f);
				d.noGravity = true;
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			if (Bounces >= 2f)
				return true;

			Bounces++;
			if (Projectile.velocity.X != oldVelocity.X)
				Projectile.velocity.X = -oldVelocity.X * 0.85f;
			if (Projectile.velocity.Y != oldVelocity.Y)
				Projectile.velocity.Y = -oldVelocity.Y * 0.85f;

			Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
			for (int i = 0; i < 5; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Stone,
					Main.rand.NextVector2Circular(2f, 2f), 110, default, 1f);
				d.noGravity = true;
			}
			return false;
		}
	}
}
