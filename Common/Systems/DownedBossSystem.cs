using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Terrapex.Common.Systems
{
	public class DownedBossSystem : ModSystem
	{
		public static bool downedDormantEye;
		public static bool downedKeeper;
		public static bool downedWeaver;
		public static bool downedFirstKeeper;
		public static bool downedHarvester;
		public static bool downedShellback;
		public static bool downedScarab;
		public static bool downedRime;

		public override void OnWorldLoad()
		{
			downedDormantEye = false;
			downedKeeper = false;
			downedWeaver = false;
			downedFirstKeeper = false;
			downedHarvester = false;
			downedShellback = false;
			downedScarab = false;
			downedRime = false;
		}

		public override void OnWorldUnload()
		{
			downedDormantEye = false;
			downedKeeper = false;
			downedWeaver = false;
			downedFirstKeeper = false;
			downedHarvester = false;
			downedShellback = false;
			downedScarab = false;
			downedRime = false;
		}

		public override void SaveWorldData(TagCompound tag)
		{
			if (downedDormantEye)
				tag["downedDormantEye"] = true;
			if (downedKeeper)
				tag["downedKeeper"] = true;
			if (downedWeaver)
				tag["downedWeaver"] = true;
			if (downedFirstKeeper)
				tag["downedFirstKeeper"] = true;
			if (downedHarvester)
				tag["downedHarvester"] = true;
			if (downedShellback)
				tag["downedShellback"] = true;
			if (downedScarab)
				tag["downedScarab"] = true;
			if (downedRime)
				tag["downedRime"] = true;
		}

		public override void LoadWorldData(TagCompound tag)
		{
			downedDormantEye = tag.ContainsKey("downedDormantEye");
			downedKeeper = tag.ContainsKey("downedKeeper");
			downedWeaver = tag.ContainsKey("downedWeaver");
			downedFirstKeeper = tag.ContainsKey("downedFirstKeeper");
			downedHarvester = tag.ContainsKey("downedHarvester");
			downedShellback = tag.ContainsKey("downedShellback");
			downedScarab = tag.ContainsKey("downedScarab");
			downedRime = tag.ContainsKey("downedRime");
		}

		public override void NetSend(BinaryWriter writer)
		{
			var flags = new BitsByte();
			flags[0] = downedKeeper;
			flags[1] = downedDormantEye;
			flags[2] = downedWeaver;
			flags[3] = downedFirstKeeper;
			flags[4] = downedHarvester;
			flags[5] = downedShellback;
			flags[6] = downedScarab;
			flags[7] = downedRime;
			writer.Write(flags);
		}

		public override void NetReceive(BinaryReader reader)
		{
			BitsByte flags = reader.ReadByte();
			downedKeeper = flags[0];
			downedDormantEye = flags[1];
			downedWeaver = flags[2];
			downedFirstKeeper = flags[3];
			downedHarvester = flags[4];
			downedShellback = flags[5];
			downedScarab = flags[6];
			downedRime = flags[7];
		}
	}
}
