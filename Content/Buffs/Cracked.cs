using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Buffs
{
	// The verb the whole mod is built on: crack it first, then hit it.
	// The damage bonus itself lives in CrackedGlobalNPC.
	public class Cracked : ModBuff
	{
		public const float DamageBonus = 0.15f;

		public override void SetStaticDefaults()
		{
			Main.debuff[Type] = true;
			Main.pvpBuff[Type] = true;
			Main.buffNoSave[Type] = true;
			BuffID.Sets.IsATagBuff[Type] = true;
		}
	}
}
