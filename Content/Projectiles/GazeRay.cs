using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// A line of sight, made lethal. The First Keeper does not throw this — it simply looks,
	/// and the looking is the attack.
	///
	/// The ray is a ray rather than a travelling projectile because the tier's whole subject is
	/// being seen: a beam that sweeps is something you have to stay out from under, which is a
	/// different verb from dodging a bullet and from stepping over a thread. It is completely
	/// harmless for its first <see cref="Telegraph"/> ticks and visibly narrows while it charges,
	/// for exactly the reason <see cref="RiftThread"/> does — an instantly lethal line drawn
	/// across a player who was already standing there is not a fight.
	///
	/// Three forms, all the same class:
	/// fixed (spin 0), sweeping (a constant spin), and tracking (<c>Anchor</c> set to the NPC it
	/// comes out of, turning toward that NPC's target at a capped rate so it can be outrun).
	/// </summary>
	public class GazeRay : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_1";

		public const int Telegraph = 46;
		private const int FadeOut = 20;
		private const float Length = 1500f;
		private const float Width = 20f;

		/// <summary>Which way it points, in radians. Advanced by the spin every tick.</summary>
		private ref float Angle => ref Projectile.ai[0];
		/// <summary>Radians per tick. Zero for a fixed ray.</summary>
		private ref float Spin => ref Projectile.ai[1];
		/// <summary>NPC this ray hangs off, or -1 for one pinned to a point in the world.</summary>
		private ref float Anchor => ref Projectile.localAI[1];
		private ref float Age => ref Projectile.localAI[0];

		/// <summary>How fast a tracking ray may turn. Slow enough that running out from under
		/// it is always an option, which is what keeps it from being a homing attack.</summary>
		private const float TrackRate = 0.0075f;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.aiStyle = -1;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 60 * 5;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.netImportant = true;
			Projectile.hide = false;
		}

		public override bool ShouldUpdatePosition() => false;

		/// <summary>
		/// <paramref name="anchor"/> is the NPC index the ray hangs off, or -1 to pin it where
		/// it was cast. An anchored ray follows its host and turns toward the host's target.
		/// </summary>
		public static Projectile Spawn(IEntitySource source, Vector2 origin, float angle, float spin,
			int damage, int life, int anchor = -1)
		{
			Projectile p = Projectile.NewProjectileDirect(source, origin,
				Vector2.Zero, ModContent.ProjectileType<GazeRay>(), damage, 0f, Main.myPlayer,
				angle, spin);
			p.Center = origin;
			p.timeLeft = life;
			p.localAI[1] = anchor;
			p.netUpdate = true;
			return p;
		}

		private float Strength
		{
			get
			{
				if (Age < Telegraph)
					return 0f;
				return Projectile.timeLeft < FadeOut ? Projectile.timeLeft / (float)FadeOut : 1f;
			}
		}

		private Vector2 Tip => Projectile.Center + Angle.ToRotationVector2() * Length;

		public override void AI()
		{
			Age++;

			int anchor = (int)Anchor;
			if (anchor >= 0 && anchor < Main.maxNPCs)
			{
				NPC host = Main.npc[anchor];
				if (!host.active)
				{
					Projectile.Kill();
					return;
				}
				Projectile.Center = host.Center;

				// A ray tracks only when it has no spin of its own. A sweep that also crept
				// toward the player would be a homing attack wearing a pattern's clothes, and
				// nothing a patterned boss fires in this mod homes - that rule cost the Weaver
				// a whole redesign. So: spin and it sweeps, no spin and it follows, never both.
				Player watched = Main.player[host.target];
				if (Spin == 0f && watched.active && !watched.dead)
				{
					float want = (watched.Center - Projectile.Center).ToRotation();
					float turn = MathHelper.WrapAngle(want - Angle);
					Angle += MathHelper.Clamp(turn, -TrackRate, TrackRate);
				}
			}

			Angle += Spin;

			if (Age == Telegraph)
			{
				SoundEngine.PlaySound(SoundID.Item122 with { Pitch = 0.6f }, Projectile.Center);
				for (int i = 0; i < 16; i++)
				{
					Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.WhiteTorch,
						Angle.ToRotationVector2().RotatedByRandom(0.4f) * Main.rand.NextFloat(2f, 7f),
						110, default, 1.2f);
					d.noGravity = true;
				}
			}

			if (Age < Telegraph)
				return;

			Vector2 dir = Angle.ToRotationVector2();
			for (float d2 = 60f; d2 < Length; d2 += 140f)
				Lighting.AddLight(Projectile.Center + dir * d2, 0.55f, 0.55f, 0.6f);

			if (Main.rand.NextBool(2))
			{
				Vector2 at = Projectile.Center + dir * Main.rand.NextFloat(40f, Length);
				Dust d3 = Dust.NewDustPerfect(at, DustID.WhiteTorch,
					dir.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-1.4f, 1.4f), 130, default, 0.9f);
				d3.noGravity = true;
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (Strength < 0.35f)
				return false;
			float point = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
				Projectile.Center, Tip, Width, ref point);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Vector2 a = Projectile.Center;
			Vector2 b = Tip;
			float live = Strength;

			if (live <= 0f)
			{
				// the charge: a hairline that brightens and a pupil that closes on it
				float draw = MathHelper.Clamp(Age / Telegraph, 0f, 1f);
				RiftDraw.Line(a, b, RiftDraw.Glow(90, 95, 110, 0.16f + draw * 0.34f), 1.4f + draw * 1.6f);
				RiftDraw.Line(a, b, RiftDraw.Glow(255, 255, 255, 0.10f + draw * 0.30f), 0.8f);
				RiftDraw.Bloom(a, RiftDraw.Glow(255, 255, 255, 0.35f + draw * 0.55f),
					0.9f - draw * 0.45f);
				return false;
			}

			float pulse = 1f + 0.09f * (float)Math.Sin(Age * 0.24f);
			float w = Width * live * pulse;

			// stacked the way every beam in this mod is: haze, body, white core. The core is
			// white and the haze is near-black, which is the T6 read — light with a hard rim
			// rather than a coloured bar.
			RiftDraw.Line(a, b, RiftDraw.Glow(24, 26, 38, live * 0.55f), w * 2.6f);
			RiftDraw.Line(a, b, RiftDraw.Glow(150, 158, 180, live * 0.60f), w * 1.35f);
			RiftDraw.Line(a, b, RiftDraw.Glow(235, 240, 250, live * 0.85f), w);
			RiftDraw.Line(a, b, Color.White * (live * 0.9f), w * 0.30f);

			RiftDraw.Bloom(a, RiftDraw.Glow(255, 255, 255, live * 0.8f), 1.1f * pulse);
			RiftDraw.Ring(a, RiftDraw.Glow(210, 216, 235, live * 0.45f), 0.9f, Age * 0.03f);
			return false;
		}
	}
}
