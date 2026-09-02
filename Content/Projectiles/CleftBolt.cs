using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// The Cleft's bolt. It splits — that is the whole weapon. The halves fly on from the
	/// impact rather than from the bow, so the crossbow rewards shooting *into* a group
	/// instead of at its nearest edge.
	/// </summary>
	public class CleftBolt : ModProjectile
	{
		/// <summary>Set on the halves so they cannot split again and cascade.</summary>
		private ref float IsHalf => ref Projectile.ai[0];

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 7;
			ProjectileID.Sets.TrailingMode[Type] = 3;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// a short, hard tail that falls off as f squared: the bolt splits into three, and a
			// long smear would hide which one you are looking at
			float half = IsHalf > 0f ? 0.55f : 1f;
			RiftDraw.Trail(Projectile,
				f => RiftDraw.Glow(138, 74, 190, f * f * 0.50f * half),
				f => 0.40f + 0.55f * f,
				0.6f);
			RiftDraw.Head(Projectile, Projectile.GetAlpha(lightColor));
			return false;
		}

		public override void SetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 300;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = false;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			Projectile.velocity.Y += IsHalf > 0f ? 0.14f : 0.07f;

			Lighting.AddLight(Projectile.Center, 0.16f, 0.06f, 0.22f);
			if (Main.rand.NextBool(4))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Glass,
					Projectile.velocity * -0.08f, 150, default, 0.75f);
				d.noGravity = true;
			}
		}

		private void Split()
		{
			if (IsHalf > 0f || Main.myPlayer != Projectile.owner)
				return;

			for (int i = -1; i <= 1; i += 2)
			{
				Projectile p = Projectile.NewProjectileDirect(
					Projectile.GetSource_FromThis(), Projectile.Center,
					Projectile.velocity.RotatedBy(i * 0.55f) * 0.7f,
					Type, (int)(Projectile.damage * 0.55f), Projectile.knockBack * 0.5f, Projectile.owner);
				p.ai[0] = 1f;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => Split();

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			Split();
			return true;
		}

		public override void OnKill(int timeLeft)
		{
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Shatter with { Volume = 0.35f }, Projectile.Center);
			for (int i = 0; i < 5; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Glass,
					Main.rand.NextVector2Circular(2.5f, 2.5f), 130, default, 0.85f);
				d.noGravity = true;
			}
		}
	}
}
