using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Particles;
using Terrapex.Content.Items.Weapons.Trees.Melee;

namespace Terrapex.Content.Projectiles.Trees.Melee
{
	/// <summary>
	/// The swept arc behind a Confluence-tree swing. Pure decoration, no hitbox: the sword's own
	/// swing does the hitting. It samples where the blade points every tick and draws the band the
	/// blade swept, as a stack of thin spokes - a haze in the current's colour, a body, and a hot
	/// edge in the node's own colour along the outer rim - so the arc reads as a smear of light
	/// rather than a sprite pasted behind the player.
	///
	/// ai[0] = current, ai[1] = node, ai[2] = radius in px.
	/// </summary>
	public class ConfluenceSlash : ModProjectile
	{
		private const int History = 14;
		private readonly float[] angles = new float[History];
		private int count;

		public override string Texture => "Terrapex/Content/Projectiles/RiftFlare";

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 60;
		}

		private float BladeAngle(Player p)
			=> p.itemRotation + (p.direction == 1 ? -MathHelper.PiOver4 : -3f * MathHelper.PiOver4);

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			if (Projectile.localAI[1] == 0f)
				Projectile.localAI[1] = Math.Max(2, p.itemAnimationMax);
			// one arc per swing: with autoReuse the next swing starts before this one is gone,
			// so the arc ends on its own clock rather than on itemAnimation reaching zero
			if (!p.active || p.dead || p.itemAnimation <= 0 || ++Projectile.localAI[0] > Projectile.localAI[1])
			{
				// let the smear fade out over a few ticks after the swing ends
				if (count > 0)
					count -= 3;
				if (count <= 0)
					Projectile.Kill();
				Projectile.Center = p.MountedCenter;
				return;
			}
			Projectile.Center = p.MountedCenter;
			for (int i = History - 1; i > 0; i--)
				angles[i] = angles[i - 1];
			angles[0] = BladeAngle(p);
			count = Math.Min(History, count + 1);
			Projectile.timeLeft = 10;

			if (Main.dedServ || count < 2)
				return;
			Current c = (Current)(int)Projectile.ai[0];
			Vector2 tip = Projectile.Center + angles[0].ToRotationVector2() * Projectile.ai[2];
			if (Main.rand.NextBool(2))
			{
				Vector2 v = (angles[0] + MathHelper.PiOver2 * Math.Sign(angles[0] - angles[1])).ToRotationVector2() * 3f;
				ParticleSystem.Spawn(new Spark(tip, v + Main.rand.NextVector2Circular(1f, 1f), Currents.Of(c),
					Main.rand.NextFloat(0.6f, 1f), 16));
			}
			Lighting.AddLight(tip, Currents.Of(c).ToVector3() * 0.5f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (count < 2)
				return false;
			Current c = (Current)(int)Projectile.ai[0];
			int node = (int)Projectile.ai[1];
			float r = Projectile.ai[2];
			Color body = Currents.Of(c);
			Color edge = Currents.Edge(node);
			Texture2D px = TextureAssets.MagicPixel.Value;
			Rectangle src = new(0, 0, 1, 1);
			Vector2 at = Projectile.Center - Main.screenPosition;
			const int Sub = 6;

			for (int i = 0; i < count - 1; i++)
			{
				float a0 = angles[i], a1 = angles[i + 1];
				float d = MathHelper.WrapAngle(a1 - a0);
				for (int s = 0; s < Sub; s++)
				{
					float k = (i + s / (float)Sub) / (count - 1);
					float fade = (1f - k) * (1f - k);
					float a = a0 + d * (s / (float)Sub);
					Vector2 dir = a.ToRotationVector2();
					// haze: wide and soft, the colour of the current
					Main.EntitySpriteDraw(px, at + dir * r * 0.35f, src, Glow(body, 0.18f * fade), a,
						new Vector2(0f, 0.5f), new Vector2(r * 0.72f, 5f), SpriteEffects.None, 0);
					// body: the band itself, thinning toward the tail
					Main.EntitySpriteDraw(px, at + dir * r * 0.55f, src, Glow(body, 0.55f * fade), a,
						new Vector2(0f, 0.5f), new Vector2(r * 0.45f, 3f), SpriteEffects.None, 0);
					// the hot rim, the node's own colour
					Main.EntitySpriteDraw(px, at + dir * r * 0.86f, src, Glow(edge, 0.9f * fade), a,
						new Vector2(0f, 0.5f), new Vector2(r * 0.16f * (0.4f + 0.6f * (1f - k)), 2.4f),
						SpriteEffects.None, 0);
				}
			}
			// a flare riding the leading tip
			RiftDraw.Bloom(Projectile.Center + angles[0].ToRotationVector2() * r * 0.95f, Glow(edge, 0.5f), 0.32f + 0.05f * node);
			return false;
		}

		private static Color Glow(Color c, float s)
		{
			Color o = c * s;
			o.A = 0;
			return o;
		}
	}
}
