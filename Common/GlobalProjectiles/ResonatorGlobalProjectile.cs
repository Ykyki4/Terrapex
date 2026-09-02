using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Accessories;

namespace Terrapex.Common.GlobalProjectiles
{
	/// <summary>
	/// The Shard Resonator: every fifth shot is fired twice.
	///
	/// Minions, sentries and whips are excluded. A minion that doubled itself would break the
	/// slot budget outright, and a whip is one swing rather than a shot, so doubling it just
	/// stacks two identical arcs on the same pixel.
	/// </summary>
	public class ResonatorGlobalProjectile : GlobalProjectile
	{
		/// <summary>Set while the copy is being spawned, or the copy counts itself.</summary>
		private static bool duplicating;

		public override void OnSpawn(Projectile projectile, IEntitySource source)
		{
			if (duplicating || projectile.owner != Main.myPlayer)
				return;
			if (!projectile.friendly || projectile.hostile || projectile.damage <= 0)
				return;
			if (projectile.minion || projectile.sentry || projectile.aiStyle == 0)
				return;
			if (ProjectileID.Sets.IsAWhip[projectile.type])
				return;

			Player owner = Main.player[projectile.owner];
			if (!owner.active)
				return;
			TerrapexPlayer mp = owner.GetModPlayer<TerrapexPlayer>();
			if (!mp.shardResonator)
				return;

			if (++mp.resonatorCount < ShardResonator.Every)
				return;
			mp.resonatorCount = 0;

			duplicating = true;
			Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center,
				projectile.velocity.RotatedByRandom(0.14f), projectile.type,
				projectile.damage, projectile.knockBack, projectile.owner);
			duplicating = false;

			for (int i = 0; i < 6; i++)
			{
				Dust d = Dust.NewDustPerfect(projectile.Center, DustID.Vortex,
					Main.rand.NextVector2Circular(1.8f, 1.8f), 130, default, 0.8f);
				d.noGravity = true;
			}
		}
	}
}
