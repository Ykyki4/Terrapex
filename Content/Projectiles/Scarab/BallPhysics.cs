using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Terrapex.Content.Projectiles.Scarab
{
	/// <summary>
	/// The one set of rules every rolling ball in the Scarab Sultan's set obeys - the Sultan's sun,
	/// the Sand Roller's ball and the player's own sun pebbles.
	///
	/// It is a plain step function rather than Terraria's own tile collision for one reason: the
	/// boss draws a ball's whole path before it kicks it, and that drawing is only honest if it is
	/// the same arithmetic the ball then runs. <see cref="Predict"/> walks the same
	/// <see cref="Step"/> the projectile walks, so the dotted line on the floor is where the ball
	/// will actually go.
	///
	/// A projectile that uses this keeps its true velocity in its own fields, hands the engine only
	/// the displacement (<c>Projectile.velocity = moved</c>), and must set <c>tileCollide = false</c>
	/// and <c>ignoreWater = true</c> so the engine adds exactly that and nothing else.
	/// </summary>
	public static class BallPhysics
	{
		public const float Gravity = 0.3f;
		public const float MaxFall = 14f;
		/// <summary>Share of the downward speed a ball keeps when it bounces off a floor.</summary>
		public const float FloorBounce = 0.5f;
		/// <summary>Below this falling speed a landing does not bounce: the ball starts to roll.</summary>
		public const float BounceFloor = 3f;
		public const float WallBounce = 0.75f;
		/// <summary>Speed kept per tick of rolling on a floor.</summary>
		public const float Roll = 0.994f;

		/// <summary>
		/// One tick. <paramref name="pos"/> is the top-left of the hitbox and is NOT moved; the
		/// return value is the displacement to add. <paramref name="vel"/> is updated in place.
		/// <paramref name="pos"/> may be raised by one tile when the ball climbs a step.
		/// </summary>
		public static Vector2 Step(ref Vector2 pos, ref Vector2 vel, int w, int h)
		{
			vel.Y = Math.Min(vel.Y + Gravity, MaxFall);
			Vector2 moved = Collision.TileCollision(pos, vel, w, h, false, false);

			if (moved.X != vel.X)
			{
				// rolling into a one-tile step climbs it; anything taller, or a hit in the air, bounces
				bool rolling = Math.Abs(vel.Y) < 1f;
				Vector2 up = new(pos.X + vel.X, pos.Y - 18f);
				if (rolling && !Collision.SolidCollision(up, w, h))
				{
					pos.Y -= 18f;
					moved = new Vector2(vel.X, 0f);
				}
				else
					vel.X = -vel.X * WallBounce;
			}

			if (moved.Y != vel.Y)
			{
				if (vel.Y > 0f)
				{
					vel.Y = vel.Y > BounceFloor ? -vel.Y * FloorBounce : 0f;
					vel.X *= Roll;
				}
				else
					vel.Y = 0f;
			}
			return moved;
		}

		private const int MaxPath = 320;
		private static readonly Vector2[] path = new Vector2[MaxPath];
		private static readonly Vector2[] bounce = new Vector2[12];

		/// <summary>
		/// The path a ball of size <paramref name="w"/>x<paramref name="h"/> launched from the centre
		/// <paramref name="center"/> at <paramref name="vel"/> will take over its first
		/// <paramref name="ticks"/> ticks, walked with the very <see cref="Step"/> the ball runs.
		///
		/// Drawn where the ball meets the floor - the line the player has to jump - as four layers: a
		/// faint glow on the floor along the path, a thin continuous line so the whole path reads at a
		/// glance, a flat flash at every point the ball will bounce, and motes running OUTWARD along it
		/// from the ball, faster and brighter as <paramref name="alpha"/> climbs toward the kick. With
		/// <paramref name="sun"/> the motes are little sun-glyphs (the Sultan's); without, grit (the
		/// Sand Roller's, which stays plainer - the mob teaches the boss, it does not outshine it).
		/// </summary>
		public static void DrawPath(Vector2 center, Vector2 vel, int w, int h, int ticks, Color color, float alpha, bool sun = false)
		{
			int n = 0;
			int bounces = 0;
			Vector2 pos = center - new Vector2(w, h) * 0.5f;
			Vector2 foot = new(w * 0.5f, h - 4f);
			int count = Math.Min(ticks, MaxPath - 1);
			path[n++] = pos + foot;
			for (int i = 0; i < count; i++)
			{
				float before = vel.Y;
				Vector2 moved = Step(ref pos, ref vel, w, h); // Step may raise pos for a step; add after
				pos += moved;
				path[n++] = pos + foot;
				if (before > 0f && vel.Y < 0f && bounces < bounce.Length)
					bounce[bounces++] = pos + new Vector2(w * 0.5f, h);
			}

			SpriteBatch sb = Main.spriteBatch;
			Texture2D px = TextureAssets.MagicPixel.Value;
			Texture2D flare = ModContent.Request<Texture2D>("Terrapex/Content/Projectiles/RiftFlare").Value;
			Vector2 fo = flare.Size() * 0.5f;
			Color glow = color;
			glow.A = 0;
			Color white = new(255, 255, 255, 0);
			float time = Main.GlobalTimeWrappedHourly;

			// the floor glow and the line, fading toward the far end
			for (int i = 2; i < n; i += 2)
			{
				float f = 1f - i / (float)n;
				Vector2 a = path[i - 2], b = path[i];
				if (sun && i % 4 == 0)
					sb.Draw(flare, b - Main.screenPosition, null, glow * (alpha * 0.16f * (0.3f + 0.7f * f)), 0f, fo,
						new Vector2(0.6f, 0.16f), SpriteEffects.None, 0f);
				Vector2 seg = b - a;
				if (seg.LengthSquared() < 0.25f)
					continue;
				sb.Draw(px, a - Main.screenPosition, new Rectangle(0, 0, 1, 1), glow * (alpha * (0.22f + 0.38f * f)), seg.ToRotation(),
					new Vector2(0f, 0.5f), new Vector2(seg.Length() + 0.5f, sun ? 2f : 1.5f), SpriteEffects.None, 0f);
			}

			// where it will bounce: a flat flash on the floor, beating
			for (int k = 0; k < bounces; k++)
			{
				Vector2 at = bounce[k] - Main.screenPosition;
				float beat = 0.75f + 0.25f * (float)Math.Sin(time * 8f + k);
				sb.Draw(flare, at, null, glow * (alpha * 0.5f * beat), 0f, fo, new Vector2(0.9f, 0.2f), SpriteEffects.None, 0f);
				sb.Draw(flare, at, null, white * (alpha * 0.4f * beat), 0f, fo, new Vector2(0.35f, 0.08f), SpriteEffects.None, 0f);
			}

			// motes running outward along the path from the ball - the direction of travel, made visible
			int motes = sun ? 9 : 6;
			float speed = 0.25f + 0.45f * alpha;
			for (int k = 0; k < motes && n > 1; k++)
			{
				float s = (time * speed + k / (float)motes) % 1f;
				float idx = s * (n - 1);
				int i0 = (int)idx;
				int i1 = Math.Min(n - 1, i0 + 1);
				Vector2 at = Vector2.Lerp(path[i0], path[i1], idx - i0) - Main.screenPosition;
				float k2 = alpha * (float)Math.Sqrt(Math.Sin(s * MathHelper.Pi)) * (1f - s * 0.5f);
				if (sun)
				{
					// a sun-glyph: a hot dot with a small four-point glint, turning as it goes
					float rot = time * 3f + k;
					sb.Draw(flare, at, null, glow * (0.55f * k2), 0f, fo, 0.28f, SpriteEffects.None, 0f);
					sb.Draw(flare, at, null, glow * k2, rot, fo, new Vector2(0.34f, 0.045f), SpriteEffects.None, 0f);
					sb.Draw(flare, at, null, glow * k2, rot + MathHelper.PiOver2, fo, new Vector2(0.34f, 0.045f), SpriteEffects.None, 0f);
					sb.Draw(flare, at, null, white * k2, 0f, fo, 0.08f, SpriteEffects.None, 0f);
				}
				else
				{
					sb.Draw(flare, at, null, glow * (0.35f * k2), 0f, fo, 0.16f, SpriteEffects.None, 0f);
					sb.Draw(px, at, new Rectangle(0, 0, 1, 1), color * k2, 0f, new Vector2(0.5f), 3f, SpriteEffects.None, 0f);
				}
			}
		}
	}
}
