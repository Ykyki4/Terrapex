using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terrapex.Common.Particles;
using Terrapex.Content.Items.Weapons.Trees.Reaper;

namespace Terrapex.Content.Projectiles.Trees.Reaper
{
	/// <summary>
	/// The Eclipse's mark on a point where two rites crossed: a dark disc, a corona of rays in the
	/// two rites' colours turning round it, a white inner ring, and the one bright bead on the rim
	/// that a real eclipse has at second contact - the diamond ring. Harmless; it is the tell that
	/// the cut was a double, so the player learns the edge rule by seeing it.
	///
	/// ai[0], ai[1]: the two rites (their tints). ai[2]: scale, 0 meaning 1. A projectile rather
	/// than particles so every client in the world sees it, not only the owner.
	/// </summary>
	public class EclipseCorona : ModProjectile
	{
		private const int Life = 36;
		private const float Disc = 12f;

		public override string Texture => "Terrapex/Content/Projectiles/RiftFlare";

		private ref float Timer => ref Projectile.localAI[0];
		private float Size => Projectile.ai[2] > 0f ? Projectile.ai[2] : 1f;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.aiStyle = -1;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Life;
		}

		public override bool? CanDamage() => false;

		private Color TintA => MoonRites.Tints[(int)Projectile.ai[0] % MoonRites.Tints.Length];
		private Color TintB => MoonRites.Tints[(int)Projectile.ai[1] % MoonRites.Tints.Length];

		public override void AI()
		{
			if (Timer == 0f)
			{
				ParticleSystem.Spawn(new Shockwave(Projectile.Center, MoonDraw.Light(Color.Lerp(TintA, TintB, 0.5f), 0.8f),
					0.15f * Size, 1.3f * Size, 26));
				FX.Sparks(Projectile.Center, TintA, 6, 6f * Size, Size, 22);
				FX.Sparks(Projectile.Center, TintB, 6, 6f * Size, Size, 22);
			}
			Timer++;
			Projectile.velocity = Vector2.Zero;
			Lighting.AddLight(Projectile.Center, Color.Lerp(TintA, TintB, 0.5f).ToVector3() * 0.5f * (1f - Timer / Life));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float t = Timer / Life;
			float open = 1f - (float)Math.Pow(1f - Math.Min(1f, t * 3f), 3);
			float fade = t < 0.65f ? 1f : 1f - (t - 0.65f) / 0.35f;
			float s = Size * open;
			Vector2 c = Projectile.Center - Main.screenPosition;
			var emit = MoonDraw.Direct;
			float spin = Timer * 0.03f;
			float r = Disc * s;

			// the outer corona: a wide soft glow of both rites, then the streamers
			MoonDraw.Glow(emit, c, MoonDraw.Light(Color.Lerp(TintA, TintB, 0.5f), 0.45f * fade), 1.4f * s);
			for (int i = 0; i < 12; i++)
			{
				float a = spin + MathHelper.TwoPi * i / 12f;
				float len = (18f + 10f * (float)Math.Sin(Timer * 0.2f + i * 1.7f)) * s * (i % 2 == 0 ? 1.3f : 0.8f);
				MoonDraw.Ray(emit, c, a, r * 0.6f, len, 7f * s, MoonDraw.Light(i % 2 == 0 ? TintA : TintB, 0.7f * fade));
			}
			// the inner corona: the white that hugs the limb
			MoonDraw.Glow(emit, c, MoonDraw.Light(Color.White, 0.5f * fade), 0.55f * s);
			MoonDraw.Beads(emit, c, r + 2f, Math.Max(12, (int)(r * 1.6f)), spin * 2f,
				i => MoonDraw.Light(Color.Lerp(Color.White, TintA, (i % 3) / 3f), 0.85f * fade));

			// the disc itself, painted, not lit
			MoonDraw.Disc(emit, c, r, new Color(14, 10, 26) * fade);
			MoonDraw.Disc(emit, c + new Vector2(-2f, -2f) * s, r * 0.55f, new Color(30, 22, 48) * (0.6f * fade));

			// the diamond ring: one bead of the sun still showing at the edge
			Vector2 bead = c + (spin * 3f - 0.8f).ToRotationVector2() * (r + 1f);
			MoonDraw.Glow(emit, bead, MoonDraw.Light(Color.White, 0.9f * fade), 0.3f * s);
			MoonDraw.Bead(emit, bead, Color.White * fade);
			return false;
		}
	}
}
