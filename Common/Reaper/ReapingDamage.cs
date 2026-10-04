using Terraria.ModLoader;

namespace Terrapex.Common.Reaper
{
	/// <summary>
	/// The fifth class: scythes. See <c>CLASS_REAPER.md</c>.
	///
	/// It stands on its own rather than inheriting melee. A melee subclass would let the warrior
	/// emblem and every melee accessory carry it, and then the class would only be a sword with a
	/// longer animation; its own gear is what makes picking it a decision.
	/// </summary>
	public class ReapingDamage : DamageClass
	{
		public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
			=> damageClass == Generic ? StatInheritanceData.Full : StatInheritanceData.None;

		public override bool GetEffectInheritance(DamageClass damageClass) => false;
	}
}
