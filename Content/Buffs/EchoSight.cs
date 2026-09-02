using Terraria;
using Terraria.ModLoader;

namespace Terrapex.Content.Buffs
{
	public class EchoSight : ModBuff
	{
		public override void SetStaticDefaults() => Main.buffNoTimeDisplay[Type] = false;

		public override void Update(Player player, ref int buffIndex) => player.detectCreature = true;
	}
}
