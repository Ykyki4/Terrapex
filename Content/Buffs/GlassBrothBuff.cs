using Terraria;
using Terraria.ModLoader;

namespace Terrapex.Content.Buffs
{
	/// <summary>
	/// Plan item #50's buff. It stacks the vanilla well-fed slot rather than adding a
	/// second food buff, so a player cannot run both this and a vanilla meal.
	/// </summary>
	public class GlassBrothBuff : ModBuff
	{
		public override void SetStaticDefaults() => Main.buffNoTimeDisplay[Type] = false;

		public override void Update(Player player, ref int buffIndex)
		{
			player.wellFed = true;
			player.GetCritChance(DamageClass.Generic) += 4;
			player.statDefense += 3;
			player.manaRegenBonus += 6;
			player.lifeRegen += 2;
		}
	}
}
