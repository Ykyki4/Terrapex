using Terraria;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Buffs
{
	public class SpindleBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.ownedProjectileCounts[ModContent.ProjectileType<SpindleMinion>()] > 0)
			{
				player.buffTime[buffIndex] = 18000;
				return;
			}

			player.DelBuff(buffIndex);
			buffIndex--;
		}
	}
}
