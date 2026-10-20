using Terraria;
using Terraria.ModLoader;

namespace Terrapex.Content.Buffs
{
	/// <summary>
	/// Shown while the Hermit's Hull has the player tucked in. The numbers live on the accessory's
	/// player (HermitsHullPlayer); this only puts the state where the player can see it.
	/// </summary>
	public class HermitShellBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}
	}
}
