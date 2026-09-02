using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Buffs
{
	public class ShardlingBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = true;
			Main.vanityPet[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			player.buffTime[buffIndex] = 18000;
			int type = ModContent.ProjectileType<ShardlingCompanion>();
			if (player.whoAmI == Main.myPlayer && player.ownedProjectileCounts[type] <= 0)
				Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.Center,
					Vector2.Zero, type, 0, 0f, player.whoAmI);
		}
	}
}
