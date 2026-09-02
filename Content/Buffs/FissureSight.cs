using Terraria;
using Terraria.ModLoader;
using Terrapex.Common.Players;

namespace Terrapex.Content.Buffs
{
	public class FissureSight : ModBuff
	{
		public override void SetStaticDefaults() => Main.buffNoTimeDisplay[Type] = false;

		public override void Update(Player player, ref int buffIndex)
			=> player.GetModPlayer<TerrapexPlayer>().fissureSight = true;
	}
}
