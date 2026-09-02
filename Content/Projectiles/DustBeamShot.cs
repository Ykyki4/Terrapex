using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Projectiles
{
	// A slow bolt that speeds up, like everything else the rift throws:
	// see the AccelDelay / MaxBoost note in RiftShard.
	public class DustBeamShot : ModProjectile
	{
		private const int AccelDelay = 8;
		private const float MaxBoost = 1.9f;

		public override void SetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.aiStyle = -1;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.friendly = true;
			Projectile.penetrate = 2;
			Projectile.tileCollide = true;
			Projectile.timeLeft = 240;
			Projectile.light = 0.6f;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

			Projectile.ai[0]++;
			if (Projectile.ai[0] > AccelDelay && Projectile.velocity.Length() < 9f * MaxBoost)
				Projectile.velocity *= 1.035f;

			if (Main.rand.NextBool(2))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
					-Projectile.velocity * 0.12f, 120, default, 0.95f);
				d.noGravity = true;
			}
		}

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 8; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
					Main.rand.NextVector2Circular(2.4f, 2.4f), 110, default, 1.05f);
				d.noGravity = true;
			}
		}
	}
}
