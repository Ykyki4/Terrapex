using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// The Weaver's closing net: a twelve-sided ring of silk that tightens on a point, with two
	/// of its sides deliberately left unstrung.
	///
	/// This is one projectile rather than twelve threads because the ring has to *move*, and a
	/// <see cref="RiftThread"/> is pinned by definition. Owning all twelve sides also lets the
	/// gap be a designed thing instead of an accident: it is always exactly two sides wide, it
	/// is drawn from the first frame, and the ring turns slowly while it closes so that finding
	/// the door once is not enough — you have to keep it.
	///
	/// The whole shape is harmless for its first 60 ticks. That is the read: you are shown the
	/// ring, and the hole in it, before anything can touch you.
	/// </summary>
	public class WebCollapse : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_1";

		private const int Sides = 12;
		private const int GapSides = 2;
		public const int Telegraph = 60;
		private const int Contract = 150;
		private const int Hold = 40;
		public const int Life = Telegraph + Contract + Hold;

		private const float StartRadius = 540f;
		private const float EndRadius = 88f;
		private const float Width = 12f;

		/// <summary>First of the two unstrung sides.</summary>
		private ref float Gap => ref Projectile.ai[0];
		/// <summary>Which way the ring turns as it closes.</summary>
		private ref float Spin => ref Projectile.ai[1];

		private int Age => Life - Projectile.timeLeft;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.aiStyle = -1;
			Projectile.friendly = false;
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.netImportant = true;
		}

		public override bool ShouldUpdatePosition() => false;

		public static Projectile Spawn(NPC source, Vector2 centre, int damage, int gap, float spin)
		{
			Projectile p = Projectile.NewProjectileDirect(source.GetSource_FromAI(), centre,
				Vector2.Zero, ModContent.ProjectileType<WebCollapse>(), damage, 0f, Main.myPlayer,
				gap, spin);
			p.Center = centre;
			p.netUpdate = true;
			return p;
		}

		/// <summary>Radius now: held wide through the telegraph, then drawn in.</summary>
		private float Radius
		{
			get
			{
				int t = Age - Telegraph;
				if (t <= 0)
					return StartRadius;
				// eased, so the last stretch is the fast one and the panic is at the end
				float f = MathHelper.Clamp(t / (float)Contract, 0f, 1f);
				return MathHelper.Lerp(StartRadius, EndRadius, f * f);
			}
		}

		private float Rotation => Age * 0.0055f * (Spin < 0f ? -1f : 1f);

		private Vector2 Vertex(int i)
			=> Projectile.Center
				+ (Rotation + i * MathHelper.TwoPi / Sides).ToRotationVector2() * Radius;

		/// <summary>The gap is two consecutive sides; everything else is strung.</summary>
		private bool Strung(int side)
		{
			int g = (int)Gap;
			for (int k = 0; k < GapSides; k++)
				if (side == ((g + k) % Sides + Sides) % Sides)
					return false;
			return true;
		}

		public override void AI()
		{
			if (Age == Telegraph)
			{
				SoundEngine.PlaySound(SoundID.Item122 with { Pitch = -0.3f }, Projectile.Center);
				if (Main.netMode != NetmodeID.Server)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center,
						Main.rand.NextVector2Unit(), 6f, 6f, 18, 2400f, "WebCollapse"));
			}

			Lighting.AddLight(Projectile.Center, 0.3f, 0.7f, 0.65f);

			if (Age < Telegraph || !Main.rand.NextBool(2))
				return;

			int side = Main.rand.Next(Sides);
			if (!Strung(side))
				return;
			Dust d = Dust.NewDustPerfect(Vector2.Lerp(Vertex(side), Vertex(side + 1),
				Main.rand.NextFloat()), DustID.Vortex, Vector2.Zero, 120, default, 1f);
			d.noGravity = true;
			d.velocity *= 0.2f;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (Age < Telegraph)
				return false;

			for (int i = 0; i < Sides; i++)
			{
				if (!Strung(i))
					continue;
				float point = 0f;
				if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
					Vertex(i), Vertex(i + 1), Width, ref point))
					return true;
			}
			return false;
		}

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Item27, Projectile.Center);
			for (int i = 0; i < 30; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center,
					DustID.Vortex, Main.rand.NextVector2CircularEdge(5f, 5f), 110, default, 1.3f);
				d.noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			bool live = Age >= Telegraph;
			float draw = MathHelper.Clamp(Age / (float)Telegraph, 0f, 1f);
			// it brightens as it tightens, so how close the net is is legible at a glance
			float tight = live
				? 1f - MathHelper.Clamp((Radius - EndRadius) / (StartRadius - EndRadius), 0f, 1f)
				: 0f;

			Color hot = live
				? RiftDraw.Glow(150, 255, 240, 0.85f + tight * 0.15f)
				: RiftDraw.Glow(60, 165, 165, 0.28f + draw * 0.5f);
			Color haze = live ? RiftDraw.Glow(30, 160, 150, 0.55f + tight * 0.35f) : default;
			float thick = live ? 5f + tight * 3.5f + (float)Math.Sin(Age * 0.16f) * 1.2f : 1.7f;
			float pulse = live ? Age * 0.011f % 1f : -1f;

			for (int i = 0; i < Sides; i++)
			{
				if (!Strung(i))
					continue;
				RiftDraw.Silk(Vertex(i), Vertex(i + 1), hot, haze, thick, 0f, pulse, 6);
			}

			// the two posts either side of the door, so the way out is never ambiguous
			int g = (int)Gap;
			foreach (int v in new[] { g, g + GapSides })
				RiftDraw.Bloom(Vertex(v), RiftDraw.Glow(255, 210, 140, live ? 0.9f : 0.5f + draw * 0.4f),
					0.55f + (live ? tight * 0.3f : 0f));

			// the eye of the net: what the ring is closing on
			RiftDraw.Ring(Projectile.Center, RiftDraw.Glow(120, 255, 235, live ? 0.35f + tight * 0.4f : draw * 0.25f),
				Radius / 32f * 0.18f, Rotation);
			RiftDraw.Bloom(Projectile.Center, RiftDraw.Glow(150, 255, 240, live ? 0.25f + tight * 0.5f : 0.1f),
				0.6f + tight);
			return false;
		}
	}
}
