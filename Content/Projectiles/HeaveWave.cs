using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// What the Heave's Shelf sends out along the floor when it lands: the Lid's shockwave with
	/// the ownership reversed. It hugs the ground and never rises, which is the whole dodge —
	/// the answer to Shelf is a jump, and a wave that drifted upward would take that away.
	/// </summary>
	public class HeaveWave : ModProjectile
	{
		public override string Texture => "Terrapex/Content/Projectiles/StoneShockwave";

		public override void SetStaticDefaults() => Main.projFrames[Type] = 4;

		public override void SetDefaults()
		{
			Projectile.width = 30;
			Projectile.height = 24;
			Projectile.aiStyle = -1;
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 240;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
		}

		public override void AI()
		{
			Projectile.ai[0]++;
			Projectile.spriteDirection = Projectile.direction = Projectile.velocity.X >= 0f ? 1 : -1;

			bool ground = Collision.SolidCollision(
				Projectile.BottomLeft + new Vector2(2f, 0f), Projectile.width - 4, 6);
			if (ground)
			{
				Projectile.velocity.Y = 0f;
				Projectile.position.Y = (float)Math.Floor(Projectile.position.Y);
			}
			else
			{
				Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.4f, 10f);
			}

			Projectile.velocity.X *= 0.992f;
			if (Math.Abs(Projectile.velocity.X) < 1.4f)
				Projectile.Kill();

			if (++Projectile.frameCounter >= 5)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
			}

			Lighting.AddLight(Projectile.Center, 0.35f, 0.12f, 0.45f);

			if (!ground)
				return;

			for (int i = 0; i < 2; i++)
			{
				if (!Main.rand.NextBool(2))
					continue;
				Dust d = Dust.NewDustPerfect(
					Projectile.Bottom + new Vector2(Main.rand.NextFloat(-13f, 13f), -2f),
					DustID.Stone,
					new Vector2(-Projectile.velocity.X * 0.12f, -Main.rand.NextFloat(0.6f, 2.4f)),
					110, default, 1.1f);
				d.velocity.X += Main.rand.NextFloat(-0.6f, 0.6f);
			}
			if (Main.rand.NextBool(6))
			{
				Dust r = Dust.NewDustPerfect(
					Projectile.Bottom + new Vector2(Main.rand.NextFloat(-10f, 10f), -3f),
					DustID.PurpleTorch, new Vector2(0f, -Main.rand.NextFloat(0.5f, 1.6f)), 130, default, 0.95f);
				r.noGravity = true;
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			// a wall stops it, a floor does not — and the grace period is what lets it be born
			// standing on the ground the boss has just hit
			if (Projectile.ai[0] > 5f && Math.Abs(oldVelocity.X - Projectile.velocity.X) > 0.1f)
				return true;

			Projectile.velocity.X = oldVelocity.X;
			Projectile.velocity.Y = 0f;
			return false;
		}

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.5f }, Projectile.Center);
			for (int i = 0; i < 8; i++)
			{
				Dust.NewDustPerfect(Projectile.Bottom, DustID.Stone,
					new Vector2(Main.rand.NextFloat(-2.2f, 2.2f), -Main.rand.NextFloat(0.5f, 2.6f)),
					110, default, 1f);
			}
		}
	}
}
