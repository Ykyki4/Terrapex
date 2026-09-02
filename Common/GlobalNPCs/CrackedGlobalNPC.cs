using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;

namespace Terrapex.Common.GlobalNPCs
{
	public class CrackedGlobalNPC : GlobalNPC
	{
		public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
		{
			if (npc.HasBuff(ModContent.BuffType<Cracked>()))
				modifiers.FinalDamage *= 1f + Cracked.DamageBonus;
		}

		public override void PostAI(NPC npc)
		{
			// the debuff has to be visible on the target, or the set bonus is a
			// number in a tooltip that the player never sees happen
			if (!npc.HasBuff(ModContent.BuffType<Cracked>()) || !Main.rand.NextBool(7))
				return;

			Dust d = Dust.NewDustDirect(npc.position, npc.width, npc.height,
				DustID.PurpleTorch, 0f, 0f, 130, default, 0.9f);
			d.noGravity = true;
			d.velocity *= 0.3f;
		}
	}
}
