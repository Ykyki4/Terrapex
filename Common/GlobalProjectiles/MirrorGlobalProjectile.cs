using Terraria;
using Terraria.ModLoader;

namespace Terrapex.Common.GlobalProjectiles
{
	/// <summary>
	/// Backs the Mirror Charm. The roll has to happen once per projectile, not once per
	/// tick it spends inside the player, or a slow bolt would be reflected with certainty
	/// and a fast one almost never — the exact opposite of what the charm should do.
	/// </summary>
	public class MirrorGlobalProjectile : GlobalProjectile
	{
		public override bool InstancePerEntity => true;

		public bool mirrorRolled;
	}
}
