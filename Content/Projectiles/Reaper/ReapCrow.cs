using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;

namespace Terrapex.Content.Projectiles.Reaper
{
	/// <summary>
	/// A straw crow on the reaper's side: the Hollow Harvester's own projectile, borrowed sheet and
	/// all, for the Hollow Harvest and the Crow Charm. It flies straight and pecks two things.
	/// </summary>
	public class ReapCrow : ModProjectile
	{
		public override string Texture => "Terrapex/Content/Projectiles/Harvester/HarvesterCrow";

		public override void SetStaticDefaults() => Main.projFrames[Type] = 2;

		public override void SetDefaults()
		{
			Projectile.width = 18;
			Projectile.height = 18;
			Projectile.friendly = true;
			Projectile.DamageType = ModContent.GetInstance<ReapingDamage>();
			Projectile.penetrate = 2;
			Projectile.tileCollide = true;
			Projectile.timeLeft = 90;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void AI()
		{
			Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;
			Projectile.rotation = Projectile.velocity.ToRotation() + (Projectile.spriteDirection < 0 ? MathHelper.Pi : 0f);
			if (++Projectile.frameCounter >= 5)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => ReapingSlash.RangedHit(Projectile, target, hit);

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 4; i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.Hay, Main.rand.NextVector2Circular(2f, 2f), 80, default, 0.8f);
		}
	}
}
