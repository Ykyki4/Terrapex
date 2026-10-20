using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace Terrapex.Common.Particles
{
	/// <summary>
	/// The Scarab Sultan's palette for light and sand. Not the rift's: no violet anywhere, the hot
	/// end runs white - pale yellow - gold - orange - ember red, and sand is lit by the world.
	/// </summary>
	internal static class SunPalette
	{
		public static readonly Color White = new(255, 252, 235);
		public static readonly Color Pale = new(255, 240, 170);
		public static readonly Color Gold = new(255, 200, 74);
		public static readonly Color Orange = new(255, 138, 36);
		public static readonly Color Ember = new(216, 74, 30);
		public static readonly Color Deep = new(138, 36, 36);
		public static readonly Color Sand = new(242, 212, 154);
		public static readonly Color SandDark = new(217, 168, 101);
		public static readonly Color SandShade = new(168, 115, 63);
		public static readonly Color Lapis = new(154, 216, 255);

		/// <summary>Down the hot ramp: 0 is white, 1 is ember red.</summary>
		public static Color Heat(float t)
		{
			t = MathHelper.Clamp(t, 0f, 1f);
			if (t < 0.25f)
				return Color.Lerp(White, Pale, t / 0.25f);
			if (t < 0.5f)
				return Color.Lerp(Pale, Gold, (t - 0.25f) / 0.25f);
			if (t < 0.75f)
				return Color.Lerp(Gold, Orange, (t - 0.5f) / 0.25f);
			return Color.Lerp(Orange, Ember, (t - 0.75f) / 0.25f);
		}

		public static Color Glow(Color c, float alpha)
		{
			Color o = c * alpha;
			o.A = 0;
			return o;
		}
	}

	/// <summary>
	/// Draw helpers the Sultan's projectiles share. Everything draws into whatever batch is open -
	/// the default premultiplied one, where a colour with A = 0 adds light.
	/// </summary>
	public static class SunDraw
	{
		private static Texture2D FlareTex => ParticleSystem.Flare;

		/// <summary>The soft flare, stretched: a ray, a smear of floor glow, a spike of a star.</summary>
		public static void Flare(SpriteBatch sb, Vector2 world, Color color, Vector2 sizePx, float rotation)
		{
			Texture2D t = FlareTex;
			sb.Draw(t, world - Main.screenPosition, null, color, rotation, t.Size() * 0.5f,
				sizePx / t.Width, SpriteEffects.None, 0f);
		}

		/// <summary>A flat MagicPixel run between two world points.</summary>
		public static void Line(SpriteBatch sb, Vector2 a, Vector2 b, Color color, float thick)
		{
			Vector2 seg = b - a;
			float len = seg.Length();
			if (len < 0.5f)
				return;
			sb.Draw(TextureAssets.MagicPixel.Value, a - Main.screenPosition, new Rectangle(0, 0, 1, 1), color,
				seg.ToRotation(), new Vector2(0f, 0.5f), new Vector2(len, thick), SpriteEffects.None, 0f);
		}

		/// <summary>
		/// A four-point star glint: a long spike, a shorter cross spike, a bloom and a white heart.
		/// <paramref name="size"/> is the long spike's length in px.
		/// </summary>
		public static void Star(SpriteBatch sb, Vector2 world, Color color, float size, float rotation, float alpha)
		{
			if (alpha <= 0f)
				return;
			Flare(sb, world, SunPalette.Glow(color, 0.5f * alpha), new Vector2(size * 0.55f), 0f);
			Flare(sb, world, SunPalette.Glow(color, alpha), new Vector2(size, size * 0.075f + 1.5f), rotation);
			Flare(sb, world, SunPalette.Glow(color, alpha * 0.85f), new Vector2(size * 0.6f, size * 0.07f + 1.5f), rotation + MathHelper.PiOver2);
			Flare(sb, world, SunPalette.Glow(SunPalette.White, alpha), new Vector2(size * 0.4f, size * 0.04f + 1f), rotation);
			Flare(sb, world, SunPalette.Glow(SunPalette.White, alpha * 0.9f), new Vector2(size * 0.18f), 0f);
		}

		/// <summary>
		/// The halo of a little sun, drawn BEHIND its sprite: three layers of bloom, cool to hot
		/// inward, and a corona of three sets of rays turning at their own rates and lengths, each
		/// ray breathing on its own phase - so the corona never repeats and never reads as a
		/// spinning wheel. <paramref name="radius"/> is the visible body's radius in px.
		/// </summary>
		public static void Halo(SpriteBatch sb, Vector2 world, float radius, float intensity, float time, float seed)
		{
			if (intensity <= 0f)
				return;
			float breathe = 1f + 0.06f * (float)Math.Sin(time * 4f + seed);
			Flare(sb, world, SunPalette.Glow(SunPalette.Ember, 0.22f * intensity), new Vector2(radius * 7.5f * breathe), 0f);
			Flare(sb, world, SunPalette.Glow(SunPalette.Orange, 0.38f * intensity), new Vector2(radius * 4.6f * breathe), 0f);
			Flare(sb, world, SunPalette.Glow(SunPalette.Pale, 0.5f * intensity), new Vector2(radius * 3f), 0f);

			Corona(sb, world, radius, intensity, time, seed, 4, 3.1f, 2.6f, 0.45f, 0.42f, SunPalette.Gold);
			Corona(sb, world, radius, intensity, time, seed + 1.3f, 6, 2.2f, 2.0f, -0.8f, 0.5f, SunPalette.Pale);
			Corona(sb, world, radius, intensity, time, seed + 2.9f, 3, 4.4f, 1.6f, 0.2f, 0.24f, SunPalette.Orange);
		}

		private static void Corona(SpriteBatch sb, Vector2 world, float radius, float intensity, float time, float seed,
			int count, float length, float width, float speed, float alpha, Color color)
		{
			for (int k = 0; k < count; k++)
			{
				float a = time * speed + MathHelper.Pi * k / count + seed;
				float len = radius * length * (1f + 0.22f * (float)Math.Sin(time * 2.7f + k * 1.9f + seed * 3f));
				Flare(sb, world, SunPalette.Glow(color, alpha * intensity), new Vector2(len * 2f, width * 2f), a);
			}
		}

		/// <summary>The heat on top of the sprite - a faint white wash so the body itself reads hot.</summary>
		public static void Core(SpriteBatch sb, Vector2 world, float radius, float intensity)
			=> Flare(sb, world, SunPalette.Glow(SunPalette.White, 0.28f * intensity), new Vector2(radius * 2.2f), 0f);

		/// <summary>
		/// The cracked sun's cracks, as polylines on the unit disc. Hand-placed rather than random so
		/// the ball's crack pattern is the same one every time - it is a character, not noise.
		/// </summary>
		private static readonly Vector2[][] Cracks = {
			new[] { new Vector2(-0.12f, -0.1f), new Vector2(0.22f, -0.38f), new Vector2(0.38f, -0.5f), new Vector2(0.62f, -0.62f) },
			new[] { new Vector2(-0.12f, -0.1f), new Vector2(-0.42f, 0.02f), new Vector2(-0.58f, 0.22f), new Vector2(-0.86f, 0.3f) },
			new[] { new Vector2(-0.12f, -0.1f), new Vector2(0.02f, 0.28f), new Vector2(-0.06f, 0.52f), new Vector2(0.04f, 0.84f) },
			new[] { new Vector2(0.02f, 0.28f), new Vector2(0.34f, 0.36f), new Vector2(0.62f, 0.5f) },
			new[] { new Vector2(-0.42f, 0.02f), new Vector2(-0.46f, -0.32f), new Vector2(-0.38f, -0.66f) },
		};

		/// <summary>
		/// Light leaking out of the cracks. <paramref name="open"/> runs 0..1 over the fuse and
		/// widens them; <paramref name="pulse"/> is 0..1 and is the heartbeat. Where each crack
		/// meets the rim a short beam of light escapes outward, growing as it opens.
		/// </summary>
		public static void CrackLight(SpriteBatch sb, Vector2 world, float radius, float rotation, float open, float pulse, float alpha)
		{
			if (alpha <= 0f)
				return;
			float thick = 1f + 2.6f * open + 1.2f * pulse;
			float bright = (0.45f + 0.35f * open + 0.3f * pulse) * alpha;
			foreach (Vector2[] crack in Cracks)
			{
				for (int i = 0; i < crack.Length - 1; i++)
				{
					Vector2 a = world + crack[i].RotatedBy(rotation) * radius;
					Vector2 b = world + crack[i + 1].RotatedBy(rotation) * radius;
					Line(sb, a, b, SunPalette.Glow(SunPalette.Orange, bright * 0.6f), thick * 2.4f);
					Line(sb, a, b, SunPalette.Glow(SunPalette.Pale, bright), thick);
					Line(sb, a, b, SunPalette.Glow(SunPalette.White, bright * 0.8f), Math.Max(1f, thick * 0.4f));
				}
				Vector2 tip = crack[crack.Length - 1];
				if (tip.Length() > 0.55f)
				{
					Vector2 dir = tip.RotatedBy(rotation).SafeNormalize(Vector2.UnitX);
					float len = 6f + 38f * open * (0.7f + 0.5f * pulse);
					Vector2 from = world + dir * radius * 0.85f;
					Flare(sb, from + dir * len * 0.5f, SunPalette.Glow(SunPalette.Gold, bright * 0.7f),
						new Vector2(len * 1.6f, 3f + 4f * open), dir.ToRotation());
				}
			}
		}
	}

	/// <summary>A heat mote: rises, wobbles on its own phase, and cools down the hot ramp as it goes.</summary>
	public class SunEmber : Particle
	{
		private readonly float phase = Main.rand.NextFloat(MathHelper.TwoPi);
		private readonly float wobble = Main.rand.NextFloat(0.6f, 1.4f);
		public float Lift = 0.035f;
		public float Heat;

		public SunEmber(Vector2 pos, Vector2 vel, float scale, int life, float heat = 0.2f)
		{
			Position = pos; Velocity = vel; Scale = scale; Lifetime = life; Heat = heat;
		}

		public override void Update()
		{
			Velocity.Y -= Lift;
			Velocity.X *= 0.96f;
			Velocity.Y *= 0.985f;
			Position += Velocity;
			Position.X += (float)Math.Sin(Time * 0.22f * wobble + phase) * 0.4f;
		}

		public override void Draw(SpriteBatch sb)
		{
			float fade = 1f - Progress;
			Color c = SunPalette.Heat(Heat + Progress * 0.8f);
			float s = Scale * (1f - Progress * 0.5f);
			SunDraw.Flare(sb, Position, SunPalette.Glow(c, 0.7f * fade), new Vector2(9f * s), 0f);
			sb.Draw(TextureAssets.MagicPixel.Value, Position - Main.screenPosition, new Rectangle(0, 0, 1, 1),
				SunPalette.Glow(Color.Lerp(SunPalette.White, c, Progress), fade), 0f, new Vector2(0.5f),
				Math.Max(1f, 2f * s), SpriteEffects.None, 0f);
		}
	}

	/// <summary>
	/// A grain of sand. Falls, and dies the moment it touches a solid tile - which is what lets a
	/// stream of them pour INTO a crack instead of piling on the floor. Paints, lit by the world.
	/// </summary>
	public class SandGrain : Particle
	{
		public float Gravity = 0.2f;

		public SandGrain(Vector2 pos, Vector2 vel, Color color, float scale, int life)
		{
			Position = pos; Velocity = vel; Color = color; Scale = scale; Lifetime = life; Glow = false;
		}

		public override void Update()
		{
			Velocity.Y = Math.Min(Velocity.Y + Gravity, 9f);
			Velocity.X *= 0.98f;
			Position += Velocity;
			if (Velocity.Y > 0f && Time > 2)
			{
				Point p = Position.ToTileCoordinates();
				if (WorldGen.InWorld(p.X, p.Y) && WorldGen.SolidTile(p.X, p.Y))
					Time = Lifetime;
			}
		}

		public override void Draw(SpriteBatch sb)
		{
			float fade = Progress < 0.75f ? 1f : 1f - (Progress - 0.75f) / 0.25f;
			Color lit = Lighting.GetColor(Position.ToTileCoordinates());
			Color c = Color.MultiplyRGB(Color.Lerp(lit, Color.White, 0.25f)) * fade;
			sb.Draw(TextureAssets.MagicPixel.Value, Position - Main.screenPosition, new Rectangle(0, 0, 1, 1), c, 0f,
				new Vector2(0.5f), new Vector2(2f * Scale, 2f * Scale + Math.Min(3f, Math.Abs(Velocity.Y) * 0.5f)),
				SpriteEffects.None, 0f);
		}
	}

	/// <summary>
	/// A scorch on the floor where the sun has rolled: a flat smear of light that starts gold and
	/// cools to ember red as it fades, so the floor behind a rolling sun reads as burnt.
	/// </summary>
	public class SunScorch : Particle
	{
		public SunScorch(Vector2 pos, float scale, int life)
		{
			Position = pos; Scale = scale; Lifetime = life;
		}

		public override void Draw(SpriteBatch sb)
		{
			float fade = (float)Math.Pow(1f - Progress, 1.4f);
			Color c = SunPalette.Heat(0.45f + Progress * 0.55f);
			SunDraw.Flare(sb, Position, SunPalette.Glow(c, 0.55f * fade), new Vector2(34f * Scale, 7f * Scale), 0f);
			if (Progress < 0.35f)
				SunDraw.Flare(sb, Position, SunPalette.Glow(SunPalette.Pale, 0.6f * (1f - Progress / 0.35f)),
					new Vector2(16f * Scale, 3f * Scale), 0f);
		}
	}

	/// <summary>A star glint that swells and dies - a jewel's gleam, a lance's tip, a kick's spark.</summary>
	public class SunGlint : Particle
	{
		public float Spin;

		public SunGlint(Vector2 pos, Color color, float size, int life, float rotation = 0f, float spin = 0f)
		{
			Position = pos; Color = color; Scale = size; Lifetime = life; Rotation = rotation; Spin = spin;
		}

		public override void Update()
		{
			base.Update();
			Rotation += Spin;
		}

		public override void Draw(SpriteBatch sb)
		{
			float k = (float)Math.Sin(Math.Min(1f, Progress * 3f) * MathHelper.PiOver2) * (1f - Progress);
			SunDraw.Star(sb, Position, Color, Scale * (0.6f + 0.4f * k), Rotation, k);
		}
	}

	/// <summary>
	/// One ray of a sunburst: shoots out from <see cref="Particle.Position"/> along its angle,
	/// tapering at both ends, and thins as it dies.
	/// </summary>
	public class SunRay : Particle
	{
		public float Length;
		public float Width;

		public SunRay(Vector2 origin, float angle, float length, float width, Color color, int life)
		{
			Position = origin; Rotation = angle; Length = length; Width = width; Color = color; Lifetime = life;
		}

		public override void Draw(SpriteBatch sb)
		{
			float grow = 1f - (float)Math.Pow(1f - Progress, 3);
			float len = Length * (0.25f + 0.75f * grow);
			float fade = 1f - Progress;
			Vector2 dir = Rotation.ToRotationVector2();
			Vector2 mid = Position + dir * len * 0.5f;
			SunDraw.Flare(sb, mid, SunPalette.Glow(Color, 0.8f * fade), new Vector2(len * 1.3f, Width * 2f * (1f - Progress * 0.6f)), Rotation);
			SunDraw.Flare(sb, mid, SunPalette.Glow(SunPalette.White, 0.6f * fade), new Vector2(len * 0.9f, Width * 0.7f), Rotation);
		}
	}

	/// <summary>
	/// A puff of a sand storm, circling a centre on a flattened orbit while it drifts outward.
	/// Paints sand; the flare gives it no hard edge.
	/// </summary>
	public class SandSwirl : Particle
	{
		public Vector2 Center;
		public float Angle;
		public float Radius;
		public float Spin;
		public float Expand;

		public SandSwirl(Vector2 center, float angle, float radius, float spin, float expand, Color color, float scale, int life)
		{
			Center = center; Angle = angle; Radius = radius; Spin = spin; Expand = expand; Color = color; Scale = scale;
			Lifetime = life; Glow = false;
			Position = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle) * 0.5f) * radius;
		}

		public override void Update()
		{
			Angle += Spin;
			Spin *= 0.99f;
			Radius += Expand;
			Expand *= 0.985f;
			Center.Y -= 0.15f;
			Position = Center + new Vector2((float)Math.Cos(Angle), (float)Math.Sin(Angle) * 0.5f) * Radius;
		}

		public override void Draw(SpriteBatch sb)
		{
			float fade = (float)Math.Sin(Progress * MathHelper.Pi) * 0.5f;
			Color lit = Lighting.GetColor(Position.ToTileCoordinates());
			Color c = Color.MultiplyRGB(Color.Lerp(lit, Color.White, 0.35f)) * fade;
			SunDraw.Flare(sb, Position, c, new Vector2(64f * Scale, 40f * Scale), Angle + MathHelper.PiOver2);
		}
	}

	/// <summary>Bursts the Sultan's set builds out of the particles above and in Particles.cs.</summary>
	public static class ScarabFX
	{
		/// <summary>
		/// A crack across a floor. Unlike <see cref="FX.Crack"/> it hugs the surface - a floor crack
		/// that wanders up into the air reads as lightning - and it is laid twice on the same
		/// segments: a dark split that paints, and a thin glowing core inside it, the light from
		/// below. It runs out both ways from <paramref name="at"/>.
		/// </summary>
		public static void FloorCrack(Vector2 at, float halfLength, int life, float thick, Color glow)
		{
			var segs = new List<(Vector2, Vector2, float)>();
			foreach (float side in new[] { -1f, 1f })
			{
				Vector2 p = at;
				float step = 7f;
				int n = Math.Max(2, (int)(halfLength / step));
				for (int i = 0; i < n; i++)
				{
					Vector2 q = new(p.X + side * step * Main.rand.NextFloat(0.8f, 1.2f),
						at.Y + Main.rand.NextFloat(-2.5f, 1.5f));
					float when = i / (float)n;
					segs.Add((p, q, when));
					// a short fork dipping into the floor, appearing just after the trunk passes
					if (i > 0 && Main.rand.NextBool(3))
					{
						Vector2 fork = q + new Vector2(side * Main.rand.NextFloat(3f, 8f), Main.rand.NextFloat(3f, 7f));
						segs.Add((q, fork, Math.Min(1f, when + 0.12f)));
					}
					p = q;
				}
			}
			ParticleSystem.Spawn(new CrackLine(segs, new Color(60, 30, 30), life, thick * 2.2f) { Glow = false });
			ParticleSystem.Spawn(new CrackLine(segs, glow, life, thick));
		}

		/// <summary>Sand thrown up off a floor: grains and dusty puffs, both ways.</summary>
		public static void SandKick(Vector2 floor, float size, int grains, int puffs)
		{
			for (int i = 0; i < grains; i++)
			{
				Color c = Main.rand.NextBool() ? SunPalette.Sand : SunPalette.SandDark;
				ParticleSystem.Spawn(new SandGrain(floor + new Vector2(Main.rand.NextFloat(-10f, 10f) * size, -3f),
					new Vector2(Main.rand.NextFloat(-3f, 3f) * size, -Main.rand.NextFloat(1.5f, 4.5f) * size), c,
					Main.rand.NextFloat(0.8f, 1.2f), Main.rand.Next(30, 50)));
			}
			for (int i = 0; i < puffs; i++)
			{
				float dir = Main.rand.NextBool() ? 1f : -1f;
				ParticleSystem.Spawn(new Smoke(floor + new Vector2(Main.rand.NextFloat(-8f, 8f) * size, -4f),
					new Vector2(dir * Main.rand.NextFloat(1f, 3f), -Main.rand.NextFloat(0.3f, 1.4f)) * size,
					SunPalette.Sand * 0.7f, Main.rand.NextFloat(0.18f, 0.3f) * size, Main.rand.Next(26, 40)));
			}
		}

		/// <summary>
		/// A sunburst: a white flash, a hot bloom, rings, rays at random lengths and sparks. Used by
		/// the cracked sun's burst (small), the phase-two roar and the death (large).
		/// </summary>
		public static void Sunburst(Vector2 at, float size, int rays, float rayOffset = -1f)
		{
			ParticleSystem.Spawn(new Bloom(at, SunPalette.White, 0.4f * size, 2.4f * size, 18));
			ParticleSystem.Spawn(new Bloom(at, SunPalette.Orange, 0.8f * size, 4f * size, 30));
			ParticleSystem.Spawn(new Shockwave(at, SunPalette.Gold, 0.1f * size, 2.4f * size, 24));
			ParticleSystem.Spawn(new Shockwave(at, SunPalette.Orange, 0.1f * size, 3.6f * size, 36));
			for (int i = 0; i < rays; i++)
			{
				float a = rayOffset >= 0f ? rayOffset + MathHelper.TwoPi * i / rays : Main.rand.NextFloat(MathHelper.TwoPi);
				float len = (rayOffset >= 0f ? 200f : Main.rand.NextFloat(110f, 260f)) * size;
				ParticleSystem.Spawn(new SunRay(at, a, len, Main.rand.NextFloat(5f, 8f) * size,
					Main.rand.NextBool() ? SunPalette.Gold : SunPalette.Pale, Main.rand.Next(20, 30)));
			}
			FX.Sparks(at, SunPalette.Gold, (int)(18 * size), 9f * size, 1.1f, 26, 0.05f);
			for (int i = 0; i < 8 * size; i++)
				ParticleSystem.Spawn(new SunEmber(at + Main.rand.NextVector2Circular(14f, 14f) * size,
					Main.rand.NextVector2Circular(3f, 3f) * size, Main.rand.NextFloat(0.8f, 1.3f), Main.rand.Next(40, 70)));
		}

		/// <summary>A ring of sand-storm puffs whirling out from a centre.</summary>
		public static void Sandstorm(Vector2 center, int count, float radius, float size)
		{
			for (int i = 0; i < count; i++)
			{
				float a = MathHelper.TwoPi * i / count + Main.rand.NextFloat(-0.2f, 0.2f);
				Color c = Main.rand.NextBool(3) ? SunPalette.SandShade : SunPalette.Sand;
				ParticleSystem.Spawn(new SandSwirl(center, a, radius * Main.rand.NextFloat(0.4f, 1f),
					Main.rand.NextFloat(0.07f, 0.13f), Main.rand.NextFloat(1.2f, 2.6f) * size, c,
					Main.rand.NextFloat(0.45f, 0.8f) * size, Main.rand.Next(50, 80)));
			}
		}
	}
}
