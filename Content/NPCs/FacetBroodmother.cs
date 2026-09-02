using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Accessories;
using Terrapex.Content.Items.Placeable;

namespace Terrapex.Content.NPCs
{
	/// <summary>
	/// Mini-boss, T1. A beetle that got old, and it keeps laying more.
	///
	/// It exists to solve a supply problem rather than to be a fight: fissurite runs out, and
	/// walking to the next vein is not gameplay. She is a rare spawn nobody summons, and killing
	/// her pays a tier's worth of ore in one go plus the carapace.
	///
	/// The brood is capped and the cap is low. An add-spawner with no ceiling stops being a
	/// fight and becomes a decision about when to leave, which is not the decision this is for.
	/// </summary>
	public class FacetBroodmother : ModNPC
	{
		private const int BroodCap = 4;
		private const int LayEvery = 150;

		private ref float Beat => ref NPC.ai[0];

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 4;
			NPCID.Sets.MPAllowedEnemies[Type] = true;
		}

		public override void SetDefaults()
		{
			NPC.width = 54;
			NPC.height = 44;
			NPC.damage = 30;
			NPC.defense = 12;
			NPC.lifeMax = 700;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.value = Item.buyPrice(silver: 60);
			NPC.knockBackResist = 0.1f;
			NPC.noGravity = true;
			NPC.noTileCollide = false;
			NPC.aiStyle = -1;
			NPC.rarity = 2;
			NPC.npcSlots = 4f;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Underground,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.FacetBroodmother.Bestiary")
			});
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.PlayerInTown || spawnInfo.Water)
				return 0f;
			if (NPC.AnyNPCs(Type))
				return 0f;
			return spawnInfo.Player.ZoneRockLayerHeight ? 0.0035f : 0f;
		}

		private int Brood()
		{
			int type = ModContent.NPCType<FacetBeetle>(), n = 0;
			for (int i = 0; i < Main.maxNPCs; i++)
				if (Main.npc[i].active && Main.npc[i].type == type)
					n++;
			return n;
		}

		public override void AI()
		{
			NPC.TargetClosest();
			Player target = Main.player[NPC.target];
			Beat++;

			Vector2 want = target.Center - NPC.Center;
			float dist = want.Length();
			if (dist > 8f)
			{
				Vector2 aim = want / dist * 3.1f;
				aim.Y += (float)Math.Sin(Beat * 0.05f) * 1.1f;
				NPC.velocity = Vector2.Lerp(NPC.velocity, aim, 0.03f);
			}

			NPC.spriteDirection = NPC.direction = NPC.velocity.X > 0f ? 1 : -1;
			NPC.rotation = NPC.velocity.X * 0.03f;
			Lighting.AddLight(NPC.Center, 0.30f, 0.12f, 0.38f);

			if (Beat % LayEvery != 0f || Main.netMode == NetmodeID.MultiplayerClient)
				return;
			if (Brood() >= BroodCap)
				return;

			SoundEngine.PlaySound(SoundID.NPCDeath1 with { Pitch = 0.6f }, NPC.Center);
			int who = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y + 8,
				ModContent.NPCType<FacetBeetle>());
			if (who < Main.maxNPCs && Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.SyncNPC, number: who);
		}

		public override void FindFrame(int frameHeight)
		{
			if (++NPC.frameCounter >= 5.0)
			{
				NPC.frameCounter = 0.0;
				NPC.frame.Y = (NPC.frame.Y + frameHeight) % (frameHeight * Main.npcFrameCount[Type]);
			}
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 34 : 5); i++)
			{
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.PurpleTorch,
					hit.HitDirection, -1f, 110, default, 1.3f);
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FissuriteOre>(), 1, 14, 22));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FacetCarapace>(), 1));
		}
	}
}
