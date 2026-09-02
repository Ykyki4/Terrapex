using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Buffs
{
	/// <summary>
	/// The Glassthread's tag. Unlike a damage tag it does nothing on its own — the crit is
	/// applied by <see cref="Common.GlobalNPCs.GlassthreadGlobalNPC"/> when a summon hits.
	/// </summary>
	public class GlassthreadTag : ModBuff
	{
		public override void SetStaticDefaults()
		{
			Main.debuff[Type] = true;
			Main.buffNoSave[Type] = true;
			BuffID.Sets.IsATagBuff[Type] = true;
		}
	}
}
