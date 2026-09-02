using System.IO;
using Terraria.ModLoader;
using Terrapex.Common.Systems;

namespace Terrapex
{
	/// <summary>Message kinds carried by this mod's own packets. Order is the wire format.</summary>
	public enum TerrapexPacket : byte
	{
		/// <summary>Server → clients: the Breach's live wave, kill count and origin.</summary>
		BreachState = 0
	}

	public class Terrapex : Mod
	{
		/// <summary>
		/// The mod's only packet route. World data (<c>ModSystem.NetSend</c>) reaches a client
		/// once, when it joins, so anything that changes while people are already playing — the
		/// Breach's wave counter, for one — has to come through here instead.
		/// </summary>
		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			TerrapexPacket kind = (TerrapexPacket)reader.ReadByte();
			switch (kind)
			{
				case TerrapexPacket.BreachState:
					BreachSystem.ReadState(reader);
					break;
			}
		}
	}
}
