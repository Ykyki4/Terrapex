using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// The Keeper's bread-and-butter projectile. It leaves the boss slowly enough to be read,
	/// then winds up to full speed, so a wall of them is dodgeable on sight instead of being a
	/// smear the player only notices as damage.
	/// </summary>
	public class RiftShard : ModProjectile
	{
		private const int AccelDelay = 22;    // ticks of slow, readable travel
		private const float MaxBoost = 1.75f; // top speed relative to the launch speed

		private ref float LaunchSpeed => ref Projectile.ai[0];
		private ref float Timer => ref Projectile.ai[1];

		public override void SetStaticDefaults()
		{
			// the 8 frames are a rotation cycle, so the sprite spins without touching rotation
			Main.projFrames[Type] = 8;
			ProjectileID.Sets.TrailCacheLength[Type] = 9;
			ProjectileID.Sets.TrailingMode[Type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 360;
			Projectile.aiStyle = -1;
			Projectile.penetrate = 1;
		}

		public override void AI()
		{
			if (Timer == 0f)
				LaunchSpeed = Projectile.velocity.Length();
			Timer++;

			if (Timer > AccelDelay && LaunchSpeed > 0.1f)
			{
				float speed = Projectile.velocity.Length();
				if (speed < LaunchSpeed * MaxBoost)
					Projectile.velocity *= 1.014f;
			}

			if (++Projectile.frameCounter >= 4)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
			}

			// swells slightly out of the spawn flash so a fresh volley catches the eye
			Projectile.scale = MathHelper.Clamp(0.55f + Timer / 10f, 0.55f, 1f);

			Lighting.AddLight(Projectile.Center, 0.65f, 0.2f, 0.9f);

			if (Main.rand.NextBool(2))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
					Main.rand.NextBool(4) ? DustID.PinkTorch : DustID.PurpleTorch,
					Projectile.velocity * -0.12f, 120, default, 1.0f);
				d.noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			var tex = TextureAssets.Projectile[Type].Value;
			Rectangle frame = tex.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
			Vector2 origin = frame.Size() * 0.5f;
			Vector2 offset = new Vector2(Projectile.width, Projectile.height) * 0.5f - Main.screenPosition;

			// afterimages: the shard's own path, so fast volleys still show where they came from
			for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
			{
				if (Projectile.oldPos[i] == Vector2.Zero)
					continue;
				float f = 1f - i / (float)Projectile.oldPos.Length;
				Main.EntitySpriteDraw(tex, Projectile.oldPos[i] + offset, frame,
					new Color(150, 70, 210, 0) * (f * 0.45f), Projectile.rotation, origin,
					Projectile.scale * (0.55f + 0.4f * f), SpriteEffects.None, 0);
			}

			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame,
				Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
			return false;
		}

		public override Color? GetAlpha(Color lightColor) => Color.White;

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 10; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PinkTorch,
					Main.rand.NextVector2Circular(3.5f, 3.5f), 100, default, 1.2f);
				d.noGravity = true;
			}
		}
	}
}
