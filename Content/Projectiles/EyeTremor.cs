using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Particles;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// The tremor the Dormant Eye's leap sends along the floor: a run of stone spikes punching up
	/// out of the ground and sinking again behind it. It hugs the floor - stepping a few tiles up
	/// or down - and dies on a wall or a drop, so jumping it is the whole answer.
	///
	/// Its tell is the leap itself: the landing spot is marked on the floor, with ticks running out
	/// each side, from the first tick of the crouch. T0 stone, so it is matte - lit by the world,
	/// no glow.
	/// </summary>
	public class EyeTremor : ModProjectile
	{
		private const int Life = 58;

		public override string Texture => "Terrapex/Content/Projectiles/Rockfall";

		public override void SetDefaults()
		{
			Projectile.width = 26;
			Projectile.height = 28;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Life;
		}

		public override void AI()
		{
			// find the floor under it: from a few tiles up to a few tiles down
			int tx = (int)(Projectile.Center.X / 16f);
			int top = (int)((Projectile.Bottom.Y - 48f) / 16f);
			int ground = -1;
			for (int ty = top; ty < top + 8; ty++)
			{
				// platforms carry it too: a tremor that dies on a plank would leave a safe shelf
				if (NPCs.Bosses.DormantEye.Standable(tx, ty))
				{
					ground = ty;
					break;
				}
			}
			if (ground < 0 || ground == top)
			{
				Projectile.Kill();
				return;
			}
			Projectile.position.Y = ground * 16f - Projectile.height;

			if (Main.dedServ)
				return;
			Vector2 foot = new(Projectile.Center.X, Projectile.Bottom.Y);
			if (Main.rand.NextBool(2))
				ParticleSystem.Spawn(new Chip(foot + new Vector2(Main.rand.NextFloat(-10f, 10f), -4f),
					new Vector2(Projectile.velocity.X * Main.rand.NextFloat(0.1f, 0.4f), -Main.rand.NextFloat(1.5f, 4f)),
					Stone[Main.rand.Next(Stone.Length)], Main.rand.NextFloat(0.6f, 1.1f), 40));
			if (Projectile.timeLeft % 4 == 0)
				HarvestFX.MatteCrack(foot + new Vector2(0f, 1f), new Vector2(-Math.Sign(Projectile.velocity.X), 0.05f),
					14f, new Color(0x1f, 0x19, 0x30), 36, 2f);
			if (Main.rand.NextBool(3))
				Dust.NewDustDirect(new Vector2(Projectile.position.X, Projectile.Bottom.Y - 6f), Projectile.width, 6,
					DustID.Stone, 0f, -1.5f, 100, default, 1f);
		}

		public override void OnKill(int timeLeft)
		{
			if (!Main.dedServ)
				HarvestFX.Rubble(Projectile.Bottom - new Vector2(0f, 6f), Stone, 6, 3f, 0.9f, 2.5f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// five spikes on the 2 px grid, each rising and sinking on its own phase; the ones at the
			// front are rising, the ones behind sinking, so the whole thing reads as travelling
			Texture2D px = TextureAssets.MagicPixel.Value;
			Rectangle one = new(0, 0, 1, 1);
			int dir = Projectile.velocity.X >= 0f ? 1 : -1;
			float age = Life - Projectile.timeLeft;
			float rise = Math.Min(1f, age / 5f) * Math.Min(1f, Projectile.timeLeft / 10f);
			Color lit = Lighting.GetColor((int)(Projectile.Center.X / 16f), (int)(Projectile.Center.Y / 16f));
			Vector2 foot = new Vector2(Projectile.Center.X, Projectile.Bottom.Y + 2f) - Main.screenPosition;

			for (int k = -2; k <= 2; k++)
			{
				float phase = (k * dir + 2) / 4f;                       // 1 at the front
				float h = (8f + 22f * phase * (0.75f + 0.25f * MathF.Sin(age * 0.6f + k))) * rise;
				int hh = (int)(h / 2f) * 2;
				if (hh < 2)
					continue;
				int x = (int)(foot.X + k * 6f) / 2 * 2;
				int w = 6 - Math.Abs(k);
				w = Math.Max(2, w / 2 * 2);
				for (int y = 0; y < hh; y += 2)
				{
					float u = y / (float)hh;                            // 0 at the base, 1 at the tip
					int ww = Math.Max(2, (int)(w * (1f - u * 0.8f)) / 2 * 2);
					int left = x - ww / 2;
					// lit face toward the way it travels, dark face behind, outline at the edges
					Color face = Shade(u > 0.8f ? 0 : 1, lit);
					Color back = Shade(u > 0.8f ? 1 : 2, lit);
					Draw(px, one, new Vector2(left - 2, foot.Y - y - 2), 2, Outline(lit));
					Draw(px, one, new Vector2(left + ww, foot.Y - y - 2), 2, Outline(lit));
					for (int c = 0; c < ww; c += 2)
					{
						bool front = dir > 0 ? c >= ww / 2 : c < ww / 2;
						Draw(px, one, new Vector2(left + c, foot.Y - y - 2), 2, front ? face : back);
					}
				}
				Draw(px, one, new Vector2(x - 1, foot.Y - hh - 2), 2, Outline(lit));
			}
			return false;
		}

		private static void Draw(Texture2D px, Rectangle one, Vector2 at, int size, Color c)
			=> Main.EntitySpriteDraw(px, at, one, c, 0f, Vector2.Zero, size, SpriteEffects.None, 0);

		private static Color Shade(int i, Color lit) => Stone[Math.Clamp(i, 0, Stone.Length - 1)].MultiplyRGB(lit);

		private static Color Outline(Color lit) => new Color(0x24, 0x17, 0x33).MultiplyRGB(lit);

		private static readonly Color[] Stone =
			{ new(0xc9, 0xbd, 0xd1), new(0x93, 0x84, 0x9f), new(0x63, 0x54, 0x78), new(0x3f, 0x33, 0x57) };
	}
}
