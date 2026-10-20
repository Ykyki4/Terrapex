using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Projectiles.Wilds
{
	/// <summary>The Pitcher Snapper's acid: a lobbed glob that does not steer, and poisons on a hit.</summary>
	public class PitcherSpit : ModProjectile
	{
		public const float Gravity = 0.22f;

		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.hostile = true;
			Projectile.penetrate = 1;
			Projectile.tileCollide = true;
			Projectile.timeLeft = 180;
		}

		public override void AI()
		{
			Projectile.velocity.Y += Gravity;
			Projectile.rotation += 0.15f;
			if (Main.rand.NextBool(3))
				Dust.NewDustPerfect(Projectile.Center, DustID.GreenMoss, Projectile.velocity * 0.1f, 100, default, 0.9f).noGravity = true;
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(BuffID.Poisoned, 180);

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 8; i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.GreenMoss, Main.rand.NextVector2Circular(2.5f, 2.5f), 90, default, 1f);
		}
	}
}
