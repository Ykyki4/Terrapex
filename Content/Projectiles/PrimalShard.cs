using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// The First Keeper's shard. Structurally the same idea as <see cref="RiftShard"/> — it
	/// leaves slow and winds up, so a volley can be led rather than reacted to — but it is the
	/// tier's own colour: a white core inside a black rim, with no rift violet anywhere.
	///
	/// T6's whole visual claim is that the crack came from something that predates the colour
	/// the rest of the mod is painted in, so the last tier is the one place the palette drops
	/// its hue entirely.
	/// </summary>
	public class PrimalShard : ModProjectile
	{
		private const int AccelDelay = 22;
		private const float MaxBoost = 2.4f;

		private ref float Age => ref Projectile.ai[0];

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 10;
			ProjectileID.Sets.TrailingMode[Type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.aiStyle = -1;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 420;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
		}

		public override void AI()
		{
			Age++;
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

			// slow out of the gate, then it winds up: the shot is readable at the moment it is
			// fired and dangerous by the time it arrives
			if (Age > AccelDelay && Projectile.velocity.Length() < 5.4f * MaxBoost)
				Projectile.velocity *= 1.017f;

			Lighting.AddLight(Projectile.Center, 0.55f, 0.55f, 0.6f);

			if (Main.rand.NextBool(3))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.WhiteTorch,
					Projectile.velocity * -0.08f, 120, default, 0.9f);
				d.noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float speed = MathHelper.Clamp(Projectile.velocity.Length() / 12f, 0f, 1f);
			// the trail grows with the wind-up, so the acceleration is legible without a number
			RiftDraw.Trail(Projectile, f => RiftDraw.Glow(220, 225, 235, f * 0.55f),
				f => 0.5f + f * 0.5f, 0.4f + speed * 0.6f);
			RiftDraw.Bloom(Projectile.Center, RiftDraw.Glow(255, 255, 255, 0.35f + speed * 0.3f), 0.34f);
			RiftDraw.Head(Projectile, Color.White);
			return false;
		}

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 8; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.WhiteTorch,
					Main.rand.NextVector2Circular(3f, 3f), 110, default, 1.1f);
				d.noGravity = true;
			}
		}
	}
}
