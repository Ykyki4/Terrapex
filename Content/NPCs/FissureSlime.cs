using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Items.Placeable;

namespace Terrapex.Content.NPCs
{
	/// <summary>
	/// Plan mob #1. A slime with rock in it. Killing it does not end the fight: it comes apart
	/// into three smaller ones, which is the first thing this mod teaches - that breaking a
	/// thing open is how most of it works.
	///
	/// One class, not two. The small form is the same NPC with <see cref="Split"/> set, resized
	/// in <c>OnSpawn</c>; a second type would mean a second sprite sheet for what is visibly
	/// the same creature.
	/// </summary>
	public class FissureSlime : ModNPC
	{
		private const int Shards = 3;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 2;
			NPCID.Sets.NPCBestiaryDrawModifiers value = new() { Position = new Vector2(0f, 12f) };
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
		}

		public override void SetDefaults()
		{
			NPC.width = 30;
			NPC.height = 24;
			NPC.damage = 14;
			NPC.defense = 2;
			NPC.lifeMax = 45;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.value = Item.buyPrice(copper: 60);
			NPC.knockBackResist = 0.5f;
			NPC.aiStyle = NPCAIStyleID.Slime;
			AIType = NPCID.BlueSlime;
			AnimationType = NPCID.BlueSlime;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Underground,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.FissureSlime.Bestiary")
			});
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.PlayerInTown || spawnInfo.Water)
				return 0f;
			return spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight
				? 0.09f : 0f;
		}

		public override void AI()
		{
			if (Main.rand.NextBool(14))
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height,
					DustID.PurpleTorch, 0f, 0f, 140, default, 0.8f * NPC.scale);
				d.noGravity = true;
			}
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 14 : 3); i++)
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Stone,
					hit.HitDirection, -1f, 100, default, NPC.scale);

			if (NPC.life > 0 || Main.netMode == NetmodeID.MultiplayerClient)
				return;

			SoundEngine.PlaySound(SoundID.NPCDeath1, NPC.Center);
			for (int i = 0; i < Shards; i++)
			{
				Vector2 kick = new Vector2(Main.rand.NextFloat(-3.5f, 3.5f), Main.rand.NextFloat(-4f, -1f));
				int who = NPC.NewNPC(NPC.GetSource_Death(), (int)NPC.Center.X, (int)NPC.Center.Y,
					ModContent.NPCType<FissureSlimelet>());
				if (who >= Main.maxNPCs)
					continue;
				Main.npc[who].velocity = kick;
				if (Main.netMode == NetmodeID.Server)
					NetMessage.SendData(MessageID.SyncNPC, number: who);
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ItemID.Gel, 1, 1, 2));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FissuriteOre>(), 4, 1, 2));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RiftDust>(), 3, 1, 2));
		}
	}
}
