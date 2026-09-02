using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Content.Buffs;

namespace Terrapex.Content.Projectiles
{
	/// <summary>The Rib's arrow. Its whole job is to leave the mark behind.</summary>
	public class RibArrow : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 8;
			ProjectileID.Sets.TrailingMode[Type] = 3;
		}

		public override void SetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 400;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = false;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			Projectile.velocity.Y += 0.055f;

			Lighting.AddLight(Projectile.Center, 0.24f, 0.08f, 0.32f);
			if (Main.rand.NextBool(3))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
					Projectile.velocity * -0.06f, 140, default, 0.85f);
				d.noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// bone body lit by the world, rift tail behind it — the arrow shows what it is
			// carrying before it lands, which is the only reason to fire it
			RiftDraw.Trail(Projectile,
				f => RiftDraw.Glow(176, 74, 214, f * 0.38f),
				f => 0.45f + 0.50f * f);
			RiftDraw.Bloom(Projectile.Center, RiftDraw.Glow(210, 120, 235, 0.30f), 0.13f);
			RiftDraw.Head(Projectile, Projectile.GetAlpha(lightColor));
			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(ModContent.BuffType<RiftMark>(), 480);
			target.AddBuff(ModContent.BuffType<Cracked>(), 180);

			// the mark landing gets its own pulse: a support shot does little damage, so
			// without this there is nothing telling you it connected
			for (int i = 0; i < 14; i++)
			{
				Vector2 out2 = (MathHelper.TwoPi * i / 14f).ToRotationVector2();
				Dust d = Dust.NewDustPerfect(target.Center + out2 * 6f, DustID.PurpleTorch,
					out2 * 3.4f, 100, default, 1.1f);
				d.noGravity = true;
			}
		}

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 5; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
					Main.rand.NextVector2Circular(2.5f, 2.5f), 120, default, 1f);
				d.noGravity = true;
			}
		}
	}
}
