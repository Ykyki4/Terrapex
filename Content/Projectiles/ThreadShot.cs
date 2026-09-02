using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.GlobalNPCs;
using Terrapex.Common.Players;

namespace Terrapex.Content.Projectiles
{
	/// <summary>The Threadcaster's round. It carries the needle, not the damage.</summary>
	public class ThreadShot : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 12;
			ProjectileID.Sets.TrailingMode[Type] = 3;
		}

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 240;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			Lighting.AddLight(Projectile.Center, 0.12f, 0.34f, 0.32f);

			if (Main.rand.NextBool(3))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Vortex,
					Projectile.velocity * -0.05f, 140, default, 0.7f);
				d.noGravity = true;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			NPC other = Main.player[Projectile.owner].GetModPlayer<TerrapexPlayer>().TakeSeamTarget(target);
			if (other != null)
				BoundGlobalNPC.Bind(other, target);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// a long thin tail, because the shot is literally paying out thread behind it
			RiftDraw.Trail(Projectile,
				f => RiftDraw.Glow(40, 170, 158, f * 0.55f),
				f => 0.30f + 0.30f * f);
			RiftDraw.Head(Projectile, Color.White);
			return false;
		}

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 6; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Vortex,
					Main.rand.NextVector2Circular(2.4f, 2.4f), 120, default, 0.85f);
				d.noGravity = true;
			}
		}
	}
}
