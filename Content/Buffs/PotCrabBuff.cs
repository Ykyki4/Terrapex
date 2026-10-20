using Terraria;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Shellback;

namespace Terrapex.Content.Buffs
{
	/// <summary>The Rusted Crab Pot's minion buff.</summary>
	public class PotCrabBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.ownedProjectileCounts[ModContent.ProjectileType<PotCrabMinion>()] > 0)
			{
				player.buffTime[buffIndex] = 18000;
				return;
			}
			player.DelBuff(buffIndex);
			buffIndex--;
		}
	}
}
