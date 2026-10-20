using Terraria;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;

namespace Terrapex.Content.Buffs
{
	/// <summary>The Harvest Potion's buff: the reaper's Wrath, Archery and Magic Power.</summary>
	public class HarvestBuff : ModBuff
	{
		public const float DamageBonus = 0.10f;
		public const float GainBonus = 0.20f;

		public override void Update(Player player, ref int buffIndex)
		{
			player.GetDamage<ReapingDamage>() += DamageBonus;
			player.GetModPlayer<ReaperPlayer>().harvestGain += GainBonus;
		}
	}
}
