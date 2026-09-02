using Terraria;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;
using Terrapex.Content.Projectiles;

namespace Terrapex.Common.GlobalNPCs
{
	/// <summary>
	/// Turns the Glassthread's tag into what it promises: minions crit a tagged target far
	/// more often. Rolled here rather than on the whip, because the whip is long gone by
	/// the time the minions arrive.
	/// </summary>
	public class GlassthreadGlobalNPC : GlobalNPC
	{
		public override void ModifyHitByProjectile(NPC npc, Projectile projectile,
			ref NPC.HitModifiers modifiers)
		{
			if (!npc.HasBuff(ModContent.BuffType<GlassthreadTag>()))
				return;
			if (projectile.DamageType != DamageClass.Summon && !projectile.minion)
				return;

			if (Main.rand.Next(100) < GlassthreadWhip.TagCrit)
				modifiers.SetCrit();
		}
	}
}
