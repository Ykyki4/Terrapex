using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// A blink: a ring of light thrown outward from a point, with one wedge of it missing.
	///
	/// The Weaver's <see cref="WebCollapse"/> closes on the player and its door runs away from
	/// them; this is the same idea turned inside out, which is what keeps the two from reading
	/// as one move. The ring opens outward instead of closing in, so the answer is not to hold
	/// a door but to be standing in the right wedge before it reaches you — and since it is
	/// drawn small and bright at the centre for thirty ticks before it can touch anything, the
	/// wedge is chosen calmly rather than in a panic.
	///
	/// It is one projectile because the shape moves, for the same reason the closing web is.
	/// </summary>
	public class BlinkRing : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_1";

		private const int Telegraph = 30;
		private const int Travel = 118;
		public const int Life = Telegraph + Travel;

		private const float StartRadius = 44f;
		private const float EndRadius = 900f;
		private const float Thickness = 26f;
		private const int Segments = 44;

		/// <summary>Middle of the missing wedge, in radians.</summary>
		private ref float Gap => ref Projectile.ai[0];
		/// <summary>Half-width of the missing wedge, in radians.</summary>
		private ref float GapHalf => ref Projectile.ai[1];

		private int Age => Life - Projectile.timeLeft;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.aiStyle = -1;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.netImportant = true;
		}

		public override bool ShouldUpdatePosition() => false;

		public static Projectile Spawn(IEntitySource source, Vector2 centre, int damage,
			float gap, float gapHalf)
		{
			Projectile p = Projectile.NewProjectileDirect(source, centre, Vector2.Zero,
				ModContent.ProjectileType<BlinkRing>(), damage, 0f, Main.myPlayer, gap, gapHalf);
			p.Center = centre;
			p.netUpdate = true;
			return p;
		}

		/// <summary>Eased so it leaves fast and arrives slow — the far edge is the readable one.</summary>
		private float Radius
		{
			get
			{
				int t = Age - Telegraph;
				if (t <= 0)
					return StartRadius;
				float f = MathHelper.Clamp(t / (float)Travel, 0f, 1f);
				return MathHelper.Lerp(StartRadius, EndRadius, 1f - (1f - f) * (1f - f));
			}
		}

		private bool InGap(float angle)
			=> Math.Abs(MathHelper.WrapAngle(angle - Gap)) < GapHalf;

		public override void AI()
		{
			if (Age == Telegraph)
			{
				SoundEngine.PlaySound(SoundID.Item122 with { Pitch = -0.5f }, Projectile.Center);
				if (Main.netMode != NetmodeID.Server)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center,
						Main.rand.NextVector2Unit(), 5f, 6f, 14, 2400f, "BlinkRing"));
			}

			Lighting.AddLight(Projectile.Center, 0.5f, 0.5f, 0.55f);

			if (Age < Telegraph)
				return;

			for (int i = 0; i < 2; i++)
			{
				float a = Main.rand.NextFloat(MathHelper.TwoPi);
				if (InGap(a))
					continue;
				Dust d = Dust.NewDustPerfect(Projectile.Center + a.ToRotationVector2() * Radius,
					DustID.WhiteTorch, a.ToRotationVector2() * 1.5f, 120, default, 1f);
				d.noGravity = true;
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (Age < Telegraph)
				return false;

			Vector2 to = targetHitbox.Center.ToVector2() - Projectile.Center;
			float dist = to.Length();
			float half = Thickness * 0.5f + Math.Max(targetHitbox.Width, targetHitbox.Height) * 0.5f;
			if (Math.Abs(dist - Radius) > half)
				return false;
			return !InGap(to.ToRotation());
		}

		public override bool PreDraw(ref Color lightColor)
		{
			bool live = Age >= Telegraph;
			float draw = MathHelper.Clamp(Age / (float)Telegraph, 0f, 1f);
			// it thins as it grows: a ring that kept its weight out at 900 px would black out
			// half the screen, and the far edge is the only part that still matters
			float fade = live ? 1f - MathHelper.Clamp((Radius - StartRadius) / EndRadius, 0f, 1f) * 0.55f : draw;
			float r = Radius;
			float thick = live ? Thickness * (0.55f + fade * 0.45f) : 3f;

			Vector2 prev = Vector2.Zero;
			bool has = false;
			for (int i = 0; i <= Segments; i++)
			{
				float a = i / (float)Segments * MathHelper.TwoPi;
				if (InGap(a))
				{
					has = false;
					continue;
				}
				Vector2 p = Projectile.Center + a.ToRotationVector2() * r;
				if (has)
				{
					RiftDraw.Line(prev, p, RiftDraw.Glow(22, 24, 34, fade * 0.55f), thick * 1.9f);
					RiftDraw.Line(prev, p, RiftDraw.Glow(200, 206, 226, fade * 0.65f), thick);
					RiftDraw.Line(prev, p, Color.White * (fade * 0.8f), thick * 0.28f);
				}
				prev = p;
				has = true;
			}

			// the two posts at the edges of the wedge, so the way through is never a guess
			for (int k = -1; k <= 1; k += 2)
			{
				Vector2 post = Projectile.Center + (Gap + k * GapHalf).ToRotationVector2() * r;
				RiftDraw.Bloom(post, RiftDraw.Glow(255, 235, 190, live ? 0.85f : 0.4f + draw * 0.4f),
					0.5f + fade * 0.3f);
			}

			RiftDraw.Bloom(Projectile.Center, RiftDraw.Glow(255, 255, 255, live ? fade * 0.35f : draw * 0.7f),
				live ? 0.8f : 0.5f + draw * 1.4f);
			return false;
		}
	}
}
