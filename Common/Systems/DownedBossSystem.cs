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

		public override void OnWorldLoad()
		{
			downedDormantEye = false;
			downedKeeper = false;
			downedWeaver = false;
			downedFirstKeeper = false;
		}

		public override void OnWorldUnload()
		{
			downedDormantEye = false;
			downedKeeper = false;
			downedWeaver = false;
			downedFirstKeeper = false;
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
		}

		public override void LoadWorldData(TagCompound tag)
		{
			downedDormantEye = tag.ContainsKey("downedDormantEye");
			downedKeeper = tag.ContainsKey("downedKeeper");
			downedWeaver = tag.ContainsKey("downedWeaver");
			downedFirstKeeper = tag.ContainsKey("downedFirstKeeper");
		}

		public override void NetSend(BinaryWriter writer)
		{
			var flags = new BitsByte();
			flags[0] = downedKeeper;
			flags[1] = downedDormantEye;
			flags[2] = downedWeaver;
			flags[3] = downedFirstKeeper;
			writer.Write(flags);
		}

		public override void NetReceive(BinaryReader reader)
		{
			BitsByte flags = reader.ReadByte();
			downedKeeper = flags[0];
			downedDormantEye = flags[1];
			downedWeaver = flags[2];
			downedFirstKeeper = flags[3];
		}
	}
}
