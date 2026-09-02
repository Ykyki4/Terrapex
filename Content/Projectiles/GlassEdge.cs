using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// The Glasscutter's swing throws this: a thin pane of glass that flies flat, cuts through
	/// a couple of targets and then shatters into splinters. Glass does not survive the hit —
	/// the same rule the Glass Arrow follows — so the break is part of the damage.
	/// </summary>
	public class GlassEdge : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 10;
			ProjectileID.Sets.TrailingMode[Type] = 3;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// the tail slides down the glass ramp rather than fading one colour, so the pane
			// throws light the way glass does instead of smearing
			RiftDraw.Trail(Projectile,
				f => Color.Lerp(new Color(93, 92, 158, 0), new Color(234, 244, 255, 0), f) * (f * 0.50f),
				f => 0.60f + 0.40f * f);
			RiftDraw.Head(Projectile, Projectile.GetAlpha(lightColor));
			return false;
		}

		public override void SetDefaults()
		{
			Projectile.width = 26;
			Projectile.height = 14;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = 3;
			Projectile.timeLeft = 46;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();
			Projectile.velocity *= 0.985f;

			// fades out rather than vanishing, so the pane reads as glass
			Projectile.alpha = (int)MathHelper.Clamp((46 - Projectile.timeLeft) * 5f, 0f, 160f);

			if (Main.rand.NextBool(3))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 6f),
					DustID.Glass, Projectile.velocity * 0.1f, 140, default, 0.8f);
				d.noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, 0.16f, 0.20f, 0.32f);
		}

		public override void OnKill(int timeLeft)
		{
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Shatter with { Volume = 0.6f }, Projectile.Center);
			for (int i = 0; i < 8; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Glass,
					Main.rand.NextVector2Circular(3.4f, 3.4f), 120, default, 0.9f);
				d.noGravity = true;
			}

			if (Main.myPlayer != Projectile.owner)
				return;

			// three splinters thrown forward in a narrow fan
			Vector2 dir = Projectile.velocity.LengthSquared() > 0.04f
				? Vector2.Normalize(Projectile.velocity) : Vector2.UnitX;
			for (int i = -1; i <= 1; i++)
			{
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center,
					dir.RotatedBy(i * 0.34f) * 6.5f, ProjectileID.CrystalShard,
					(int)(Projectile.damage * 0.4f), 0f, Projectile.owner);
			}
		}
	}
}
