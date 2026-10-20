using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Particles;
using Terrapex.Content.Items.Weapons.Trees.Melee;

namespace Terrapex.Content.Projectiles.Trees.Melee
{
	/// <summary>
	/// The seam a Worldseam-node wave leaves where it lands: a cut in the air. It stands open
	/// for <see cref="Open"/> ticks - harmless, a thread drawn tight and widening - then snaps
	/// shut and hits everything along its line once, in the colour of the current that made it.
	/// The delay is the point: the hit lands after the wave has moved on, so a pack the wave went
	/// through takes a second blow it was not looking at.
	///
	/// ai[0] = angle of the cut, ai[1] = current, ai[2] = node.
	/// </summary>
	public class WorldseamSeam : ModProjectile
	{
		public const float Share = 0.6f;
		public const int Open = 30;
		private const int Snap = 6;
		private const int Fade = 16;
		private const float HalfLength = 56f;

		public override string Texture => "Terrapex/Content/Projectiles/RiftFlare";

		private int Age => (int)Projectile.localAI[0];
		private float Half => HalfLength * (1f + 0.15f * ((int)Projectile.ai[2] - 3));
		private Vector2 Dir => Projectile.ai[0].ToRotationVector2();

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.timeLeft = Open + Snap + Fade;
		}

		public override void AI()
		{
			Projectile.localAI[0]++;
			Projectile.velocity = Vector2.Zero;
			Color col = Currents.Of((Current)(int)Projectile.ai[1]);
			Lighting.AddLight(Projectile.Center, col.ToVector3() * 0.4f);
			if (Age == Open && !Main.dedServ)
			{
				SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.5f, Pitch = 0.4f }, Projectile.Center);
				Vector2 a = Projectile.Center - Dir * Half, b = Projectile.Center + Dir * Half;
				for (int i = 0; i < 8; i++)
				{
					Vector2 p = Vector2.Lerp(a, b, i / 7f);
					FX.Sparks(p, col, 2, 6f, 0.9f, 18, 0f, 0.8f, Projectile.ai[0] + MathHelper.PiOver2 * (i % 2 == 0 ? 1 : -1));
				}
				ParticleSystem.Spawn(new Bloom(Projectile.Center, Color.White, 0.4f, 1.3f, 14));
				ParticleSystem.Spawn(new Shockwave(Projectile.Center, col, 0.1f, 1.2f, 20, 0.35f) { Rotation = Projectile.ai[0] });
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (Age < Open || Age >= Open + Snap)
				return false;
			float point = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
				Projectile.Center - Dir * Half, Projectile.Center + Dir * Half, 18f, ref point);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
			=> Currents.Debuff(target, (Current)(int)Projectile.ai[1], (int)Projectile.ai[2]);

		public override bool PreDraw(ref Color lightColor)
		{
			Color col = Currents.Of((Current)(int)Projectile.ai[1]);
			Color thread = new(127, 242, 224);
			Vector2 c = Projectile.Center;
			if (Age < Open)
			{
				// open: the cut widens along its length, a dark gap held by tight thread
				float t = Age / (float)Open;
				float len = Half * (0.35f + 0.65f * (float)Math.Sqrt(t));
				Vector2 a = c - Dir * len, b = c + Dir * len;
				Vector2 n = Dir.RotatedBy(MathHelper.PiOver2);
				float gap = 1.5f + 3.5f * t;
				RiftDraw.Line(a, b, new Color(10, 22, 32) * 0.85f, gap * 2f);
				RiftDraw.Silk(a + n * gap, b + n * gap, Glow(col, 0.7f), Glow(col, 0.12f), 1.4f, 0f, (t * 2f) % 1f, 10);
				RiftDraw.Silk(a - n * gap, b - n * gap, Glow(thread, 0.7f), Glow(thread, 0.1f), 1.4f, 0f, (t * 2f + 0.5f) % 1f, 10);
				RiftDraw.Bloom(a, Glow(col, 0.5f), 0.18f);
				RiftDraw.Bloom(b, Glow(col, 0.5f), 0.18f);
			}
			else
			{
				// shut: one bright line that thins away
				float t = (Age - Open) / (float)(Snap + Fade);
				float k = 1f - t;
				Vector2 a = c - Dir * Half * (1f + 0.2f * t), b = c + Dir * Half * (1f + 0.2f * t);
				RiftDraw.Line(a, b, Glow(col, 0.35f * k), 14f * k + 2f);
				RiftDraw.Line(a, b, Glow(col, 0.9f * k), 5f * k + 1f);
				RiftDraw.Line(a, b, Glow(Color.White, k), 2f * k + 0.5f);
				RiftDraw.Bloom(c, Glow(Color.White, 0.6f * k), 0.5f * k + 0.1f, Projectile.ai[0]);
			}
			return false;
		}

		private static Color Glow(Color c, float s)
		{
			Color o = c * s;
			o.A = 0;
			return o;
		}
	}
}
