using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace Terrapex.Common.Particles
{
	/// <summary>
	/// A curl of wind: a streak whose heading turns every tick, drawn from its own last few
	/// positions so it reads as a swirl rather than as a spark. The ranged tree's signature - the
	/// tailwind sheds them, the compass flash throws them out at the four points.
	/// </summary>
	public class Gust : Particle
	{
		private const int Keep = 9;
		private readonly Vector2[] past = new Vector2[Keep];
		private int filled;
		public float Curl;
		public float Thick = 1.6f;

		public Gust(Vector2 pos, Vector2 vel, Color color, float curl, int life, float thick = 1.6f)
		{
			Position = pos; Velocity = vel; Color = color; Curl = curl; Lifetime = life; Thick = thick;
		}

		public override void Update()
		{
			for (int i = Keep - 1; i > 0; i--)
				past[i] = past[i - 1];
			past[0] = Position;
			filled = Math.Min(Keep, filled + 1);
			Velocity = Velocity.RotatedBy(Curl) * 0.96f;
			base.Update();
		}

		public override void Draw(SpriteBatch sb)
		{
			float fade = (float)Math.Sin(Progress * MathHelper.Pi);
			Texture2D px = TextureAssets.MagicPixel.Value;
			Rectangle src = new(0, 0, 1, 1);
			Vector2 prev = Position;
			for (int i = 0; i < filled; i++)
			{
				Vector2 p = past[i];
				Vector2 seg = p - prev;
				float len = seg.Length();
				if (len > 0.1f)
				{
					float f = 1f - i / (float)Keep;
					sb.Draw(px, prev - Main.screenPosition, src, Tint(fade * f), seg.ToRotation(),
						new Vector2(0f, 0.5f), new Vector2(len + 0.5f, Thick * (0.4f + 0.6f * f)), SpriteEffects.None, 0f);
				}
				prev = p;
			}
		}
	}

	/// <summary>
	/// A drop of sea spray: a short solid streak that falls and fades, lit by the world. Not light -
	/// the spray wind is water, and water is the one wind that should not glow.
	/// </summary>
	public class Droplet : Particle
	{
		public Droplet(Vector2 pos, Vector2 vel, Color color, float scale, int life)
		{
			Position = pos; Velocity = vel; Color = color; Scale = scale; Lifetime = life; Glow = false;
		}

		public override void Update()
		{
			Velocity.Y += 0.18f;
			Velocity.X *= 0.97f;
			base.Update();
		}

		public override void Draw(SpriteBatch sb)
		{
			float fade = 1f - Progress * Progress;
			Color lit = Lighting.GetColor(Position.ToTileCoordinates());
			Color c = Color.MultiplyRGB(Color.Lerp(lit, Color.White, 0.35f)) * fade;
			float len = Math.Max(2f, Velocity.Length() * 1.2f) * Scale;
			sb.Draw(TextureAssets.MagicPixel.Value, Position - Main.screenPosition, new Rectangle(0, 0, 1, 1), c,
				Velocity.ToRotation(), new Vector2(0.5f), new Vector2(len, 2f * Scale), SpriteEffects.None, 0f);
		}
	}

	/// <summary>The ranged tree's one-call bursts.</summary>
	public static class WindFX
	{
		public static readonly Color SprayTeal = new(120, 225, 230);
		public static readonly Color SunGold = new(255, 196, 90);
		public static readonly Color FrostIce = new(170, 230, 255);
		public static readonly Color CleftPink = new(255, 120, 215);
		public static readonly Color Breeze = new(235, 248, 255);
		public static readonly Color Regard = new(255, 190, 110);

		public static Color Of(int wind) => wind switch
		{
			0 => SprayTeal,
			1 => SunGold,
			2 => FrostIce,
			_ => CleftPink,
		};

		/// <summary>The compass needle swinging: four gusts thrown out at the four points, one per wind.</summary>
		public static void CompassFlash(Vector2 at, float aim)
		{
			ParticleSystem.Spawn(new Shockwave(at, Breeze * 0.8f, 0.05f, 0.55f, 18));
			for (int i = 0; i < 4; i++)
			{
				float a = aim + i * MathHelper.PiOver2;
				Vector2 dir = a.ToRotationVector2();
				ParticleSystem.Spawn(new Spark(at, dir * 6f, Of(i), 1.1f, 18));
				ParticleSystem.Spawn(new Gust(at + dir * 6f, dir * 3.5f, Of(i), 0.12f, 26));
				ParticleSystem.Spawn(new Gust(at + dir * 6f, dir * 3.5f, Of(i), -0.12f, 26));
			}
			ParticleSystem.Spawn(new Bloom(at, Breeze, 0.3f, 0.7f, 14));
		}

		/// <summary>The moment an arrow catches the trade wind: a ring of curls around it and a puff.</summary>
		public static void Catch(Vector2 at, Vector2 heading)
		{
			ParticleSystem.Spawn(new Shockwave(at, Breeze * 0.7f, 0.05f, 0.45f, 16, 0.55f) { Rotation = heading.ToRotation() + MathHelper.PiOver2 });
			for (int i = 0; i < 6; i++)
			{
				float a = MathHelper.TwoPi * i / 6f;
				ParticleSystem.Spawn(new Gust(at, a.ToRotationVector2() * 2.5f + heading * 0.3f, Breeze,
					i % 2 == 0 ? 0.14f : -0.14f, 24));
			}
		}
	}
}
