using Terraria;
using Terraria.ModLoader;
using Terrapex.Common.Players;

namespace Terrapex.Common.GlobalProjectiles
{
	/// <summary>
	/// Backs the Shard Accelerator. Only ordinary shots wind up: minions, whips and held
	/// projectiles are excluded, because speeding those up breaks how they are aimed rather
	/// than making them better.
	/// </summary>
	public class AcceleratorGlobalProjectile : GlobalProjectile
	{
		public override bool InstancePerEntity => true;

		private const float MaxBoost = 1.45f;
		private const float PerTick = 1.012f;
		private const int Delay = 12;

		private float launchSpeed;
		private int age;

		public override void AI(Projectile projectile)
		{
			if (!projectile.friendly || projectile.minion || projectile.sentry || projectile.aiStyle == 0)
				return;
			if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
				return;

			Player owner = Main.player[projectile.owner];
			if (!owner.active || !owner.GetModPlayer<TerrapexPlayer>().shardAccelerator)
				return;

			if (launchSpeed == 0f)
			{
				launchSpeed = projectile.velocity.Length();
				if (launchSpeed <= 0.05f)
					launchSpeed = -1f;   // stationary things are left alone for good
			}
			if (launchSpeed < 0f)
				return;

			if (++age < Delay)
				return;
			if (projectile.velocity.Length() < launchSpeed * MaxBoost)
				projectile.velocity *= PerTick;
		}
	}
}
