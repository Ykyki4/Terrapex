using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace Terrapex.Common.Particles
{
	/// <summary>
	/// A torn corner of a page: a small pale quad that flutters, sways and settles. It is what a
	/// page turn throws off, so turning the page is seen as well as heard. Matte, not glow -
	/// paper does not shine.
	/// </summary>
	public class PageLeaf : Particle
	{
		private readonly float sway;
		private readonly float spin;

		public PageLeaf(Vector2 pos, Vector2 vel, Color color, float scale, int life)
		{
			Position = pos; Velocity = vel; Color = color; Scale = scale; Lifetime = life;
			Glow = false;
			sway = Main.rand.NextFloat(MathHelper.TwoPi);
			spin = Main.rand.NextFloat(-0.2f, 0.2f);
		}

		public override void Update()
		{
			base.Update();
			Velocity *= 0.93f;
			Velocity.Y += 0.05f;
			Velocity.X += (float)Math.Sin(sway + Time * 0.25f) * 0.08f;
			Rotation += spin;
		}

		public override void Draw(SpriteBatch sb)
		{
			float fade = 1f - Progress * Progress;
			Color lit = Lighting.GetColor((int)(Position.X / 16f), (int)(Position.Y / 16f));
			Color c = Color.Lerp(Color, Color.MultiplyRGB(lit), 0.5f) * fade;
			// a flutter is a quad whose width breathes - the page edge turning toward you and away
			float flip = 0.35f + 0.65f * Math.Abs((float)Math.Sin(sway + Time * 0.3f));
			sb.Draw(TextureAssets.MagicPixel.Value, Position - Main.screenPosition, new Rectangle(0, 0, 1, 1), c,
				Rotation, new Vector2(0.5f, 0.5f), new Vector2(5f * flip, 6f) * Scale, SpriteEffects.None, 0f);
		}
	}

	/// <summary>The Almanac tree's bursts, kept in one place so the four books share them.</summary>
	public static class AlmanacFX
	{
		public static readonly Color Parchment = new(0xf4, 0xef, 0xe6);

		/// <summary>The page turning: a few leaves thrown off and a ring in the new page's colour.</summary>
		public static void TurnPage(Vector2 at, Color page)
		{
			for (int i = 0; i < 6; i++)
			{
				Vector2 v = new Vector2(Main.rand.NextFloat(-2.2f, 2.2f), Main.rand.NextFloat(-2.6f, -0.6f));
				ParticleSystem.Spawn(new PageLeaf(at + Main.rand.NextVector2Circular(6f, 4f), v,
					i % 2 == 0 ? Parchment : page, Main.rand.NextFloat(0.6f, 1f), Main.rand.Next(34, 50)));
			}
			ParticleSystem.Spawn(new Shockwave(at, page, 0.08f, 0.55f, 18, 0.6f));
			ParticleSystem.Spawn(new Bloom(at, page, 0.25f, 0.5f, 14));
		}

		/// <summary>A cast leaving the book: a muzzle bloom and a fan of sparks down the aim.</summary>
		public static void CastFlash(Vector2 at, Vector2 dir, Color page, bool charged)
		{
			float size = charged ? 1.4f : 0.8f;
			ParticleSystem.Spawn(new Bloom(at, page, 0.2f * size, 0.55f * size, 12));
			FX.Sparks(at, page, charged ? 10 : 5, 5f * size, 0.8f, 16, 0f, 0.9f, dir.ToRotation());
			if (charged)
				ParticleSystem.Spawn(new Shockwave(at, page, 0.1f, 0.9f, 20));
		}
	}
}
