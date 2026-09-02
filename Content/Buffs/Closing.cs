using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Buffs
{
	/// <summary>
	/// Closure's whip tag. The mark itself does nothing; what reads it is
	/// <c>TerrapexPlayer.ModifyHitNPCWithProj</c>, where a minion striking a marked target
	/// always crits. Kept as a real buff rather than a field so the player can see which enemy
	/// the whip actually caught.
	/// </summary>
	public class Closing : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.debuff[Type] = true;
			Main.pvpBuff[Type] = true;
			Main.buffNoSave[Type] = true;
			BuffID.Sets.IsATagBuff[Type] = true;
		}
	}
}
