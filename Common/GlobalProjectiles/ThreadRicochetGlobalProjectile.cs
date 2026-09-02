using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.GlobalNPCs;
using Terrapex.Common.Players;

namespace Terrapex.Common.GlobalProjectiles
{
	/// <summary>
	/// The Darner Visor's set bonus: a ranged hit on a stitched target throws a second, weaker
	/// shot straight down the thread at the other end.
	///
	/// This is what makes the ranged head play differently rather than just carry a bigger
	/// number — stitching two things turns every shot into two, so the ranged build is paid
	/// for setting the thread up first.
	/// </summary>
	public class ThreadRicochetGlobalProjectile : GlobalProjectile
	{
		/// <summary>Share of the original that rides the thread.</summary>
		public const float Share = 0.5f;

		/// <summary>Set while the echo is spawned, or the echo ricochets off its own hit.</summary>
		private static bool bouncing;

		public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (bouncing || projectile.owner != Main.myPlayer)
				return;
			if (projectile.DamageType != DamageClass.Ranged || projectile.minion || projectile.sentry)
				return;

			Player owner = Main.player[projectile.owner];
			if (!owner.active || !owner.GetModPlayer<TerrapexPlayer>().darnerRanged)
				return;

			BoundGlobalNPC bound = target.GetGlobalNPC<BoundGlobalNPC>();
			if (!bound.IsBound)
				return;

			NPC other = Main.npc[bound.Partner];
			Vector2 dir = other.Center - target.Center;
			if (dir.LengthSquared() < 4f)
				return;

			bouncing = true;
			Projectile.NewProjectile(projectile.GetSource_FromThis(), target.Center,
				Vector2.Normalize(dir) * MathHelper.Max(projectile.velocity.Length(), 8f),
				projectile.type, (int)(projectile.damage * Share), projectile.knockBack,
				projectile.owner);
			bouncing = false;

			for (int i = 0; i < 6; i++)
			{
				Dust d = Dust.NewDustPerfect(target.Center, DustID.Vortex,
					Vector2.Normalize(dir).RotatedByRandom(0.5f) * Main.rand.NextFloat(1f, 3f),
					110, default, 0.9f);
				d.noGravity = true;
			}
		}
	}
}
