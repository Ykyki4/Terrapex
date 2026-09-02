using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Players;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// The crescent the Rift Scythe throws. It flies out, turns, and comes back through the
	/// same crowd, so one swing is two passes. Every enemy it touches banks a reap stack on
	/// the owner, and it is born wider for every stack the owner already had.
	/// </summary>
	public class ScytheArc : ModProjectile
	{
		private const int Life = 130;
		private const int TurnAt = 38;

		private ref float Timer => ref Projectile.ai[0];
		private ref float Stacks => ref Projectile.ai[1];

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 10;
			ProjectileID.Sets.TrailingMode[Type] = 3;
		}

		public override void SetDefaults()
		{
			Projectile.width = 44;
			Projectile.height = 26;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			// one hit per target per leg, so the return pass is a second chance rather than
			// a free doubling on whatever it happens to be sitting inside
			Projectile.localNPCHitCooldown = TurnAt;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			Timer++;

			if (Timer < TurnAt)
			{
				// it barely coasted before: at 0.955 the arc had spent four fifths of its
				// speed by the turn and died almost on top of the player
				Projectile.velocity *= 0.982f;
			}
			else
			{
				// the return leg homes, so it always finds its way back through the pack
				Vector2 home = owner.MountedCenter - Projectile.Center;
				float dist = home.Length();
				if (dist < 34f)
				{
					Projectile.Kill();
					return;
				}
				Projectile.velocity = Vector2.Lerp(Projectile.velocity,
					home / dist * 16f, 0.10f);
			}

			Projectile.rotation = Projectile.velocity.ToRotation();
			Projectile.scale = (0.75f + 0.055f * Stacks)
				* MathHelper.Lerp(0.8f, 1.25f, 1f - System.Math.Abs(Timer - TurnAt) / (float)TurnAt);

			Lighting.AddLight(Projectile.Center, 0.16f, 0.40f, 0.36f);
			if (Main.rand.NextBool(2))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(14f, 10f),
					DustID.Vortex, Projectile.velocity * 0.06f, 130, default, 0.9f);
				d.noGravity = true;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.owner == Main.myPlayer)
				Main.player[Projectile.owner].GetModPlayer<TerrapexPlayer>().Reap();

			for (int i = 0; i < 5; i++)
			{
				Dust d = Dust.NewDustPerfect(target.Center, DustID.Vortex,
					Main.rand.NextVector2Circular(3f, 3f), 110, default, 1f);
				d.noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// the trail widens with the blade rather than tapering, which is what makes the
			// crescent read as opening instead of shrinking away behind itself
			RiftDraw.Trail(Projectile,
				f => RiftDraw.Glow(40, 170, 158, f * (0.30f + 0.03f * Stacks)),
				f => 0.55f + 0.45f * f);
			RiftDraw.Head(Projectile, Color.White);
			return false;
		}

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 10; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Vortex,
					Main.rand.NextVector2Circular(3.2f, 3.2f), 120, default, 1f);
				d.noGravity = true;
			}
		}
	}
}
