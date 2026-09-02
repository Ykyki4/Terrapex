using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;

namespace Terrapex.Common.GlobalNPCs
{
	/// <summary>Applies the Rift Mark's bonus, and draws the tell that says it is on.</summary>
	public class RiftMarkGlobalNPC : GlobalNPC
	{
		public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
		{
			if (npc.HasBuff(ModContent.BuffType<RiftMark>()))
				modifiers.FinalDamage *= 1f + RiftMark.Bonus;
		}

		public override void DrawEffects(NPC npc, ref Color drawColor)
		{
			if (!npc.HasBuff(ModContent.BuffType<RiftMark>()))
				return;
			if (!Main.rand.NextBool(6))
				return;

			Dust d = Dust.NewDustDirect(npc.position, npc.width, npc.height,
				Terraria.ID.DustID.PurpleTorch, 0f, 0f, 120, default, 0.9f);
			d.noGravity = true;
			d.velocity *= 0.25f;
		}
	}
}
