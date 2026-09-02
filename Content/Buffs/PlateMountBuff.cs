using Terraria;
using Terraria.ModLoader;
using Terrapex.Content.Mounts;

namespace Terrapex.Content.Buffs
{
	public class PlateMountBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			player.mount.SetMount(ModContent.MountType<PlateMount>(), player);
			player.buffTime[buffIndex] = 10;
		}
	}
}
