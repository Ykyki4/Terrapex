using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Systems;
using Terrapex.Content.NPCs;

namespace Terrapex.Common.GlobalNPCs
{
	/// <summary>
	/// Marks the creatures that came out of a Breach mouth, so the event can count its own
	/// kills and a mouth can tell how much of its brood is still alive.
	///
	/// The mark matters because the Breach happens in the cavern, where the ordinary spawn pool
	/// is running too. Counting every Riftling that dies during the event would let a player
	/// finish a wave on the mobs the world was going to send anyway.
	/// </summary>
	public class BreachGlobalNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		/// <summary>True when a mouth emitted this one.</summary>
		public bool FromBreach;

		/// <summary>Index of the mouth that emitted it, or -1.</summary>
		public int Parent = -1;

		public override void OnKill(NPC npc)
		{
			if (!FromBreach || Main.netMode == NetmodeID.MultiplayerClient)
				return;

			BreachSystem.CountKill();

			if (Parent >= 0 && Parent < Main.maxNPCs && Main.npc[Parent].active
				&& Main.npc[Parent].ModNPC is BreachMouth mouth)
				mouth.Recount();
		}
	}
}
