using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// A rift torn open next to the player. It spawns in the middle of a busy fight, so the whole
	/// opening sequence is built as a tell: a shrinking ring marks the spot, a bloom grows
	/// underneath it, and the moment the hitbox arms gets its own flash and bang.
	/// </summary>
	public class RiftTear : ModProjectile
	{
		private const int FrameTicks = 10;  // 6 frames of opening
		private const int ArmedFrame = 4;   // frames 0-3 are the tell and do no damage
		private const int ArmTime = FrameTicks * ArmedFrame;

		private ref float FiredBurst => ref Projectile.ai[0];
		private ref float Timer => ref Projectile.ai[1];

		private bool Armed => Projectile.frame >= ArmedFrame;

		public override void SetStaticDefaults()
		{
			Main.projFrames[Type] = 6;
		}

		public override void SetDefaults()
		{
			Projectile.width = 34;
			Projectile.height = 16;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 118;
			Projectile.aiStyle = -1;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			Projectile.velocity = Vector2.Zero;

			if (Timer == 0f)
			{
				// arrival: audible and visible before any part of it is dangerous
				SoundEngine.PlaySound(SoundID.Item122 with { Pitch = -0.7f, Volume = 0.9f }, Projectile.Center);
				for (int i = 0; i < 22; i++)
				{
					Dust d = Dust.NewDustPerfect(Projectile.Center,
						Main.rand.NextBool(3) ? DustID.PinkTorch : DustID.PurpleTorch,
						Main.rand.NextVector2CircularEdge(5f, 5f) * Main.rand.NextFloat(0.4f, 1f),
						90, default, 1.3f);
					d.noGravity = true;
				}
			}
			Timer++;

			if (Projectile.frame < Main.projFrames[Type] - 1 && ++Projectile.frameCounter >= FrameTicks)
			{
				Projectile.frameCounter = 0;
				Projectile.frame++;

				if (Projectile.frame == ArmedFrame)
				{
					// the hitbox turns on here, so it gets its own flash, bang and shockwave
					SoundEngine.PlaySound(SoundID.Item14 with { Pitch = 0.5f }, Projectile.Center);
					for (int i = 0; i < 26; i++)
					{
						Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PinkTorch,
							Main.rand.NextVector2CircularEdge(7f, 3.5f), 60, default, 1.6f);
						d.noGravity = true;
					}
				}
			}

			// grows into place instead of popping in at full size
			Projectile.scale = MathHelper.Clamp(0.35f + Timer / 16f, 0.35f, 1f);

			float lit = Armed ? 1f : Projectile.frame / (float)ArmedFrame;
			Lighting.AddLight(Projectile.Center, 0.9f * lit + 0.25f, 0.25f, 1.15f * lit + 0.3f);

			// once fully open it spits shards along its own axis, then closes
			if (Projectile.frame == Main.projFrames[Type] - 1 && FiredBurst == 0f)
			{
				FiredBurst = 1f;
				SoundEngine.PlaySound(SoundID.Item92, Projectile.Center);

				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					for (int i = -1; i <= 1; i++)
					{
						Vector2 v = new Vector2(0f, -4.8f).RotatedBy(MathHelper.ToRadians(38f) * i);
						Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, v,
							ModContent.ProjectileType<RiftShard>(), Projectile.damage, 2f, Projectile.owner);
						Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, -v,
							ModContent.ProjectileType<RiftShard>(), Projectile.damage, 2f, Projectile.owner);
					}
				}
			}

			if (Main.rand.NextBool(Armed ? 1 : 3))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(16f, 6f),
					DustID.PurpleTorch, Vector2.Zero, 120, default, 1.1f);
				d.noGravity = true;
				d.velocity *= 0.25f;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			var flare = ModContent.Request<Texture2D>("Terrapex/Content/Projectiles/RiftFlare").Value;
			var ring = ModContent.Request<Texture2D>("Terrapex/Content/Projectiles/RiftRing").Value;
			Vector2 draw = Projectile.Center - Main.screenPosition;

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			if (!Armed)
			{
				// a ring closing in on the spot: the player sees exactly where and when
				float t = MathHelper.Clamp(Timer / ArmTime, 0f, 1f);
				float r = MathHelper.Lerp(3.4f, 1.0f, t * t);
				Main.EntitySpriteDraw(ring, draw, null, new Color(230, 140, 255) * (0.35f + 0.6f * t),
					Timer * 0.02f, ring.Size() * 0.5f, r, SpriteEffects.None, 0);
				Main.EntitySpriteDraw(flare, draw, null, new Color(150, 60, 220) * (0.25f + 0.45f * t),
					0f, flare.Size() * 0.5f, 0.5f + t * 0.8f, SpriteEffects.None, 0);
			}
			else
			{
				// arming flash, then a steady bloom under the open rift
				float since = Timer - ArmTime;
				float flash = MathHelper.Clamp(1f - since / 12f, 0f, 1f);
				if (flash > 0f)
				{
					Main.EntitySpriteDraw(ring, draw, null, Color.White * flash,
						0f, ring.Size() * 0.5f, 1f + (1f - flash) * 2.6f, SpriteEffects.None, 0);
					Main.EntitySpriteDraw(flare, draw, null, Color.White * flash,
						0f, flare.Size() * 0.5f, 1.4f, SpriteEffects.None, 0);
				}
				float pulse = 0.9f + 0.12f * (float)System.Math.Sin(Timer * 0.25f);
				Main.EntitySpriteDraw(flare, draw, null, new Color(190, 80, 245) * 0.7f,
					0f, flare.Size() * 0.5f, pulse, SpriteEffects.None, 0);
			}

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			return true;   // the rift sprite itself still draws normally on top
		}

		// the opening frames are pure telegraph — no hitbox until the rift is actually torn
		public override bool CanHitPlayer(Player target) => Armed;

		public override Color? GetAlpha(Color lightColor) => Color.White;

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 16; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PinkTorch,
					Main.rand.NextVector2Circular(4f, 2f), 100, default, 1.2f);
				d.noGravity = true;
			}
		}
	}
}
