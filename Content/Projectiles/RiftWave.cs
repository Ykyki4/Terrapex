using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// The Riftshard Cleaver's swing sends this out — the crack in the blade let go. It is the
	/// only sword projectile in the mod that keeps its damage over range, which is what a
	/// post-boss weapon is for.
	/// </summary>
	public class RiftWave : ModProjectile
	{
		public override void SetStaticDefaults() => Main.projFrames[Type] = 4;

		public override void SetDefaults()
		{
			Projectile.width = 34;
			Projectile.height = 34;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = 4;
			Projectile.timeLeft = 70;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 14;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation();

			if (++Projectile.frameCounter >= 5)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
			}

			// grows as it opens, then thins out at the end of its life
			Projectile.scale = Projectile.timeLeft > 20
				? MathHelper.Lerp(0.7f, 1.15f, MathHelper.Clamp((70 - Projectile.timeLeft) / 14f, 0f, 1f))
				: 1.15f * (Projectile.timeLeft / 20f);
			Projectile.alpha = Projectile.timeLeft > 20 ? 0 : (int)(255 * (1f - Projectile.timeLeft / 20f));

			if (Main.rand.NextBool(2))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
					Main.rand.NextBool() ? DustID.PurpleTorch : DustID.PinkTorch,
					Projectile.velocity * 0.08f, 100, default, 1.1f);
				d.noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, 0.42f, 0.14f, 0.50f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			for (int i = 0; i < 8; i++)
			{
				Dust d = Dust.NewDustPerfect(target.Center, DustID.PurpleTorch,
					Main.rand.NextVector2Circular(4f, 4f), 90, default, 1.2f);
				d.noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// drawn additively over the normal pass, the way RiftLaser and RiftTear do it, so
			// overlapping waves read as light instead of a solid block
			Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
			int h = tex.Height / Main.projFrames[Type];
			Rectangle src = new Rectangle(0, Projectile.frame * h, tex.Width, h);
			Vector2 origin = new Vector2(tex.Width * 0.5f, h * 0.5f);
			Vector2 pos = Projectile.Center - Main.screenPosition;

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			Main.spriteBatch.Draw(tex, pos, src, Color.White * (1f - Projectile.alpha / 255f),
				Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
			return false;
		}
	}
}
