using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Terrapex.Common
{
	/// <summary>
	/// Shared projectile draw helpers.
	///
	/// Spirit Mod's trails are vertex primitives behind fifty-odd compiled shaders. The
	/// renderer is not worth porting into a mod whose whole identity is hand-laid pixel art,
	/// but the *shape* of their API is: a trail there is a colour along its length, a width
	/// along its length, and how far back it reaches. That is exactly what
	/// <see cref="Trail"/> takes, drawn out of <c>Projectile.oldPos</c> with plain SpriteBatch.
	///
	/// Everything here draws in whatever batch is already open — no End/Begin — which is both
	/// cheaper per projectile and impossible to leave in the wrong blend state.
	/// </summary>
	public static class RiftDraw
	{
		/// <summary>
		/// A colour that glows. Terraria's default batch is **premultiplied** AlphaBlend, so an
		/// alpha of zero adds light instead of drawing nothing — this is the cheap glow every
		/// vanilla trail uses. It is *not* true inside an explicit <c>BlendState.Additive</c>
		/// batch, which is <c>SourceAlpha/One</c> and multiplies a zero-alpha draw away entirely.
		/// </summary>
		public static Color Glow(int r, int g, int b, float strength)
			=> new Color(r, g, b, 0) * strength;

		/// <summary>
		/// Afterimages down <c>oldPos</c>. <paramref name="colorAt"/> and <paramref name="scaleAt"/>
		/// are handed 1 at the projectile falling to 0 at the tail, so a caller shapes its trail by
		/// what it returns rather than by copying a loop. <paramref name="reach"/> trims the tail,
		/// which is how a trail can grow with speed or charge.
		///
		/// Requires <c>ProjectileID.Sets.TrailCacheLength</c> to be set, or oldPos is empty and
		/// this silently draws nothing.
		/// </summary>
		public static void Trail(Projectile projectile, Func<float, Color> colorAt,
			Func<float, float> scaleAt, float reach = 1f)
		{
			Texture2D tex = TextureAssets.Projectile[projectile.type].Value;
			Rectangle frame = tex.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
			Vector2 origin = frame.Size() * 0.5f;
			Vector2 offset = new Vector2(projectile.width, projectile.height) * 0.5f - Main.screenPosition;

			int len = projectile.oldPos.Length;
			int last = (int)(len * MathHelper.Clamp(reach, 0f, 1f));
			for (int i = last - 1; i >= 1; i--)
			{
				if (projectile.oldPos[i] == Vector2.Zero)
					continue;
				float f = 1f - i / (float)len;
				float rot = projectile.oldRot.Length > i ? projectile.oldRot[i] : projectile.rotation;
				Main.EntitySpriteDraw(tex, projectile.oldPos[i] + offset, frame, colorAt(f), rot,
					origin, projectile.scale * scaleAt(f), SpriteEffects.None, 0);
			}
		}

		/// <summary>The projectile's own sprite, unchanged, at the head of a trail.</summary>
		public static void Head(Projectile projectile, Color color)
		{
			Texture2D tex = TextureAssets.Projectile[projectile.type].Value;
			Rectangle frame = tex.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
			Main.EntitySpriteDraw(tex, projectile.Center - Main.screenPosition, frame, color,
				projectile.rotation, frame.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0);
		}

		/// <summary>The shared soft flare, at a world position.</summary>
		public static void Bloom(Vector2 world, Color color, float scale, float rotation = 0f)
		{
			Texture2D flare = ModContent.Request<Texture2D>("Terrapex/Content/Projectiles/RiftFlare").Value;
			Main.EntitySpriteDraw(flare, world - Main.screenPosition, null, color, rotation,
				flare.Size() * 0.5f, scale, SpriteEffects.None, 0);
		}

		/// <summary>The shared ring, at a world position.</summary>
		public static void Ring(Vector2 world, Color color, float scale, float rotation = 0f)
		{
			Texture2D ring = ModContent.Request<Texture2D>("Terrapex/Content/Projectiles/RiftRing").Value;
			Main.EntitySpriteDraw(ring, world - Main.screenPosition, null, color, rotation,
				ring.Size() * 0.5f, scale, SpriteEffects.None, 0);
		}

		/// <summary>
		/// A straight line between two world points, out of MagicPixel. Telegraphs use it —
		/// an aim line the player can read before the boss commits to a dash.
		/// </summary>
		public static void Line(Vector2 a, Vector2 b, Color color, float thick)
		{
			Vector2 seg = b - a;
			float len = seg.Length();
			if (len < 1f)
				return;
			Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, a - Main.screenPosition,
				new Rectangle(0, 0, 1, 1), color, seg.ToRotation(), new Vector2(0f, 0.5f),
				new Vector2(len, thick), SpriteEffects.None, 0);
		}

		/// <summary>
		/// One strand of the Weaver's silk: a sagging line drawn as a haze, a body and a hot
		/// core, with bright pulses running along it.
		///
		/// The sag is what stops a thread reading as a laser. It is a real catenary bias —
		/// straight down, in world space — so a horizontal strand bellies and a vertical one
		/// does not, which is how silk behaves and how the eye expects it to.
		///
		/// <paramref name="pulse"/> is a phase in [0,1); pass a negative number for none. The
		/// pulses are the cheapest thing here and they do most of the work: a static line looks
		/// painted on, a line with light travelling down it looks alive and under tension.
		/// </summary>
		public static void Silk(Vector2 a, Vector2 b, Color hot, Color haze, float thick,
			float sag, float pulse, int segments = 14)
		{
			Texture2D px = TextureAssets.MagicPixel.Value;
			Rectangle src = new Rectangle(0, 0, 1, 1);
			Vector2 prev = a;

			for (int i = 1; i <= segments; i++)
			{
				float t = i / (float)segments;
				Vector2 p = Vector2.Lerp(a, b, t);
				p.Y += (float)Math.Sin(t * MathHelper.Pi) * sag;

				Vector2 seg = p - prev;
				float rot = seg.ToRotation();
				float run = seg.Length() + 1f;
				Vector2 at = prev - Main.screenPosition;

				if (haze.PackedValue != 0u)
					Main.EntitySpriteDraw(px, at, src, haze, rot, new Vector2(0f, 0.5f),
						new Vector2(run, thick * 2.8f), SpriteEffects.None, 0);
				Main.EntitySpriteDraw(px, at, src, hot, rot, new Vector2(0f, 0.5f),
					new Vector2(run, thick), SpriteEffects.None, 0);
				Main.EntitySpriteDraw(px, at, src, Color.White * (hot.A == 0 ? 0.35f : 0.2f), rot,
					new Vector2(0f, 0.5f), new Vector2(run, thick * 0.34f), SpriteEffects.None, 0);
				prev = p;
			}

			if (pulse < 0f)
				return;
			for (int k = 0; k < 3; k++)
			{
				float t = (pulse + k / 3f) % 1f;
				Vector2 p = Vector2.Lerp(a, b, t);
				p.Y += (float)Math.Sin(t * MathHelper.Pi) * sag;
				Bloom(p, hot * 0.45f, 0.26f);
			}
		}
	}
}
