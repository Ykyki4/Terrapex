using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// The Thread Anchor's needle. The chain is drawn as thread rather than as links, which is
	/// why it needs no chain texture of its own — the whole tier is already made of this line.
	/// </summary>
	public class ThreadAnchorHook : ModProjectile
	{
		private const float Reach = 32f * 16f;

		public override void SetStaticDefaults() => Main.projFrames[Type] = 1;

		public override void SetDefaults()
		{
			Projectile.CloneDefaults(ProjectileID.GemHookAmethyst);
			Projectile.width = 14;
			Projectile.height = 14;
		}

		public override float GrappleRange() => Reach;

		public override void NumGrappleHooks(Player player, ref int numHooks) => numHooks = 1;

		public override void GrappleRetreatSpeed(Player player, ref float speed) => speed = 18f;

		public override void GrapplePullSpeed(Player player, ref float speed) => speed = 12f;

		public override bool PreDrawExtras()
		{
			Player owner = Main.player[Projectile.owner];
			Vector2 a = owner.MountedCenter - Main.screenPosition;
			Vector2 b = Projectile.Center - Main.screenPosition;
			float len = Vector2.Distance(a, b);
			if (len < 4f)
				return false;

			Texture2D px = TextureAssets.MagicPixel.Value;
			Rectangle src = new Rectangle(0, 0, 1, 1);

			// a taut line sags less the further it is pulled, which is what reads as tension
			float sag = MathHelper.Clamp(len * 0.06f, 1f, 14f);
			Vector2 prev = a;
			for (int i = 1; i <= 8; i++)
			{
				float t = i / 8f;
				Vector2 p = Vector2.Lerp(a, b, t);
				p.Y += (float)Math.Sin(t * MathHelper.Pi) * sag;
				Vector2 seg = p - prev;
				Main.spriteBatch.Draw(px, prev, src, new Color(18, 124, 120, 0) * 0.5f,
					seg.ToRotation(), new Vector2(0f, 0.5f),
					new Vector2(seg.Length() + 1f, 4f), SpriteEffects.None, 0f);
				Main.spriteBatch.Draw(px, prev, src, new Color(53, 201, 184, 0) * 0.85f,
					seg.ToRotation(), new Vector2(0f, 0.5f),
					new Vector2(seg.Length() + 1f, 1.6f), SpriteEffects.None, 0f);
				prev = p;
			}
			return false;
		}
	}
}
