using Terraria;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Weapons.Trees.Reaper
{
	/// <summary>
	/// What the Harvest Moon tree remembers about one reaper: which rite comes next, and how much of
	/// the Long Night is left. Kept on the player rather than on the item, so swapping between two
	/// scythes of the tree carries the cycle on instead of restarting it - the moon belongs to the
	/// reaper, not to the blade.
	///
	/// Nothing here is synced. The rite and the night are only ever read on the owner's client, in
	/// <c>StrikeAt</c> and <c>AfterStrike</c>, which is the only client that knows where the cut went.
	/// </summary>
	public class MoonTreePlayer : ModPlayer
	{
		/// <summary>Three seconds of night after a Long Night or Event Horizon Reap.</summary>
		public const int NightLength = 180;

		/// <summary>The next rite of the cycle, before the modulo of the scythe's own rite count.</summary>
		public int rite;

		public int nightTicks;

		/// <summary>Ticks since the night began - the draw layer fades the veil in on it.</summary>
		public int nightAge;

		public bool NightFalls => nightTicks > 0;

		public void BeginNight()
		{
			nightTicks = NightLength;
			nightAge = 0;
		}

		public override void PostUpdate()
		{
			if (nightTicks > 0)
			{
				nightTicks--;
				nightAge++;
			}
		}

		public override void UpdateDead() => nightTicks = 0;
	}
}
