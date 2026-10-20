using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Common.Particles
{
	/// <summary>
	/// A cheap, client-only particle layer - the thing vanilla dust cannot do. Dust is a fixed
	/// sprite per type with its own physics; a boss telegraph needs a spark that stretches along
	/// its own velocity, a ring that grows from a point of impact, a puff that swells and thins,
	/// a chip of ice that tumbles and lands. Calamity's GeneralParticleHandler is the model.
	///
	/// Particles are pure decoration: nothing here touches gameplay, nothing is synced, and on a
	/// server <see cref="Spawn"/> is a no-op. Spawn them from AI or from PreDraw freely.
	///
	/// They draw after dust, inside the game's own premultiplied AlphaBlend batch. So the glow
	/// rule from RiftDraw holds: a colour with A = 0 adds light; a colour with full alpha paints.
	/// Each particle's <see cref="Particle.Glow"/> picks which, so one system can carry both a
	/// white-hot spark and a dark smoke puff.
	/// </summary>
	public abstract class Particle
	{
		public Vector2 Position;
		public Vector2 Velocity;
		public Color Color;
		public float Scale = 1f;
		public float Rotation;
		public int Time;
		public int Lifetime = 30;
		public bool Glow = true;

		/// <summary>0 at birth, 1 at death.</summary>
		public float Progress => Time / (float)Lifetime;

		public bool Dead => Time >= Lifetime;

		public virtual void Update()
		{
			Position += Velocity;
		}

		public abstract void Draw(SpriteBatch sb);

		protected Color Tint(float alpha)
		{
			Color c = Color * alpha;
			if (Glow)
				c.A = 0;
			return c;
		}
	}

	public class ParticleSystem : ModSystem
	{
		private const int Max = 1500;
		private static readonly List<Particle> live = new();

		internal static Texture2D Flare => ModContent.Request<Texture2D>("Terrapex/Content/Projectiles/RiftFlare").Value;
		internal static Texture2D RingTex => ModContent.Request<Texture2D>("Terrapex/Content/Projectiles/RiftRing").Value;

		public static T Spawn<T>(T p) where T : Particle
		{
			if (Main.dedServ || live.Count >= Max)
				return p;
			live.Add(p);
			return p;
		}

		public override void Load()
		{
			if (!Main.dedServ)
				On_Main.DrawDust += DrawAfterDust;
		}

		public override void Unload() => live.Clear();

		public override void OnWorldUnload() => live.Clear();

		public override void PostUpdateDusts()
		{
			for (int i = live.Count - 1; i >= 0; i--)
			{
				Particle p = live[i];
				p.Update();
				p.Time++;
				if (p.Dead)
					live.RemoveAt(i);
			}
		}

		private static void DrawAfterDust(On_Main.orig_DrawDust orig, Main self)
		{
			orig(self);
			if (live.Count == 0)
				return;
			SpriteBatch sb = Main.spriteBatch;
			sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None,
				RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
			Rectangle view = new((int)Main.screenPosition.X - 200, (int)Main.screenPosition.Y - 200,
				Main.screenWidth + 400, Main.screenHeight + 400);
			foreach (Particle p in live)
				if (view.Contains(p.Position.ToPoint()))
					p.Draw(sb);
			sb.End();
		}
	}

	/// <summary>A streak stretched along its velocity, slowing and shrinking. Sparks, embers, spray.</summary>
	public class Spark : Particle
	{
		public float Drag = 0.92f;
		public float Gravity;
		public float Length = 3f;

		public Spark(Vector2 pos, Vector2 vel, Color color, float scale, int life, float gravity = 0f)
		{
			Position = pos; Velocity = vel; Color = color; Scale = scale; Lifetime = life; Gravity = gravity;
		}

		public override void Update()
		{
			base.Update();
			Velocity *= Drag;
			Velocity.Y += Gravity;
		}

		public override void Draw(SpriteBatch sb)
		{
			float fade = 1f - Progress;
			float len = Math.Max(2f, Velocity.Length() * Length) * Scale;
			Vector2 at = Position - Main.screenPosition;
			float rot = Velocity.ToRotation();
			Texture2D px = TextureAssets.MagicPixel.Value;
			Rectangle src = new(0, 0, 1, 1);
			sb.Draw(px, at, src, Tint(fade), rot, new Vector2(1f, 0.5f), new Vector2(len, 2.2f * Scale * fade + 0.6f), SpriteEffects.None, 0f);
			sb.Draw(px, at, src, new Color(255, 255, 255, 0) * (fade * 0.6f), rot, new Vector2(1f, 0.5f),
				new Vector2(len * 0.5f, Scale * fade), SpriteEffects.None, 0f);
		}
	}

	/// <summary>A soft flare that swells or shrinks. Muzzle flashes, impact cores, charge-ups.</summary>
	public class Bloom : Particle
	{
		public float EndScale;

		public Bloom(Vector2 pos, Color color, float scale, float endScale, int life, Vector2 vel = default)
		{
			Position = pos; Color = color; Scale = scale; EndScale = endScale; Lifetime = life; Velocity = vel;
		}

		public override void Draw(SpriteBatch sb)
		{
			float s = MathHelper.Lerp(Scale, EndScale, Progress);
			float fade = (float)Math.Sin(Math.Min(1f, Progress * 4f) * MathHelper.PiOver2) * (1f - Progress);
			Texture2D t = ParticleSystem.Flare;
			sb.Draw(t, Position - Main.screenPosition, null, Tint(fade), Rotation, t.Size() * 0.5f, s, SpriteEffects.None, 0f);
		}
	}

	/// <summary>A ring growing out of a point and thinning as it goes - the shockwave of an impact.</summary>
	public class Shockwave : Particle
	{
		public float EndScale;
		public float Squash = 1f;

		public Shockwave(Vector2 pos, Color color, float scale, float endScale, int life, float squash = 1f)
		{
			Position = pos; Color = color; Scale = scale; EndScale = endScale; Lifetime = life; Squash = squash;
		}

		public override void Draw(SpriteBatch sb)
		{
			float e = 1f - (float)Math.Pow(1f - Progress, 3);
			float s = MathHelper.Lerp(Scale, EndScale, e);
			Texture2D t = ParticleSystem.RingTex;
			sb.Draw(t, Position - Main.screenPosition, null, Tint(1f - Progress), Rotation, t.Size() * 0.5f,
				new Vector2(s, s * Squash), SpriteEffects.None, 0f);
		}
	}

	/// <summary>
	/// A puff that swells, drifts and thins. With <c>Glow = false</c> it is dust and smoke; with
	/// glow on it is steam or a heat haze. Drawn from the flare, so it has no hard edge.
	/// </summary>
	public class Smoke : Particle
	{
		public float Grow = 1.02f;
		public float Spin;

		public Smoke(Vector2 pos, Vector2 vel, Color color, float scale, int life, bool glow = false)
		{
			Position = pos; Velocity = vel; Color = color; Scale = scale; Lifetime = life; Glow = glow;
			Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
			Spin = Main.rand.NextFloat(-0.03f, 0.03f);
		}

		public override void Update()
		{
			base.Update();
			Velocity *= 0.95f;
			Scale *= Grow;
			Rotation += Spin;
		}

		public override void Draw(SpriteBatch sb)
		{
			float fade = (float)Math.Sin(Progress * MathHelper.Pi) * 0.8f;
			Texture2D t = ParticleSystem.Flare;
			sb.Draw(t, Position - Main.screenPosition, null, Tint(fade), Rotation, t.Size() * 0.5f,
				new Vector2(Scale, Scale * 0.8f), SpriteEffects.None, 0f);
		}
	}

	/// <summary>
	/// A small tumbling chip - ice, stone, shell, glass - that falls, bounces once off the floor and
	/// fades. Square, so it reads as a solid fragment rather than as light.
	/// </summary>
	public class Chip : Particle
	{
		public float Spin;
		private bool bounced;

		public Chip(Vector2 pos, Vector2 vel, Color color, float scale, int life)
		{
			Position = pos; Velocity = vel; Color = color; Scale = scale; Lifetime = life; Glow = false;
			Spin = Main.rand.NextFloat(-0.3f, 0.3f);
		}

		public override void Update()
		{
			Velocity.Y += 0.25f;
			Velocity.X *= 0.98f;
			Rotation += Spin;
			Point below = (Position + Velocity).ToTileCoordinates();
			if (!bounced && WorldGen.InWorld(below.X, below.Y) && WorldGen.SolidTile(below.X, below.Y) && Velocity.Y > 0f)
			{
				bounced = true;
				Velocity.Y *= -0.35f;
				Velocity.X *= 0.6f;
				Spin *= 0.5f;
			}
			Position += Velocity;
		}

		public override void Draw(SpriteBatch sb)
		{
			float fade = Progress < 0.7f ? 1f : 1f - (Progress - 0.7f) / 0.3f;
			Color lit = Lighting.GetColor(Position.ToTileCoordinates());
			Color c = Color.MultiplyRGB(lit) * fade;
			sb.Draw(TextureAssets.MagicPixel.Value, Position - Main.screenPosition, new Rectangle(0, 0, 1, 1), c, Rotation,
				new Vector2(0.5f), new Vector2(3f * Scale, 2f * Scale), SpriteEffects.None, 0f);
		}
	}

	/// <summary>
	/// A crack running across a surface, drawn as a branching line that grows to its full length
	/// over its first half and then fades. <see cref="FX.Crack"/> builds the branch tree once; the
	/// particle only animates how much of it shows.
	/// </summary>
	public class CrackLine : Particle
	{
		private readonly List<(Vector2 a, Vector2 b, float at)> segments;
		public float Thick = 2f;

		public CrackLine(List<(Vector2, Vector2, float)> segs, Color color, int life, float thick = 2f)
		{
			segments = segs; Color = color; Lifetime = life; Thick = thick;
			Position = segs.Count > 0 ? segs[0].Item1 : Vector2.Zero;
		}

		public override void Draw(SpriteBatch sb)
		{
			float grow = Math.Min(1f, Progress * 2f);
			float fade = Progress < 0.5f ? 1f : 1f - (Progress - 0.5f) * 2f;
			Texture2D px = TextureAssets.MagicPixel.Value;
			foreach (var (a, b, at) in segments)
			{
				if (at > grow)
					continue;
				Vector2 seg = b - a;
				float t = Math.Min(1f, (grow - at) * 6f);
				sb.Draw(px, a - Main.screenPosition, new Rectangle(0, 0, 1, 1), Tint(fade), seg.ToRotation(),
					new Vector2(0f, 0.5f), new Vector2(seg.Length() * t, Thick), SpriteEffects.None, 0f);
			}
		}
	}

	/// <summary>One-call bursts built out of the particles above, so effects stay consistent.</summary>
	public static class FX
	{
		/// <summary>A radial burst of sparks around a point.</summary>
		public static void Sparks(Vector2 at, Color color, int count, float speed, float scale = 1f, int life = 24,
			float gravity = 0f, float spread = MathHelper.TwoPi, float direction = 0f)
		{
			for (int i = 0; i < count; i++)
			{
				float a = direction + Main.rand.NextFloat(-spread * 0.5f, spread * 0.5f);
				Vector2 v = a.ToRotationVector2() * speed * Main.rand.NextFloat(0.4f, 1f);
				ParticleSystem.Spawn(new Spark(at, v, color, scale * Main.rand.NextFloat(0.7f, 1.2f),
					(int)(life * Main.rand.NextFloat(0.7f, 1.2f)), gravity));
			}
		}

		/// <summary>An impact: a hot core, a ring and sparks - the default "something landed here".</summary>
		public static void Impact(Vector2 at, Color hot, Color ring, float size = 1f)
		{
			ParticleSystem.Spawn(new Bloom(at, hot, 0.5f * size, 1.4f * size, 16));
			ParticleSystem.Spawn(new Shockwave(at, ring, 0.1f * size, 1.1f * size, 22));
			Sparks(at, hot, (int)(10 * size), 7f * size, size, 22);
		}

		/// <summary>A floor-hugging shockwave: a squashed ring plus dust thrown up both ways.</summary>
		public static void GroundSlam(Vector2 floor, Color ring, Color dust, float size = 1f)
		{
			ParticleSystem.Spawn(new Shockwave(floor, ring, 0.2f * size, 2.2f * size, 26, 0.28f));
			for (int i = 0; i < 14 * size; i++)
			{
				float dir = Main.rand.NextBool() ? 1f : -1f;
				ParticleSystem.Spawn(new Smoke(floor + new Vector2(Main.rand.NextFloat(-20f, 20f) * size, -4f),
					new Vector2(dir * Main.rand.NextFloat(2f, 6f), -Main.rand.NextFloat(0.5f, 2.5f)) * size,
					dust, Main.rand.NextFloat(0.25f, 0.45f) * size, Main.rand.Next(30, 50)));
			}
		}

		/// <summary>
		/// Builds a branching crack from <paramref name="from"/> heading along <paramref name="dir"/>
		/// for <paramref name="length"/> px and spawns it. Branches split off at random angles and
		/// grow later than the trunk, so the crack visibly *runs*.
		/// </summary>
		public static void Crack(Vector2 from, Vector2 dir, float length, Color color, int life = 50,
			float thick = 2f, int branches = 3)
		{
			var segs = new List<(Vector2, Vector2, float)>();
			void Run(Vector2 a, Vector2 d, float len, float startAt, int depth)
			{
				float step = 14f;
				int n = Math.Max(1, (int)(len / step));
				Vector2 p = a;
				for (int i = 0; i < n; i++)
				{
					d = d.RotatedBy(Main.rand.NextFloat(-0.45f, 0.45f));
					Vector2 q = p + d * step;
					float at = startAt + i / (float)n * (1f - startAt) * (depth == 0 ? 1f : 0.6f);
					segs.Add((p, q, at));
					if (depth < 2 && Main.rand.NextFloat() < branches / (float)n)
						Run(q, d.RotatedBy(Main.rand.NextBool() ? 0.8f : -0.8f), len * 0.35f, at, depth + 1);
					p = q;
				}
			}
			Run(from, dir.SafeNormalize(Vector2.UnitX), length, 0f, 0);
			ParticleSystem.Spawn(new CrackLine(segs, color, life, thick));
		}
	}
}
