using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;

namespace Terrapex.Content.Projectiles
{
	/// <summary>Plan item #120's shot. A mote of the stuff the crack was made of, in a jacket.</summary>
	public class PrimalBullet : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 8;
			ProjectileID.Sets.TrailingMode[Type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.width = 6;
			Projectile.height = 6;
			Projectile.aiStyle = 1;
			AIType = ProjectileID.Bullet;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 600;
			Projectile.extraUpdates = 1;
			Projectile.light = 0.5f;
		}

		public override void AI()
		{
			if (Main.rand.NextBool(3))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.WhiteTorch,
					Projectile.velocity * -0.05f, 130, default, 0.7f);
				d.noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// no glow ramp and no colour: the tier is white light with a hard rim, and a bullet
			// is the smallest place that rule has to hold
			RiftDraw.Trail(Projectile, f => RiftDraw.Glow(210, 216, 235, f * 0.45f), f => f, 1f);
			RiftDraw.Head(Projectile, Color.White);
			return false;
		}
	}
}
