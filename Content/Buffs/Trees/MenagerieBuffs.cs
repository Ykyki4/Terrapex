using Terraria;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Trees.Summon;

namespace Terrapex.Content.Buffs.Trees
{
	/// <summary>
	/// The Menagerie's four minion buffs. Each keeps its own beasts alive; from the Wild Court on,
	/// the buff is also where "the court in session" pays out - +15% summon damage while all four
	/// forms are on the field. It is done here rather than in the minion because buffs update
	/// after ResetEffects and before projectiles, so the bonus is in place for every minion hit
	/// in the same tick.
	/// </summary>
	public abstract class MenagerieBuff : ModBuff
	{
		protected abstract int BeastType { get; }
		protected virtual bool Court => true;

		public override void SetStaticDefaults()
		{
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.ownedProjectileCounts[BeastType] > 0)
			{
				player.buffTime[buffIndex] = 18000;
				if (Court && MenagerieBeast.Assembled(player, BeastType))
					player.GetDamage(DamageClass.Summon) += MenagerieBeast.CourtBonus;
				return;
			}
			player.DelBuff(buffIndex);
			buffIndex--;
		}
	}

	public class MenagerieHornBuff : MenagerieBuff
	{
		protected override int BeastType => ModContent.ProjectileType<HornBeast>();
		protected override bool Court => false;
	}

	public class WildCourtBuff : MenagerieBuff
	{
		protected override int BeastType => ModContent.ProjectileType<CourtBeast>();
	}

	public class GroveConclaveBuff : MenagerieBuff
	{
		protected override int BeastType => ModContent.ProjectileType<ConclaveBeast>();
	}

	public class ArkBuff : MenagerieBuff
	{
		protected override int BeastType => ModContent.ProjectileType<ArkBeast>();
	}
}
