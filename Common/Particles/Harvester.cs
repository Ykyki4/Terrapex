using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace Terrapex.Common.Particles
{
	/// <summary>
	/// Particle types owned by the Hollow Harvester and the Dormant Eye. They sit beside the shared
	/// set in Particles.cs rather than inside it, so the two bosses can grow their own vocabulary
	/// without touching the file every other effect depends on.
	///
	/// Everything matte here (straw, feathers, chaff) is lit by the world, the way a Chip is: a
	/// piece of straw is not a light source, and at night it should read as a dark sliver.
	/// <see cref="Particle.Glow"/> stays on only for <see cref="Mote"/>.
	/// </summary>
	internal static class HarvestPalette
	{
		public static readonly Color StrawLight = new(0xff, 0xf0, 0xc8);
		public static readonly Color Straw = new(0xe8, 0xcf, 0x94);
		public static readonly Color StrawMid = new(0xc0, 0xa0, 0x66);
		public static readonly Color StrawDark = new(0x8a, 0x6b, 0x45);
		public static readonly Color Ghost = new(190, 236, 200);
		public static readonly Color Ember = new(0xf0, 0xc0, 0x7a);
		public static readonly Color Pumpkin = new(255, 140, 50);
		public static readonly Color Crow = new(0x3a, 0x1c, 0x33);
		public static readonly Color CrowLit = new(0x63, 0x54, 0x78);

		public static Color RandomStraw() => Main.rand.Next(4) switch
		{
			0 => StrawLight,
			1 => Straw,
			2 => StrawMid,
			_ => StrawDark,
		};

		/// <summary>
		/// World light, but never all the way to black: the boss fights at night on the surface,
		/// and straw that vanishes the moment it leaves a torch is not worth spawning.
		/// </summary>
		public static Color Lit(Vector2 world, Color c, float floor = 0.35f)
		{
			Vector3 l = Lighting.GetColor(world.ToTileCoordinates()).ToVector3();
			l = Vector3.Max(l, new Vector3(floor));
			return new Color(c.ToVector3() * l);
		}
	}

	/// <summary>
	/// A cut stalk of straw: a short two-tone stick that tumbles, falls and lies still for a moment.
	/// Longer than a <see cref="Chip"/> and spun about its middle, so it reads as straw, not stone.
	/// </summary>
	public class Straw : Particle
	{
		public float Spin;
		public float Length;
		public float Gravity = 0.16f;

		public Straw(Vector2 pos, Vector2 vel, Color color, float length, int life)
		{
			Position = pos; Velocity = vel; Color = color; Length = length; Lifetime = life; Glow = false;
			Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
			Spin = Main.rand.NextFloat(-0.25f, 0.25f);
		}

		public override void Update()
		{
			Velocity.X *= 0.96f;
			Velocity.Y = Math.Min(Velocity.Y * 0.98f + Gravity, 3.5f);
			Rotation += Spin * (0.4f + Velocity.Length() * 0.2f);
			Position += Velocity;
		}

		public override void Draw(SpriteBatch sb)
		{
			float fade = Progress < 0.75f ? 1f : 1f - (Progress - 0.75f) / 0.25f;
			Texture2D px = TextureAssets.MagicPixel.Value;
			Rectangle src = new(0, 0, 1, 1);
			Vector2 at = Position - Main.screenPosition;
			Color body = HarvestPalette.Lit(Position, Color) * fade;
			Color edge = HarvestPalette.Lit(Position, HarvestPalette.StrawDark) * fade;
			sb.Draw(px, at, src, edge, Rotation, new Vector2(0.5f), new Vector2(Length, 2f), SpriteEffects.None, 0f);
			sb.Draw(px, at, src, body, Rotation, new Vector2(0.5f), new Vector2(Length - 2f, 1f), SpriteEffects.None, 0f);
		}
	}

	/// <summary>
	/// A crow feather. Falls slowly and rocks from side to side as it goes - the sway is what tells
	/// a feather from a chip, so it is the one thing this class must not lose.
	/// </summary>
	public class Feather : Particle
	{
		private readonly float phase;

		public Feather(Vector2 pos, Vector2 vel, float scale, int life)
		{
			Position = pos; Velocity = vel; Scale = scale; Lifetime = life; Glow = false;
			Color = HarvestPalette.Crow;
			phase = Main.rand.NextFloat(MathHelper.TwoPi);
		}

		public override void Update()
		{
			Velocity *= 0.93f;
			Velocity.Y += 0.035f;
			float sway = (float)Math.Sin(Time * 0.16f + phase);
			Position += Velocity + new Vector2(sway * 0.7f, 0f);
			Rotation = sway * 0.7f + MathHelper.PiOver2 * 0.3f;
		}

		public override void Draw(SpriteBatch sb)
		{
			float fade = Progress < 0.7f ? 1f : 1f - (Progress - 0.7f) / 0.3f;
			Texture2D px = TextureAssets.MagicPixel.Value;
			Rectangle src = new(0, 0, 1, 1);
			Vector2 at = Position - Main.screenPosition;
			sb.Draw(px, at, src, HarvestPalette.Lit(Position, Color, 0.5f) * fade, Rotation, new Vector2(0.5f),
				new Vector2(6f * Scale, 2.6f * Scale), SpriteEffects.None, 0f);
			// the rachis: one lit line down the middle, so the dark vane has a shape
			sb.Draw(px, at, src, HarvestPalette.Lit(Position, HarvestPalette.CrowLit, 0.5f) * fade, Rotation,
				new Vector2(0.5f), new Vector2(6.5f * Scale, 0.8f), SpriteEffects.None, 0f);
		}
	}

	/// <summary>A speck of chaff drifting on the wind - two pixels and a sway, nothing more.</summary>
	public class Chaff : Particle
	{
		private readonly float phase;

		public Chaff(Vector2 pos, Vector2 vel, Color color, int life)
		{
			Position = pos; Velocity = vel; Color = color; Lifetime = life; Glow = false;
			phase = Main.rand.NextFloat(MathHelper.TwoPi);
		}

		public override void Update()
		{
			Velocity *= 0.985f;
			Velocity.Y += 0.008f;
			Position += Velocity + new Vector2(0f, (float)Math.Sin(Time * 0.09f + phase) * 0.35f);
		}

		public override void Draw(SpriteBatch sb)
		{
			float fade = (float)Math.Sin(Progress * MathHelper.Pi);
			sb.Draw(TextureAssets.MagicPixel.Value, Position - Main.screenPosition, new Rectangle(0, 0, 1, 1),
				HarvestPalette.Lit(Position, Color, 0.45f) * fade, Velocity.ToRotation(), new Vector2(0.5f),
				new Vector2(2.5f, 1.6f), SpriteEffects.None, 0f);
		}
	}

	/// <summary>
	/// A point of light drawn toward a target and swallowed by it - the charge-up of anything that
	/// gathers before it strikes. It accelerates as it closes, so a ring of motes reads as being
	/// pulled in rather than drifting, and it dies on arrival instead of overshooting.
	/// </summary>
	public class Mote : Particle
	{
		public Vector2 Target;
		public float Pull;

		public Mote(Vector2 pos, Vector2 target, Color color, float scale, int life, float pull = 0.6f)
		{
			Position = pos; Target = target; Color = color; Scale = scale; Lifetime = life; Pull = pull;
			Velocity = (target - pos).RotatedBy(Main.rand.NextFloat(-1.2f, 1.2f)).SafeNormalize(Vector2.Zero) * 0.6f;
		}

		public override void Update()
		{
			Vector2 to = Target - Position;
			float d = to.Length();
			if (d < 6f)
			{
				Time = Lifetime;
				return;
			}
			Velocity = Vector2.Lerp(Velocity, to / d * Math.Min(d, 4f + Progress * 10f), 0.12f + Pull * 0.1f);
			Position += Velocity;
		}

		public override void Draw(SpriteBatch sb)
		{
			float fade = Math.Min(1f, Progress * 5f);
			Texture2D px = TextureAssets.MagicPixel.Value;
			Vector2 at = Position - Main.screenPosition;
			float len = Math.Max(2f, Velocity.Length() * 2.2f) * Scale;
			sb.Draw(px, at, new Rectangle(0, 0, 1, 1), Tint(fade), Velocity.ToRotation(), new Vector2(1f, 0.5f),
				new Vector2(len, 2f * Scale), SpriteEffects.None, 0f);
			sb.Draw(ParticleSystem.Flare, at, null, Tint(fade * 0.35f), 0f, ParticleSystem.Flare.Size() * 0.5f,
				0.12f * Scale, SpriteEffects.None, 0f);
		}
	}

	/// <summary>Bursts the two bosses share that Particles.cs has no matte version of.</summary>
	public static class HarvestFX
	{
		/// <summary>
		/// <see cref="FX.Crack"/>, but painted instead of lit: a dark fissure in stone. The shared
		/// helper always spawns a glowing crack, and a glowing crack in a ceiling is a light leak,
		/// which is the Eye's own signal and must not be spent on the rockfall.
		/// </summary>
		public static void MatteCrack(Vector2 from, Vector2 dir, float length, Color color, int life = 50, float thick = 2f)
		{
			var segs = new System.Collections.Generic.List<(Vector2, Vector2, float)>();
			void Run(Vector2 a, Vector2 d, float len, float startAt, int depth)
			{
				const float step = 8f;
				int n = Math.Max(1, (int)(len / step));
				Vector2 p = a;
				for (int i = 0; i < n; i++)
				{
					d = d.RotatedBy(Main.rand.NextFloat(-0.5f, 0.5f));
					Vector2 q = p + d * step;
					float at = startAt + i / (float)n * (1f - startAt) * (depth == 0 ? 1f : 0.6f);
					segs.Add((p, q, at));
					if (depth < 1 && Main.rand.NextBool(3))
						Run(q, d.RotatedBy(Main.rand.NextBool() ? 0.9f : -0.9f), len * 0.4f, at, depth + 1);
					p = q;
				}
			}
			Run(from, dir.SafeNormalize(Vector2.UnitX), length, 0f, 0);
			ParticleSystem.Spawn(new CrackLine(segs, color, life, thick) { Glow = false });
		}

		/// <summary>Stone thrown off a point: tumbling chips plus a low puff of grit. No light at all.</summary>
		public static void Rubble(Vector2 at, Color[] ramp, int chips, float speed, float size = 1f, float upward = 2f)
		{
			for (int i = 0; i < chips; i++)
			{
				Vector2 v = Main.rand.NextVector2Circular(speed, speed) - new Vector2(0f, Main.rand.NextFloat(upward));
				ParticleSystem.Spawn(new Chip(at + Main.rand.NextVector2Circular(6f, 6f) * size, v,
					ramp[Main.rand.Next(ramp.Length)], Main.rand.NextFloat(0.8f, 1.6f) * size, Main.rand.Next(40, 70)));
			}
			for (int i = 0; i < chips / 2 + 1; i++)
				ParticleSystem.Spawn(new Smoke(at + Main.rand.NextVector2Circular(10f, 6f) * size,
					Main.rand.NextVector2Circular(1.6f, 1.2f) * size, ramp[Main.rand.Next(ramp.Length)] * 0.8f,
					Main.rand.NextFloat(0.18f, 0.32f) * size, Main.rand.Next(30, 50)));
		}

		/// <summary>A floor-hugging dust ring for something heavy landing: <see cref="FX.GroundSlam"/> without the light.</summary>
		public static void MatteSlam(Vector2 floor, Color dust, float size = 1f)
		{
			ParticleSystem.Spawn(new Shockwave(floor, dust * 0.7f, 0.15f * size, 1.4f * size, 24, 0.25f) { Glow = false });
			for (int i = 0; i < 10 * size; i++)
			{
				float dir = Main.rand.NextBool() ? 1f : -1f;
				ParticleSystem.Spawn(new Smoke(floor + new Vector2(Main.rand.NextFloat(-18f, 18f) * size, -4f),
					new Vector2(dir * Main.rand.NextFloat(1.5f, 5f), -Main.rand.NextFloat(0.4f, 2f)) * size,
					dust, Main.rand.NextFloat(0.22f, 0.4f) * size, Main.rand.Next(30, 50)));
			}
		}
	}

	/// <summary>
	/// A crescent slash left hanging in the air: an arc that is drawn out along its sweep, thickest
	/// in the middle and pointed at both ends, then thins away. The shape of a scythe's cut, so the
	/// Harvester's marks finish with the same mark a reaper leaves.
	/// </summary>
	public class Crescent : Particle
	{
		public float Radius, Start, Sweep, Thick;
		public Color Core;

		public Crescent(Vector2 center, float radius, float start, float sweep, float thick, Color color, Color core, int life)
		{
			Position = center; Radius = radius; Start = start; Sweep = sweep; Thick = thick;
			Color = color; Core = core; Lifetime = life;
		}

		public override void Draw(SpriteBatch sb)
		{
			float grow = Math.Min(1f, Progress * 3.5f);
			float fade = Progress < 0.35f ? 1f : 1f - (Progress - 0.35f) / 0.65f;
			Draw(sb, Position, Radius + Progress * 6f, Start, Sweep * grow, Thick * (1f - Progress * 0.6f),
				Tint(fade), WithGlow(Core, fade));
		}

		private Color WithGlow(Color c, float a)
		{
			Color o = c * a;
			o.A = 0;
			return o;
		}

		/// <summary>The crescent shape on its own, so a projectile can draw one without a particle.</summary>
		public static void Draw(SpriteBatch sb, Vector2 center, float radius, float start, float sweep, float thick,
			Color body, Color core, int segments = 16)
		{
			if (Math.Abs(sweep) < 0.01f)
				return;
			Texture2D px = TextureAssets.MagicPixel.Value;
			Rectangle src = new(0, 0, 1, 1);
			Vector2 prev = center + start.ToRotationVector2() * radius;
			for (int i = 1; i <= segments; i++)
			{
				float t = i / (float)segments;
				float a = start + sweep * t;
				Vector2 p = center + a.ToRotationVector2() * radius;
				float w = (float)Math.Sin(t * MathHelper.Pi) * thick + 0.6f;
				Vector2 seg = p - prev;
				float rot = seg.ToRotation();
				Vector2 at = prev - Main.screenPosition;
				sb.Draw(px, at, src, body, rot, new Vector2(0f, 0.5f), new Vector2(seg.Length() + 1f, w), SpriteEffects.None, 0f);
				if (core.PackedValue != 0u)
					sb.Draw(px, at, src, core, rot, new Vector2(0f, 0.5f), new Vector2(seg.Length() + 1f, w * 0.35f),
						SpriteEffects.None, 0f);
				prev = p;
			}
		}
	}
}
