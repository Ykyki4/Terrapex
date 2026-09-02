using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// One stone out of the Scree Tome's cone. Fast, short-lived and it dies on the first
	/// thing it touches — the spell is the volume, not the individual rock.
	/// </summary>
	public class ScreeStone : ModProjectile
	{
		public override void SetStaticDefaults() => Main.projFrames[Type] = 3;

		public override void SetDefaults()
		{
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 90;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = false;
		}

		public override void AI()
		{
			Projectile.rotation += Projectile.velocity.X * 0.06f;
			Projectile.velocity.Y += 0.22f;
			if (Projectile.velocity.Y > 14f)
				Projectile.velocity.Y = 14f;

			// a stable per-stone frame, so the cone looks like different rocks
			if (Projectile.frame == 0 && Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
			}

			Lighting.AddLight(Projectile.Center, 0.12f, 0.05f, 0.16f);
			if (Main.rand.NextBool(5))
			{
				Dust.NewDustPerfect(Projectile.Center, DustID.Stone,
					Projectile.velocity * -0.08f, 130, default, 0.8f);
			}
		}

		public override void OnKill(int timeLeft)
		{
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.3f }, Projectile.Center);
			for (int i = 0; i < 5; i++)
			{
				Dust.NewDustPerfect(Projectile.Center, DustID.Stone,
					Main.rand.NextVector2Circular(2.5f, 2.5f), 110, default, 0.95f);
			}
		}
	}
}
