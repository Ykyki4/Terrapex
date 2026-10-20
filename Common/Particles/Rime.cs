using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Terrapex.Common.Particles
{
	/// <summary>
	/// The Rime's own particles and draw helpers - frost, not rift. Everything here is cosmetic and
	/// client-only, exactly like <see cref="ParticleSystem"/>, which carries and draws all of it.
	///
	/// The shared Bloom and Shockwave draw from RiftFlare and RiftRing, whose outer halo is pink:
	/// tinted cold, that halo comes out lavender. The Rime's versions draw from pure-white
	/// textures (Art/rime/rime_fx2.lua), so a tint here is exactly the colour asked for.
	///
	/// Glow rule as everywhere: the batch is premultiplied AlphaBlend, A = 0 adds light.
	/// </summary>
	public static class RimeFX
	{
		public static readonly Color Frost = new(150, 225, 255);
		public static readonly Color Pale = new(225, 248, 255);
		public static readonly Color Deep = new(44, 135, 192);
		public static readonly Color Snow = new(223, 229, 241);
		public static readonly Color SnowShade = new(179, 189, 217);
		public static readonly Color Ice = new(163, 233, 248);
		public static readonly Color Warm = new(255, 200, 110);
		public static readonly Color Crack = new(24, 40, 86);

		private static Texture2D Tex(string name)
			=> ModContent.Request<Texture2D>("Terrapex/Content/Projectiles/Rime/" + name, AssetRequestMode.ImmediateLoad).Value;

		public static Texture2D GlowTex => Tex("RimeGlow");
		public static Texture2D RingTex => Tex("RimeRing");
		public static Texture2D CrystalTex => Tex("RimeCrystal");
		public static Texture2D FlakeTex => Tex("RimeFlake");
		public static Texture2D StarTex => Tex("RimeStar");
		public static Texture2D WaveTex => Tex("RimeShockwave");

		public static Color Glow(Color c, float strength)
		{
			Color o = c * strength;
			o.A = 0;
			return o;
		}

		/// <summary>A stable 0..1 hash, so a drawn tell looks the same on every frame.</summary>
		public static float Hash(int a, int b)
		{
			unchecked
			{
				uint n = (uint)(a * 374761393 + b * 668265263);
				n = (n ^ (n >> 13)) * 1274126177u;
				n ^= n >> 16;
				return (n & 0xFFFF) / 65536f;
			}
		}

		// ------------------------------------------------------------------ draw helpers

		private static readonly Rectangle Px = new(0, 0, 1, 1);

		public static void Line(Vector2 a, Vector2 b, Color c, float thick)
		{
			Vector2 seg = b - a;
			float len = seg.Length();
			if (len < 0.5f)
				return;
			Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, a - Main.screenPosition, Px, c, seg.ToRotation(),
				new Vector2(0f, 0.5f), new Vector2(len, thick), SpriteEffects.None, 0f);
		}

		/// <summary>A streak thick at <paramref name="a"/> and thin at <paramref name="b"/>, in three steps.</summary>
		public static void Taper(Vector2 a, Vector2 b, Color c, float thick)
		{
			Line(a, Vector2.Lerp(a, b, 0.4f), c, thick);
			Line(Vector2.Lerp(a, b, 0.38f), Vector2.Lerp(a, b, 0.75f), c * 0.8f, thick * 0.62f);
			Line(Vector2.Lerp(a, b, 0.73f), b, c * 0.55f, thick * 0.32f);
		}

		public static void Soft(Vector2 world, Color c, float scale, float squash = 1f, float rot = 0f)
		{
			Texture2D t = GlowTex;
			Main.spriteBatch.Draw(t, world - Main.screenPosition, null, c, rot, t.Size() * 0.5f,
				new Vector2(scale, scale * squash), SpriteEffects.None, 0f);
		}

		public static void Ring(Vector2 world, Color c, float scale, float squash = 1f, float rot = 0f)
		{
			Texture2D t = RingTex;
			Main.spriteBatch.Draw(t, world - Main.screenPosition, null, c, rot, t.Size() * 0.5f,
				new Vector2(scale, scale * squash), SpriteEffects.None, 0f);
		}

		public static void Flake(Vector2 world, Color c, float scale, float rot)
		{
			Texture2D t = FlakeTex;
			Main.spriteBatch.Draw(t, world - Main.screenPosition, null, c, rot, t.Size() * 0.5f, scale, SpriteEffects.None, 0f);
		}

		public static void Star(Vector2 world, Color c, float scale, float rot = 0f)
		{
			Texture2D t = StarTex;
			Main.spriteBatch.Draw(t, world - Main.screenPosition, null, c, rot, t.Size() * 0.5f, scale, SpriteEffects.None, 0f);
		}

		/// <summary>A crystal of rime standing on <paramref name="foot"/>, lit by the world and rimmed in cold light.</summary>
		public static void Crystal(Vector2 foot, float sx, float sy, float rot, float glow, bool flip = false)
		{
			if (sy <= 0.02f)
				return;
			Texture2D t = CrystalTex;
			Vector2 origin = new(t.Width / 2f, t.Height);
			Color lit = Color.Lerp(Lighting.GetColor(foot.ToTileCoordinates()), Color.White, 0.45f);
			SpriteEffects fx = flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Main.spriteBatch.Draw(t, foot - Main.screenPosition, null, lit, rot, origin, new Vector2(sx, sy), fx, 0f);
			if (glow > 0f)
				Main.spriteBatch.Draw(t, foot - Main.screenPosition, null, Glow(Frost, glow), rot, origin,
					new Vector2(sx, sy) * 1.12f, fx, 0f);
		}

		/// <summary>Floor you can stand on - platforms included, which WorldGen.SolidTile leaves out.</summary>
		public static bool Floor(Vector2 world)
		{
			Point p = world.ToTileCoordinates();
			if (!WorldGen.InWorld(p.X, p.Y, 2))
				return false;
			Tile t = Main.tile[p.X, p.Y];
			return t.HasUnactuatedTile && (Main.tileSolid[t.TileType] || Main.tileSolidTop[t.TileType]);
		}

		/// <summary>
		/// The slam's tell: a frost crack running along the floor from <paramref name="origin"/>
		/// outward on <paramref name="side"/>, <paramref name="reach"/> px of it shown. It is laid
		/// from a hash of <paramref name="seed"/>, so it is the same crack every frame. Over a gap it
		/// is drawn dimmer but still drawn - it is a tell of where the wave goes, and the wave is told
		/// in full. Branches dive down into the ground face; light sits in the crack.
		/// </summary>
		public static void FloorCrack(Vector2 origin, int side, float reach, float f, int seed)
		{
			const float step = 12f;
			int n = (int)(reach / step) + 1;
			float pulse = 0.85f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 9f);
			Color haze = Glow(Frost, (0.14f + 0.22f * f) * pulse);
			Color core = Glow(Pale, (0.45f + 0.5f * f) * pulse);
			Color dark = Crack * (0.55f + 0.3f * f);
			Vector2 YAt(int k) => new(0f, 3f + (Hash(seed, k) - 0.5f) * 4f);

			for (int k = 0; k < n; k++)
			{
				float x0 = k * step, x1 = Math.Min(reach, (k + 1) * step);
				if (x1 <= x0)
					break;
				Vector2 a = origin + new Vector2(side * x0, 0f) + YAt(k);
				Vector2 b = origin + new Vector2(side * x1, 0f) + YAt(k + 1);
				bool ground = Floor(a + new Vector2(0f, 4f));
				float dim = ground ? 1f : 0.4f;
				Line(a, b, haze * dim, 6f);
				Line(a, b, dark * dim, 3f);
				Line(a, b, core * dim, 1.4f);

				// a branch down into the ground every few steps
				if (ground && Hash(seed, k * 3 + 1) < 0.34f)
				{
					float ang = 0.45f + Hash(seed, k * 3 + 2) * 0.8f;
					float len = 8f + Hash(seed, k * 5 + 7) * 16f;
					Vector2 d = new(side * (float)Math.Cos(ang), (float)Math.Sin(ang));
					Vector2 m = a + d * len * 0.55f;
					Vector2 e = m + d.RotatedBy(side * 0.35f) * len * 0.45f;
					Line(a, m, dark, 2f);
					Line(a, m, core * 0.7f, 1f);
					Line(m, e, core * 0.45f, 1f);
				}
			}

			// the running head of the crack
			Vector2 tip = origin + new Vector2(side * reach, 0f) + YAt(n);
			Soft(tip, Glow(Frost, 0.45f + 0.35f * f), 0.35f, 0.6f);
			Star(tip + new Vector2(0f, -2f), Glow(Pale, 0.6f + 0.4f * f), 0.5f + 0.3f * f * pulse);
		}

		// ------------------------------------------------------------------ bursts

		/// <summary>Something of ice breaks: a flash, a ring, chips, bits of crystal, cold streaks, vapour.</summary>
		public static void Shatter(Vector2 at, float size, int chips = 14)
		{
			ParticleSystem.Spawn(new RimeBloom(at, Pale, 0.6f * size, 1.7f * size, 14));
			ParticleSystem.Spawn(new RimeBloom(at, Frost, 1.2f * size, 2.6f * size, 22));
			ParticleSystem.Spawn(new RimeRing(at, Frost, 0.15f * size, 1.6f * size, 22));
			for (int i = 0; i < chips; i++)
			{
				Vector2 v = Main.rand.NextVector2Circular(5f, 5f) * size + new Vector2(0f, -2f);
				Color c = Main.rand.NextBool(3) ? Snow : (Main.rand.NextBool() ? Ice : Deep);
				ParticleSystem.Spawn(new Chip(at, v, c, Main.rand.NextFloat(0.8f, 1.5f) * size, Main.rand.Next(40, 70)));
			}
			for (int i = 0; i < chips / 3; i++)
				ParticleSystem.Spawn(new RimeIceBit(at, Main.rand.NextVector2Circular(4.5f, 4.5f) * size + new Vector2(0f, -2.5f),
					Main.rand.NextFloat(0.45f, 0.75f) * size, Main.rand.Next(45, 70)));
			for (int i = 0; i < chips * 2 / 3; i++)
			{
				Vector2 v = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 8f) * size;
				ParticleSystem.Spawn(new Spark(at, v, Frost, Main.rand.NextFloat(0.7f, 1.1f) * size, Main.rand.Next(12, 22), 0.05f));
			}
			for (int i = 0; i < 4 * size; i++)
				ParticleSystem.Spawn(new Smoke(at + Main.rand.NextVector2Circular(10f, 10f) * size, Main.rand.NextVector2Circular(1.5f, 1.5f),
					Pale * 0.35f, Main.rand.NextFloat(0.3f, 0.55f) * size, Main.rand.Next(30, 50), glow: true));
			ParticleSystem.Spawn(new RimeGlint(at + Main.rand.NextVector2Circular(8f, 8f), Pale, 0.9f * size, 16));
		}

		/// <summary>A puff of frozen breath out of a mouth, toward <paramref name="dir"/>.</summary>
		public static void Breath(Vector2 mouth, Vector2 dir, int count, float strength = 1f)
		{
			for (int i = 0; i < count; i++)
			{
				Vector2 v = dir.RotatedByRandom(0.45f) * Main.rand.NextFloat(1.5f, 4.5f) * strength;
				var s = ParticleSystem.Spawn(new Smoke(mouth + Main.rand.NextVector2Circular(6f, 6f), v, Pale * 0.4f,
					Main.rand.NextFloat(0.22f, 0.4f) * strength, Main.rand.Next(34, 56), glow: true));
				s.Grow = 1.03f;
			}
			for (int i = 0; i < count / 2; i++)
				ParticleSystem.Spawn(new RimeFlake(mouth, dir.RotatedByRandom(0.6f) * Main.rand.NextFloat(2f, 5f) * strength,
					Pale, Main.rand.NextFloat(0.5f, 0.8f), Main.rand.Next(40, 70), glow: true) { Gravity = 0.02f, Drag = 0.95f });
		}

		/// <summary>The slam on the floor: a squashed ring, snow thrown both ways, chips, and cracks driven into the ground.</summary>
		public static void FloorSlam(Vector2 floor, float size)
		{
			FX.GroundSlam(floor, Frost, Snow * 0.8f, size);
			ParticleSystem.Spawn(new RimeRing(floor, Pale, 0.3f * size, 3.4f * size, 30, 0.22f));
			ParticleSystem.Spawn(new RimeBloom(floor, Frost, 1f * size, 3f * size, 20, 0.35f));
			for (int i = 0; i < 22 * size; i++)
			{
				float dir = Main.rand.NextBool() ? 1f : -1f;
				Vector2 v = new(dir * Main.rand.NextFloat(1f, 6f), -Main.rand.NextFloat(2f, 7f));
				ParticleSystem.Spawn(new Chip(floor + new Vector2(Main.rand.NextFloat(-40f, 40f), -4f), v * size,
					Main.rand.NextBool(3) ? Ice : Snow, Main.rand.NextFloat(0.9f, 1.6f), Main.rand.Next(40, 70)));
			}
			for (int i = 0; i < 6; i++)
				ParticleSystem.Spawn(new RimeIceBit(floor + new Vector2(Main.rand.NextFloat(-30f, 30f), -6f),
					new Vector2(Main.rand.NextFloat(-4f, 4f), -Main.rand.NextFloat(4f, 8f)), Main.rand.NextFloat(0.6f, 0.9f), 60));
			for (int i = 0; i < 5; i++)
			{
				float a = MathHelper.Lerp(0.35f, MathHelper.Pi - 0.35f, i / 4f) + Main.rand.NextFloat(-0.15f, 0.15f);
				FX.Crack(floor + new Vector2(0f, 2f), a.ToRotationVector2(), Main.rand.NextFloat(36f, 64f) * size,
					Glow(Frost, 0.8f), 60, 1.6f, 1);
			}
		}

		/// <summary>A roar: rings off the head, a breath cone, a frost glint on the eye.</summary>
		public static void Roar(Vector2 head, Vector2 dir, float size)
		{
			ParticleSystem.Spawn(new RimeRing(head, Frost, 0.2f * size, 2.4f * size, 26));
			ParticleSystem.Spawn(new RimeRing(head, Pale, 0.1f * size, 1.6f * size, 20) { Delay = 6 });
			Breath(head + dir * 10f, dir, (int)(10 * size), 1.2f * size);
		}

		/// <summary>Snow loosened from a ceiling, sifting down in a column.</summary>
		public static void Sift(Vector2 at, float width)
		{
			ParticleSystem.Spawn(new RimeFlake(at + new Vector2(Main.rand.NextFloat(-width, width), 0f),
				new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(0.6f, 1.4f)), Snow,
				Main.rand.NextFloat(0.3f, 0.55f), Main.rand.Next(50, 90)) { Sway = 0.04f });
		}
	}

	// ====================================================================== particles

	/// <summary>A soft bloom from the white glow - a cold flash that swells and goes.</summary>
	public class RimeBloom : Particle
	{
		public float EndScale, Squash;

		public RimeBloom(Vector2 pos, Color color, float scale, float endScale, int life, float squash = 1f)
		{
			Position = pos; Color = color; Scale = scale; EndScale = endScale; Lifetime = life; Squash = squash;
		}

		public override void Draw(SpriteBatch sb)
		{
			float s = MathHelper.Lerp(Scale, EndScale, 1f - (1f - Progress) * (1f - Progress));
			float fade = (float)Math.Sin(Math.Min(1f, Progress * 5f) * MathHelper.PiOver2) * (1f - Progress);
			RimeFX.Soft(Position, Tint(fade), s, Squash);
		}
	}

	/// <summary>A ring from the white ring texture, growing and thinning. <see cref="Delay"/> holds it unseen.</summary>
	public class RimeRing : Particle
	{
		public float EndScale, Squash;
		public int Delay;

		public RimeRing(Vector2 pos, Color color, float scale, float endScale, int life, float squash = 1f)
		{
			Position = pos; Color = color; Scale = scale; EndScale = endScale; Lifetime = life; Squash = squash;
		}

		public override void Draw(SpriteBatch sb)
		{
			int t = Time - Delay;
			if (t < 0)
				return;
			float p = t / (float)Math.Max(1, Lifetime - Delay);
			float e = 1f - (float)Math.Pow(1f - p, 3);
			RimeFX.Ring(Position, Tint(1f - p), MathHelper.Lerp(Scale, EndScale, e), Squash);
		}
	}

	/// <summary>
	/// A snowflake. Drifts, sways and spins; fades in so a flake spawned on screen never pops.
	/// With glow it is cold light (breath, frost); without it is snow, lit by the world.
	/// </summary>
	public class RimeFlake : Particle
	{
		public float Gravity, Sway, Drag = 1f, Spin;
		private readonly float phase;

		public RimeFlake(Vector2 pos, Vector2 vel, Color color, float scale, int life, bool glow = false)
		{
			Position = pos; Velocity = vel; Color = color; Scale = scale; Lifetime = life; Glow = glow;
			Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
			Spin = Main.rand.NextFloat(-0.05f, 0.05f);
			phase = Main.rand.NextFloat(MathHelper.TwoPi);
		}

		public override void Update()
		{
			Velocity *= Drag;
			Velocity.Y += Gravity;
			Velocity.X += (float)Math.Sin(Time * 0.08f + phase) * Sway;
			Rotation += Spin;
			base.Update();
		}

		public override void Draw(SpriteBatch sb)
		{
			float a = Math.Min(1f, Progress * 6f) * Math.Min(1f, (1f - Progress) * 3f);
			Color c = Glow ? Tint(a) : Color.MultiplyRGB(Color.Lerp(Lighting.GetColor(Position.ToTileCoordinates()), Color.White, 0.55f)) * a;
			RimeFX.Flake(Position, c, Scale, Rotation);
		}
	}

	/// <summary>
	/// A streak of wind-driven snow: a short line along its velocity that keeps its speed, wobbles
	/// a little and fades in and out. Painted, not lit - a blizzard of light reads as a laser show.
	/// </summary>
	public class RimeStreak : Particle
	{
		public float Length = 2.2f, Thick = 1.6f, Wobble = 0.08f;
		private readonly float phase;

		public RimeStreak(Vector2 pos, Vector2 vel, Color color, float scale, int life)
		{
			Position = pos; Velocity = vel; Color = color; Scale = scale; Lifetime = life; Glow = false;
			phase = Main.rand.NextFloat(MathHelper.TwoPi);
		}

		public override void Update()
		{
			Velocity.Y += (float)Math.Sin(Time * 0.15f + phase) * Wobble;
			base.Update();
		}

		public override void Draw(SpriteBatch sb)
		{
			float a = Math.Min(1f, Progress * 5f) * Math.Min(1f, (1f - Progress) * 4f);
			float len = Velocity.Length() * Length * Scale;
			Vector2 back = Position - Velocity.SafeNormalize(Vector2.UnitX) * len;
			Color c = Color * a;
			RimeFX.Taper(Position, back, c, Thick * Scale);
		}
	}

	/// <summary>A four-point glint that pops and goes - light catching an edge of ice.</summary>
	public class RimeGlint : Particle
	{
		public RimeGlint(Vector2 pos, Color color, float scale, int life)
		{
			Position = pos; Color = color; Scale = scale; Lifetime = life;
			Rotation = Main.rand.NextFloat(-0.2f, 0.2f);
		}

		public override void Draw(SpriteBatch sb)
		{
			float a = (float)Math.Sin(Progress * MathHelper.Pi);
			RimeFX.Star(Position, Tint(a), Scale * (0.4f + 0.6f * a), Rotation + Progress * 0.6f);
			RimeFX.Soft(Position, Tint(a * 0.35f), Scale * 0.3f);
		}
	}

	/// <summary>A tumbling bit of crystal: falls, bounces once, goes. The textured cousin of <see cref="Chip"/>.</summary>
	public class RimeIceBit : Particle
	{
		public float Spin;
		private bool bounced;

		public RimeIceBit(Vector2 pos, Vector2 vel, float scale, int life)
		{
			Position = pos; Velocity = vel; Scale = scale; Lifetime = life; Glow = false; Color = Color.White;
			Spin = Main.rand.NextFloat(-0.35f, 0.35f);
			Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
		}

		public override void Update()
		{
			Velocity.Y += 0.28f;
			Velocity.X *= 0.98f;
			Rotation += Spin;
			Point below = (Position + Velocity).ToTileCoordinates();
			if (!bounced && Velocity.Y > 0f && WorldGen.InWorld(below.X, below.Y) && WorldGen.SolidTile(below.X, below.Y))
			{
				bounced = true;
				Velocity.Y *= -0.3f;
				Velocity.X *= 0.5f;
				Spin *= 0.4f;
			}
			Position += Velocity;
		}

		public override void Draw(SpriteBatch sb)
		{
			float fade = Progress < 0.7f ? 1f : 1f - (Progress - 0.7f) / 0.3f;
			Texture2D t = RimeFX.CrystalTex;
			Color lit = Color.Lerp(Lighting.GetColor(Position.ToTileCoordinates()), Color.White, 0.4f) * fade;
			sb.Draw(t, Position - Main.screenPosition, null, lit, Rotation, t.Size() * 0.5f, Scale, SpriteEffects.None, 0f);
		}
	}

	/// <summary>A drop of meltwater off an icicle's tip: stretches as it falls, splashes on the floor.</summary>
	public class RimeDrip : Particle
	{
		public RimeDrip(Vector2 pos, int life)
		{
			Position = pos; Lifetime = life; Glow = false; Color = new Color(170, 220, 255);
			Velocity = new Vector2(0f, 0.5f);
		}

		public override void Update()
		{
			Velocity.Y = Math.Min(Velocity.Y + 0.3f, 12f);
			base.Update();
			Point p = Position.ToTileCoordinates();
			if (WorldGen.InWorld(p.X, p.Y) && WorldGen.SolidTile(p.X, p.Y))
			{
				for (int i = 0; i < 3; i++)
					ParticleSystem.Spawn(new Spark(Position - new Vector2(0f, 4f), new Vector2(Main.rand.NextFloat(-1.6f, 1.6f),
						-Main.rand.NextFloat(0.8f, 2f)), new Color(170, 220, 255), 0.5f, 12, 0.15f) { Glow = false });
				Time = Lifetime;
			}
		}

		public override void Draw(SpriteBatch sb)
		{
			Color c = Color.MultiplyRGB(Color.Lerp(Lighting.GetColor(Position.ToTileCoordinates()), Color.White, 0.5f)) * 0.85f;
			float len = 2f + Velocity.Y * 0.9f;
			sb.Draw(TextureAssets.MagicPixel.Value, Position - Main.screenPosition, new Rectangle(0, 0, 1, 1), c, 0f,
				new Vector2(0.5f, 1f), new Vector2(2f, len), SpriteEffects.None, 0f);
			RimeFX.Soft(Position, RimeFX.Glow(RimeFX.Frost, 0.2f), 0.08f);
		}
	}

	/// <summary>
	/// A crystal of rime that grows out of the floor, stands, and shatters at the end of its life.
	/// <see cref="Wave"/> draws it from the slam wave's own spike sprite, so the wave reads as a
	/// wall of crystal raised and broken behind its front.
	/// </summary>
	public class RimeSpike : Particle
	{
		public float Height, GrowTicks = 6f;
		public bool Wave, Flip;
		public int Chips = 3;

		public RimeSpike(Vector2 foot, float width, float height, int life, float lean, bool wave = false)
		{
			Position = foot; Scale = width; Height = height; Lifetime = life; Rotation = lean; Wave = wave;
			Glow = false; Color = Color.White; Flip = Main.rand.NextBool();
		}

		public override void Update()
		{
			if (Time == Lifetime - 1)
			{
				Vector2 mid = Position - new Vector2(0f, (Wave ? 30f : 12f) * Height);
				for (int i = 0; i < Chips; i++)
					ParticleSystem.Spawn(new Chip(mid + Main.rand.NextVector2Circular(6f, 10f),
						new Vector2(Main.rand.NextFloat(-2.5f, 2.5f), -Main.rand.NextFloat(1f, 4f)),
						Main.rand.NextBool() ? RimeFX.Ice : RimeFX.Snow, Main.rand.NextFloat(0.7f, 1.2f), Main.rand.Next(30, 50)));
				if (Main.rand.NextBool(2))
					ParticleSystem.Spawn(new RimeIceBit(mid, new Vector2(Main.rand.NextFloat(-2f, 2f), -Main.rand.NextFloat(2f, 4f)),
						Wave ? 0.6f : 0.45f, 45));
				if (Main.rand.NextBool(3))
					ParticleSystem.Spawn(new RimeGlint(mid, RimeFX.Pale, 0.6f, 12));
			}
		}

		public override void Draw(SpriteBatch sb)
		{
			float grow = Math.Min(1f, Time / GrowTicks);
			grow = 1f - (1f - grow) * (1f - grow);
			float sy = Height * grow;
			float end = Progress > 0.85f ? (Progress - 0.85f) / 0.15f : 0f;   // it whitens just before it breaks
			if (!Wave)
			{
				RimeFX.Crystal(Position, Scale, sy, Rotation, 0.25f + 0.5f * end, Flip);
				return;
			}
			Texture2D t = RimeFX.WaveTex;
			Vector2 origin = new(t.Width / 2f, t.Height);
			Color lit = Color.Lerp(Lighting.GetColor(Position.ToTileCoordinates()), Color.White, 0.3f + 0.5f * end) * 0.92f;
			SpriteEffects fx = Flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			sb.Draw(t, Position - Main.screenPosition, null, lit, Rotation, origin, new Vector2(Scale, sy), fx, 0f);
			sb.Draw(t, Position - Main.screenPosition, null, RimeFX.Glow(RimeFX.Frost, 0.18f + 0.4f * end), Rotation, origin,
				new Vector2(Scale * 1.08f, sy * 1.04f), fx, 0f);
		}
	}

	/// <summary>A wide, faint streak of brighter air sweeping through the storm - a gust.</summary>
	public class RimeGust : Particle
	{
		public RimeGust(Vector2 pos, Vector2 vel, float scale, int life)
		{
			Position = pos; Velocity = vel; Scale = scale; Lifetime = life; Glow = false; Color = new Color(225, 240, 255);
		}

		public override void Draw(SpriteBatch sb)
		{
			float a = (float)Math.Sin(Progress * MathHelper.Pi) * 0.09f;
			float rot = Velocity.ToRotation();
			RimeFX.Soft(Position, Color * a, 3.2f * Scale, 0.12f, rot);
			RimeFX.Soft(Position - Velocity * 4f, Color * (a * 0.7f), 2.2f * Scale, 0.08f, rot);
		}
	}

	/// <summary>
	/// The blizzard's air: a cold veil over the whole screen and banks of fog drifting with the
	/// wind. One lives at a time. The boss feeds it every tick of phase two; when the feeding stops
	/// (the boss dies, or the player leaves) it thins out over its lifetime and goes on its own.
	/// </summary>
	public class RimeBlizzardFog : Particle
	{
		private static RimeBlizzardFog current;
		private uint lastUpdate;
		private float strength, target;
		private int dir = 1;
		private float drift;

		public static void Feed(int windDir, bool calm)
		{
			if (Main.dedServ)
				return;
			if (current == null || current.Dead || Main.GameUpdateCount - current.lastUpdate > 3)
			{
				current = ParticleSystem.Spawn(new RimeBlizzardFog());
				current.lastUpdate = Main.GameUpdateCount;
			}
			current.Time = 0;
			current.dir = windDir;
			current.target = calm ? 0.45f : 1f;
		}

		private RimeBlizzardFog()
		{
			Lifetime = 80; Glow = false; Color = new Color(170, 205, 240);
			Position = Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
		}

		public override void Update()
		{
			lastUpdate = Main.GameUpdateCount;
			strength = MathHelper.Lerp(strength, target * (1f - Progress), 0.04f);
			drift += dir * (target > 0.5f ? 3.2f : 0.8f);
			Position = Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
		}

		public override void Draw(SpriteBatch sb)
		{
			if (strength <= 0.01f)
				return;
			// the veil, a little past every edge so a zoom never shows its border
			sb.Draw(TextureAssets.MagicPixel.Value, new Vector2(-200f, -200f), new Rectangle(0, 0, 1, 1),
				Color * (0.09f * strength), 0f, Vector2.Zero, new Vector2(Main.screenWidth + 400f, Main.screenHeight + 400f),
				SpriteEffects.None, 0f);
			// banks of fog, low and high, rolling with the wind
			float w = Main.screenWidth + 900f;
			for (int i = 0; i < 4; i++)
			{
				float x = ((drift * (0.7f + i * 0.2f) + i * w / 4f) % w + w) % w - 450f;
				float y = Main.screenHeight * (i % 2 == 0 ? 0.78f : 0.3f) + (float)Math.Sin(drift * 0.004f + i) * 30f;
				RimeFX.Soft(Main.screenPosition + new Vector2(x, y), Color.White * (0.07f * strength), 11f, 0.35f);
			}
		}
	}
}
