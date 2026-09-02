using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Buffs
{
	/// <summary>
	/// Plan item #59's debuff. It does not tick damage — it makes the target take more, and
	/// that is deliberate: the Rib is a support bow, and a bow that also burns would leave
	/// the tier's other ranged weapon with nothing to be.
	/// </summary>
	public class RiftMark : ModBuff
	{
		/// <summary>How much more a marked target takes from every source.</summary>
		public const float Bonus = 0.12f;

		public override void SetStaticDefaults()
		{
			Main.debuff[Type] = true;
			Main.buffNoSave[Type] = true;
		}
	}
}
