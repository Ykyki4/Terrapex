using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// Skol throws one of these off every third swing. It is deliberately small: T0 melee
	/// should not out-range the tier, so the chip is a bonus for staying in the fight rather
	/// than a way to fight from outside it.
	/// </summary>
	public class StoneChip : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 180;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 10;
		}

		public override void AI()
		{
			// tumbles as it flies, and starts dropping once the throw runs out of legs
			Projectile.rotation += 0.28f * (Projectile.velocity.X >= 0f ? 1f : -1f);
			if (Projectile.timeLeft < 150)
				Projectile.velocity.Y += 0.14f;
			Projectile.velocity.X *= 0.995f;

			if (Main.rand.NextBool(8))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Stone,
					-Projectile.velocity * 0.15f, 120, default, 0.8f);
				d.noGravity = true;
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			// one bounce off the ground, then it is spent
			if (Projectile.penetrate > 1)
			{
				Projectile.penetrate--;
				if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > 0.1f)
					Projectile.velocity.X = -oldVelocity.X * 0.5f;
				if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > 0.1f)
					Projectile.velocity.Y = -oldVelocity.Y * 0.5f;
				Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.5f }, Projectile.Center);
				return false;
			}
			return true;
		}

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 5; i++)
			{
				Dust.NewDustPerfect(Projectile.Center, DustID.Stone,
					Main.rand.NextVector2Circular(2.4f, 2.4f), 120, default, 0.9f);
			}
		}
	}
}
