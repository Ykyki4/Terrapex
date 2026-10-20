using Terraria;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Rime;

namespace Terrapex.Content.Buffs
{
	/// <summary>The Glacier Bell's spirit is with you. Ends when the last one is gone.</summary>
	public class RimeSpriteBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.ownedProjectileCounts[ModContent.ProjectileType<RimeSprite>()] > 0)
				player.buffTime[buffIndex] = 18000;
			else
			{
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}
}
