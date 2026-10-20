using Terraria;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Scarab;

namespace Terrapex.Content.Buffs
{
	/// <summary>The Beetling Staff's minion buff.</summary>
	public class BeetlingBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.ownedProjectileCounts[ModContent.ProjectileType<BeetlingMinion>()] > 0)
			{
				player.buffTime[buffIndex] = 18000;
				return;
			}
			player.DelBuff(buffIndex);
			buffIndex--;
		}
	}
}
