using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// The Lid's swing drives this along the floor. It is not a thrown projectile — it hugs the
	/// ground, falls down ledges and stops dead against a wall, which is what keeps a slab
	/// weapon feeling like a slab instead of a sword that happens to shoot.
	/// </summary>
	public class StoneShockwave : ModProjectile
	{
		public override void SetStaticDefaults() => Main.projFrames[Type] = 4;

		public override void SetDefaults()
		{
			Projectile.width = 28;
			Projectile.height = 22;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = 4;
			Projectile.timeLeft = 90;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 16;
		}

		public override void AI()
		{
			Projectile.ai[0]++;
			Projectile.spriteDirection = Projectile.direction = Projectile.velocity.X >= 0f ? 1 : -1;

			// hug the floor: hold still vertically while there is ground under it, fall otherwise
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

			// it grinds to a halt rather than running forever
			Projectile.velocity.X *= 0.985f;
			if (Math.Abs(Projectile.velocity.X) < 1.2f)
				Projectile.Kill();

			if (++Projectile.frameCounter >= 5)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
			}

			if (ground)
			{
				for (int i = 0; i < 2; i++)
				{
					if (!Main.rand.NextBool(2))
						continue;
					Dust d = Dust.NewDustPerfect(
						Projectile.Bottom + new Vector2(Main.rand.NextFloat(-12f, 12f), -2f),
						DustID.Stone, new Vector2(-Projectile.velocity.X * 0.12f, -Main.rand.NextFloat(0.6f, 2.2f)),
						110, default, 1.05f);
					d.velocity.X += Main.rand.NextFloat(-0.6f, 0.6f);
				}
				if (Main.rand.NextBool(7))
				{
					Dust r = Dust.NewDustPerfect(Projectile.Bottom + new Vector2(Main.rand.NextFloat(-10f, 10f), -3f),
						DustID.PurpleTorch, new Vector2(0f, -Main.rand.NextFloat(0.5f, 1.4f)), 130, default, 0.9f);
					r.noGravity = true;
				}
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			// A wall stops it; a floor does not. The grace matters: the wave is born standing on
			// the ground, and without it a spawn that clips the floor kills the wave on tick one
			// and nothing ever appears.
			if (Projectile.ai[0] > 5f && Math.Abs(oldVelocity.X - Projectile.velocity.X) > 0.1f)
				return true;

			Projectile.velocity.X = oldVelocity.X;
			Projectile.velocity.Y = 0f;
			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// the Lid's own debuff travels with the wave
			target.AddBuff(ModContent.BuffType<Cracked>(), 180);
		}

		public override void OnKill(int timeLeft)
		{
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.6f }, Projectile.Center);
			for (int i = 0; i < 7; i++)
			{
				Dust.NewDustPerfect(Projectile.Bottom, DustID.Stone,
					new Vector2(Main.rand.NextFloat(-2f, 2f), -Main.rand.NextFloat(0.5f, 2.5f)),
					110, default, 1f);
			}
		}
	}
}
