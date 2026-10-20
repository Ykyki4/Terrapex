using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace Terrapex.Common.Particles
{
	/// <summary>
	/// The Old Shellback's own particles and bursts, on top of the shared layer in
	/// <c>Particles.cs</c>. Nested in one static class so the names (Splinter, MicroBubble...)
	/// cannot collide with another boss's file in the same namespace.
	///
	/// Everything is cosmetic and client-side; <see cref="ParticleSystem.Spawn"/> no-ops on a server.
	/// Colours come from <c>Art/shellback/sea.lua</c> so the effects sit in the sprite's own palette.
	/// </summary>
	public static class ShellbackFX
	{
		// ---- the palette of sea.lua
		public static readonly Color Outline = new(0x16, 0x1a, 0x2a);
		public static readonly Color SeaW = new(0xe8, 0xff, 0xf8), SeaL = new(0x9a, 0xee, 0xdd),
			SeaB = new(0x3f, 0xb8, 0xb0), SeaD = new(0x23, 0x7a, 0x86), SeaK = new(0x16, 0x48, 0x5a);
		public static readonly Color SandL = new(0xf8, 0xe6, 0xb4), SandB = new(0xe0, 0xc0, 0x84),
			SandD = new(0xb8, 0x93, 0x5e), SandK = new(0x86, 0x66, 0x3f);
		public static readonly Color WoodL = new(0xd8, 0xa8, 0x68), WoodB = new(0xa0, 0x70, 0x3f),
			WoodD = new(0x6e, 0x46, 0x32), WoodS = new(0x4a, 0x2e, 0x3a);
		public static readonly Color IronL = new(0xdd, 0xe6, 0xea), IronD = new(0x62, 0x74, 0x8a), IronK = new(0x3a, 0x44, 0x58);
		public static readonly Color CanvasL = new(0xf8, 0xec, 0xd0), CanvasD = new(0xa0, 0x8a, 0x6a);
		public static readonly Color BarnW = new(0xf6, 0xf2, 0xe4), BarnG = new(0x8f, 0x97, 0xa0);
		public static readonly Color Powder = new(255, 170, 90);
		public static readonly Color Soot = new(58, 52, 62);

		/// <summary>A painted colour dimmed by the light where it is, so sand smoke is not bright at night.</summary>
		public static Color Lit(Color c, Vector2 at) => Main.dedServ ? c : c.MultiplyRGB(Lighting.GetColor(at.ToTileCoordinates()));

		private static Color Pick(params Color[] cs) => cs[Main.rand.Next(cs.Length)];

		// ================================================================== particles

		/// <summary>
		/// A long tumbling fragment - plank splinter, rope end, sail rag. A <see cref="Chip"/> is a
		/// square and reads as gravel; a splinter has a length, so it reads as wood.
		/// </summary>
		public class Splinter : Particle
		{
			public float Spin, Length, Thick;
			public float Gravity = 0.22f;
			private bool bounced;

			public Splinter(Vector2 pos, Vector2 vel, Color color, float length, float thick, int life)
			{
				Position = pos; Velocity = vel; Color = color; Length = length; Thick = thick; Lifetime = life;
				Glow = false;
				Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
				Spin = Main.rand.NextFloat(-0.35f, 0.35f);
			}

			public override void Update()
			{
				Velocity.Y += Gravity;
				Velocity.X *= 0.98f;
				Rotation += Spin;
				Point below = (Position + Velocity).ToTileCoordinates();
				if (!bounced && Velocity.Y > 0f && WorldGen.InWorld(below.X, below.Y) && WorldGen.SolidTile(below.X, below.Y))
				{
					// lands flat and stays: a splinter does not bounce like a pebble
					bounced = true;
					Velocity *= 0.2f;
					Velocity.Y = -Math.Abs(Velocity.Y);
					Spin = 0f;
					Rotation = Main.rand.NextFloat(-0.25f, 0.25f);
					Gravity = 0f;
				}
				if (bounced)
					Velocity *= 0.8f;
				Position += Velocity;
			}

			public override void Draw(SpriteBatch sb)
			{
				float fade = Progress < 0.75f ? 1f : 1f - (Progress - 0.75f) / 0.25f;
				Color c = Color.MultiplyRGB(Lighting.GetColor(Position.ToTileCoordinates())) * fade;
				Texture2D px = TextureAssets.MagicPixel.Value;
				Rectangle src = new(0, 0, 1, 1);
				sb.Draw(px, Position - Main.screenPosition, src, c, Rotation, new Vector2(0.5f), new Vector2(Length, Thick),
					SpriteEffects.None, 0f);
				// the lit edge along one side
				if (Thick >= 2f)
					sb.Draw(px, Position - Main.screenPosition - new Vector2(0f, Thick * 0.25f).RotatedBy(Rotation), src,
						Color.Lerp(c, Color.White * fade, 0.35f), Rotation, new Vector2(0.5f),
						new Vector2(Length * 0.7f, 1f), SpriteEffects.None, 0f);
			}
		}

		/// <summary>A pinhead bubble that rises with a wobble and is gone. Trails the big bubbles.</summary>
		public class MicroBubble : Particle
		{
			private readonly float phase;

			public MicroBubble(Vector2 pos, Vector2 vel, float scale, int life)
			{
				Position = pos; Velocity = vel; Scale = scale; Lifetime = life; Color = SeaL;
				phase = Main.rand.NextFloat(MathHelper.TwoPi);
			}

			public override void Update()
			{
				Velocity *= 0.94f;
				Velocity.Y -= 0.04f;
				Position += Velocity + new Vector2(MathF.Sin(Time * 0.3f + phase) * 0.35f, 0f);
			}

			public override void Draw(SpriteBatch sb)
			{
				float fade = 1f - Progress * Progress;
				Texture2D flare = ParticleSystem.Flare;
				Vector2 at = Position - Main.screenPosition;
				sb.Draw(flare, at, null, Tint(0.55f * fade), 0f, flare.Size() * 0.5f, 0.09f * Scale, SpriteEffects.None, 0f);
				// a rim and one bright pixel: a bubble, not a mote of light
				Texture2D px = TextureAssets.MagicPixel.Value;
				float r = 2f * Scale;
				sb.Draw(px, at + new Vector2(-r * 0.5f), new Rectangle(0, 0, 1, 1), new Color(255, 255, 255, 0) * fade, 0f,
					new Vector2(0.5f), 1.5f, SpriteEffects.None, 0f);
				sb.Draw(px, at + new Vector2(r * 0.5f, r * 0.6f), new Rectangle(0, 0, 1, 1), SeaD * (0.7f * fade), 0f,
					new Vector2(0.5f), new Vector2(2f, 1f), SpriteEffects.None, 0f);
			}
		}

		/// <summary>
		/// A flat streak lying on the floor that fades slowly: skid marks, scuffed sand, the wet
		/// sheen a wave leaves. Painted (lit by the world) by default; with Glow it is a wet glint.
		/// </summary>
		public class GroundMark : Particle
		{
			public float Length, Thick;

			public GroundMark(Vector2 pos, float length, float thick, Color color, int life, bool glow = false)
			{
				Position = pos; Length = length; Thick = thick; Color = color; Lifetime = life; Glow = glow;
			}

			public override void Update() { }

			public override void Draw(SpriteBatch sb)
			{
				float fade = 1f - Progress;
				Color c = Glow ? Tint(fade) : Color.MultiplyRGB(Lighting.GetColor(Position.ToTileCoordinates())) * fade;
				float len = Length * (Glow ? 1f - Progress * 0.5f : 1f);
				sb.Draw(TextureAssets.MagicPixel.Value, Position - Main.screenPosition, new Rectangle(0, 0, 1, 1), c, 0f,
					new Vector2(0.5f, 0.5f), new Vector2(len, Thick), SpriteEffects.None, 0f);
			}
		}

		// ================================================================== bursts

		/// <summary>Water thrown out of a point: glinting droplets that arc and fall, plus foam.</summary>
		public static void Splash(Vector2 at, int count, float speed, float direction = -MathHelper.PiOver2,
			float spread = MathHelper.Pi, int foam = 0)
		{
			if (Main.dedServ)
				return;
			for (int i = 0; i < count; i++)
			{
				float a = direction + Main.rand.NextFloat(-spread * 0.5f, spread * 0.5f);
				Spark s = new(at, a.ToRotationVector2() * speed * Main.rand.NextFloat(0.4f, 1f),
					Main.rand.NextBool(3) ? SeaW : SeaL, Main.rand.NextFloat(0.6f, 1f), Main.rand.Next(18, 30), 0.2f);
				s.Drag = 0.98f;
				s.Length = 1.6f;
				ParticleSystem.Spawn(s);
			}
			for (int i = 0; i < foam; i++)
				ParticleSystem.Spawn(new Smoke(at + Main.rand.NextVector2Circular(8f, 4f),
					new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), -Main.rand.NextFloat(0.2f, 1.2f)),
					Lit(SeaW, at) * 0.8f, Main.rand.NextFloat(0.12f, 0.22f), Main.rand.Next(20, 34)));
		}

		/// <summary>Sand kicked up: a puff and a few grains, thrown along <paramref name="vel"/>.</summary>
		public static void SandKick(Vector2 at, Vector2 vel, int puffs, int grains)
		{
			if (Main.dedServ)
				return;
			for (int i = 0; i < puffs; i++)
				ParticleSystem.Spawn(new Smoke(at + Main.rand.NextVector2Circular(6f, 2f),
					vel * Main.rand.NextFloat(0.4f, 1f) + new Vector2(0f, -Main.rand.NextFloat(0.2f, 0.9f)),
					Lit(Pick(SandB, SandL), at) * 0.75f, Main.rand.NextFloat(0.14f, 0.26f), Main.rand.Next(26, 40)));
			for (int i = 0; i < grains; i++)
				ParticleSystem.Spawn(new Chip(at + Main.rand.NextVector2Circular(6f, 2f),
					vel * Main.rand.NextFloat(0.6f, 1.4f) + new Vector2(0f, -Main.rand.NextFloat(1.5f, 3.5f)),
					Pick(SandB, SandD, SandK), Main.rand.NextFloat(0.6f, 1f), Main.rand.Next(26, 40)));
		}

		/// <summary>Planks giving way: splinters of the wood ramp, a few barnacle chips among them.</summary>
		public static void Splinters(Vector2 at, int count, float speed, float spreadX = 8f, float spreadY = 8f)
		{
			if (Main.dedServ)
				return;
			for (int i = 0; i < count; i++)
			{
				Vector2 p = at + new Vector2(Main.rand.NextFloat(-spreadX, spreadX), Main.rand.NextFloat(-spreadY, spreadY));
				Vector2 v = Main.rand.NextVector2Circular(speed, speed) + new Vector2(0f, -speed * 0.45f);
				if (i % 5 == 4)
					ParticleSystem.Spawn(new Chip(p, v, Pick(BarnW, BarnG), Main.rand.NextFloat(0.7f, 1.1f), Main.rand.Next(40, 60)));
				else
					ParticleSystem.Spawn(new Splinter(p, v, Pick(WoodL, WoodB, WoodB, WoodD), Main.rand.NextFloat(4f, 9f),
						Main.rand.NextBool(3) ? 1f : 2f, Main.rand.Next(45, 70)));
			}
		}

		/// <summary>Black-powder smoke: dark, slow, swelling puffs.</summary>
		public static void PowderSmoke(Vector2 at, Vector2 dir, int count, float speed, float scale = 1f)
		{
			if (Main.dedServ)
				return;
			for (int i = 0; i < count; i++)
			{
				Smoke s = new(at + Main.rand.NextVector2Circular(6f, 6f),
					dir * Main.rand.NextFloat(0.3f, 1f) * speed + Main.rand.NextVector2Circular(1.2f, 1.2f) + new Vector2(0f, -0.4f),
					Lit(Main.rand.NextBool(3) ? IronD : Soot, at) * 0.85f, Main.rand.NextFloat(0.2f, 0.36f) * scale,
					Main.rand.Next(40, 64));
				s.Grow = 1.012f;
				ParticleSystem.Spawn(s);
			}
		}
	}
}
