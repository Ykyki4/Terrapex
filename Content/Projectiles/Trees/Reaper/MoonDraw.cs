using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Terrapex.Content.Projectiles.Trees.Reaper
{
	/// <summary>
	/// The Harvest Moon tree's draw pieces: a disc, a ring of beads and a ray, all snapped to the
	/// mod's 2x2 grid where they are solid, and the shared flare where they are light. Every call
	/// takes an <c>emit</c> so the same shape can go straight to the sprite batch from a
	/// projectile's PreDraw (<see cref="Direct"/>) or into a player layer's DrawDataCache - one
	/// body for the moon over the reach circle and the eclipse on the point.
	///
	/// All of it draws in the batch already open, which in both places is the premultiplied
	/// AlphaBlend: a colour with A = 0 is light, a colour with full alpha paints. The dark discs
	/// rely on the second, the coronas on the first.
	/// </summary>
	public static class MoonDraw
	{
		public const string GlyphPath = "Terrapex/Content/Projectiles/Trees/Reaper/MoonGlyphs";
		public const int GlyphSize = 32;
		public const int HarvestGlyph = 0, EclipseGlyph = 1, NightGlyph = 2, HorizonGlyph = 3;

		/// <summary>The four pillars' colours, as Final Harvest carries them down its spine.</summary>
		public static readonly Color[] Pillars =
		{
			new(0xff, 0x9a, 0x3a), new(0x3a, 0xe0, 0xc0), new(0xff, 0x6a, 0xd8), new(0x6a, 0xb0, 0xff),
		};

		public static readonly Color Regard = new(0xff, 0xd7, 0x9a);
		public static readonly Color Prime = new(0xee, 0xf1, 0xf8);
		public static readonly Color Void = new(6, 5, 14);

		private static readonly Rectangle One = new(0, 0, 1, 1);
		private static Texture2D Px => TextureAssets.MagicPixel.Value;
		public static Texture2D Flare => ModContent.Request<Texture2D>("Terrapex/Content/Projectiles/RiftFlare").Value;

		public static readonly Action<DrawData> Direct = d => Main.EntitySpriteDraw(d);

		public static Color Light(Color c, float strength) => new Color(c.R, c.G, c.B, 0) * strength;

		/// <summary>A solid disc, row by row on the 2x2 grid. <paramref name="c"/> is in screen space.</summary>
		public static void Disc(Action<DrawData> emit, Vector2 c, float r, Color color)
		{
			if (r < 1f)
				return;
			int cx = (int)c.X & ~1, cy = (int)c.Y & ~1;
			int rows = (int)Math.Ceiling(r / 2f) * 2;
			for (int y = -rows; y < rows; y += 2)
			{
				float mid = y + 1f;
				float h = r * r - mid * mid;
				if (h <= 0f)
					continue;
				int half = (int)Math.Round(Math.Sqrt(h) / 2f) * 2;
				if (half <= 0)
					continue;
				emit(new DrawData(Px, new Vector2(cx - half, cy + y), One, color, 0f, Vector2.Zero,
					new Vector2(half * 2, 2), SpriteEffects.None));
			}
		}

		/// <summary>One 2x2 bead (or a short streak, if <paramref name="length"/> &gt; 2) at a screen point.</summary>
		public static void Bead(Action<DrawData> emit, Vector2 at, Color color, float length = 2f, float rotation = 0f)
			=> emit(new DrawData(Px, new Vector2((int)at.X & ~1, (int)at.Y & ~1) + Vector2.One, One, color, rotation,
				new Vector2(0.5f), new Vector2(length, 2f), SpriteEffects.None));

		/// <summary><paramref name="count"/> beads round a circle, coloured per bead.</summary>
		public static void Beads(Action<DrawData> emit, Vector2 c, float r, int count, float phase, Func<int, Color> colorAt)
		{
			for (int i = 0; i < count; i++)
				Bead(emit, c + (phase + MathHelper.TwoPi * i / count).ToRotationVector2() * r, colorAt(i));
		}

		/// <summary>The soft flare, centred.</summary>
		public static void Glow(Action<DrawData> emit, Vector2 c, Color color, float scale)
		{
			Texture2D f = Flare;
			emit(new DrawData(f, c, null, color, 0f, f.Size() * 0.5f, scale, SpriteEffects.None));
		}

		/// <summary>
		/// A ray out of a disc: the flare squashed into a streak whose middle sits
		/// <paramref name="from"/> + half its length out along <paramref name="angle"/>.
		/// </summary>
		public static void Ray(Action<DrawData> emit, Vector2 c, float angle, float from, float length, float width, Color color)
		{
			Texture2D f = Flare;
			Vector2 mid = c + angle.ToRotationVector2() * (from + length * 0.5f);
			emit(new DrawData(f, mid, null, color, angle, f.Size() * 0.5f,
				new Vector2(length / f.Width, width / f.Height), SpriteEffects.None));
		}

		/// <summary>One frame of the moon glyph sheet, centred.</summary>
		public static void Glyph(Action<DrawData> emit, Vector2 c, int frame, Color color, float scale = 1f)
		{
			Texture2D t = ModContent.Request<Texture2D>(GlyphPath).Value;
			Rectangle src = new(0, frame * GlyphSize, GlyphSize, GlyphSize);
			emit(new DrawData(t, new Vector2((int)c.X & ~1, (int)c.Y & ~1), src, color, 0f,
				new Vector2(GlyphSize / 2f), scale, SpriteEffects.None));
		}

		/// <summary>
		/// A tilted accretion ring as beads on an ellipse. <paramref name="front"/> picks the half
		/// nearer the viewer, so a caller draws the back half, then the black disc, then the front:
		/// the ring passes behind the hole and in front of it, which is the whole illusion.
		/// The approaching side runs hot (Doppler), the receding side cold.
		/// </summary>
		public static void Accretion(Action<DrawData> emit, Vector2 c, float radius, float squash, float tilt,
			int count, float phase, bool front, Color hot, Color cold, float alpha, Color[] accents = null, int accentEvery = 0)
		{
			for (int i = 0; i < count; i++)
			{
				float th = phase + MathHelper.TwoPi * i / count + 0.35f * (float)Math.Sin(i * 12.9898f);
				float s = (float)Math.Sin(th), co = (float)Math.Cos(th);
				if ((s >= 0f) != front)
					continue;
				Vector2 p = new Vector2(co * radius, s * radius * squash).RotatedBy(tilt);
				Vector2 tangent = new Vector2(-s * radius, co * radius * squash).RotatedBy(tilt);
				float doppler = 0.5f + 0.5f * co;
				Color col = Color.Lerp(cold, hot, doppler);
				if (accents != null && accentEvery > 0 && i % accentEvery == 0)
					col = accents[(i / accentEvery) % accents.Length];
				float a = alpha * (front ? 1f : 0.7f) * (0.45f + 0.55f * doppler);
				Bead(emit, c + p, Light(col, a), 2f + 4f * doppler, tangent.ToRotation());
			}
		}
	}
}
