using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.NPCs
{
	/// <summary>
	/// What a <see cref="FissureSlime"/> comes apart into.
	///
	/// Its own type rather than the parent with a flag in <c>ai</c>, for one reason that decides
	/// it: loot rules are per type. A flagged fragment would drop the parent's table three times
	/// over and turn splitting into the best ore farm in the tier. This one drops nothing, and
	/// it borrows the parent's sheet, so it costs no art.
	/// </summary>
	public class FissureSlimelet : ModNPC
	{
		public override string Texture => "Terrapex/Content/NPCs/FissureSlime";

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 2;
			NPCID.Sets.NPCBestiaryDrawModifiers value = new() { Hide = true };
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
		}

		public override void SetDefaults()
		{
			NPC.width = 18;
			NPC.height = 15;
			NPC.damage = 9;
			NPC.defense = 0;
			NPC.lifeMax = 16;
			NPC.scale = 0.6f;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.value = 0f;
			NPC.knockBackResist = 0.8f;
			NPC.aiStyle = NPCAIStyleID.Slime;
			AIType = NPCID.BlueSlime;
			AnimationType = NPCID.BlueSlime;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
			=> bestiaryEntry.UIInfoProvider = new CommonEnemyUICollectionInfoProvider(
				ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[Type], quickUnlock: true);

		public override float SpawnChance(NPCSpawnInfo spawnInfo) => 0f;

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 8 : 2); i++)
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Stone,
					hit.HitDirection, -1f, 100, default, 0.7f);
		}
	}
}
