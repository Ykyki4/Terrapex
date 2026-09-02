using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.NPCs
{
	/// <summary>
	/// Mini-boss, T3. It walks with three <see cref="PlateBearer"/>s and keeps them in plates.
	///
	/// It does not shelter behind them and they do not gate its health bar - that is the
	/// mistake the Weaver was rebuilt to undo. What it does is *re-arm* them: every eight
	/// seconds the nearest bearer that has lost its plate gets a fresh one. So stripping the
	/// escort is worth doing and worth doing fast, and ignoring the escort is a real option
	/// that costs you time rather than a wall that costs you the fight.
	/// </summary>
	public class PlateShepherd : ModNPC
	{
		private const int Escort = 3;
		private const int RearmEvery = 60 * 8;
		private const float Reach = 520f;

		private ref float Beat => ref NPC.ai[0];
		private ref float Spawned => ref NPC.ai[1];

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 4;
			NPCID.Sets.MPAllowedEnemies[Type] = true;
		}

		public override void SetDefaults()
		{
			NPC.width = 44;
			NPC.height = 46;
			NPC.damage = 50;
			NPC.defense = 22;
			NPC.lifeMax = 1600;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.value = Item.buyPrice(gold: 2);
			NPC.knockBackResist = 0.05f;
			NPC.aiStyle = NPCAIStyleID.Fighter;
			AIType = NPCID.Zombie;
			NPC.rarity = 3;
			NPC.npcSlots = 5f;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Underground,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.PlateShepherd.Bestiary")
			});
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.PlayerInTown || spawnInfo.Water || !Main.hardMode)
				return 0f;
			if (NPC.AnyNPCs(Type))
				return 0f;
			return spawnInfo.Player.ZoneRockLayerHeight ? 0.0030f : 0f;
		}

		public override void AI()
		{
			Beat++;
			NPC.spriteDirection = NPC.direction;

			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			// the escort arrives with it, once
			if (Spawned == 0f)
			{
				Spawned = 1f;
				for (int i = 0; i < Escort; i++)
				{
					int who = NPC.NewNPC(NPC.GetSource_FromAI(),
						(int)NPC.Center.X + Main.rand.Next(-70, 71), (int)NPC.Center.Y,
						ModContent.NPCType<PlateBearer>());
					if (who < Main.maxNPCs && Main.netMode == NetmodeID.Server)
						NetMessage.SendData(MessageID.SyncNPC, number: who);
				}
				NPC.netUpdate = true;
			}

			if (Beat % RearmEvery != 0f)
				return;

			// one fresh plate, to the nearest bearer that has lost its own
			int type = ModContent.NPCType<PlateBearer>(), best = -1;
			float dist = Reach;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.type != type || n.ModNPC is not PlateBearer bearer || bearer.Shielded)
					continue;
				float d = Vector2.Distance(n.Center, NPC.Center);
				if (d < dist) { dist = d; best = i; }
			}
			if (best < 0 || Main.npc[best].ModNPC is not PlateBearer rearm)
				return;

			rearm.Rearm();
			SoundEngine.PlaySound(SoundID.Item37, NPC.Center);
		}

		/// <summary>
		/// Own framing rather than <c>AnimationType</c>. Borrowing the Zombie's animation means
		/// borrowing its sixteen frames, and the game then reads frame.Y past the end of a
		/// four-frame sheet. aiStyle and AIType still do the walking; only the frames are ours.
		/// </summary>
		public override void FindFrame(int frameHeight)
		{
			if (System.Math.Abs(NPC.velocity.X) < 0.1f)
			{
				NPC.frameCounter = 0.0;
				NPC.frame.Y = 0;
				return;
			}
			NPC.frameCounter += System.Math.Abs(NPC.velocity.X) * 0.32;
			if (NPC.frameCounter < 3.0)
				return;
			NPC.frameCounter = 0.0;
			NPC.frame.Y = (NPC.frame.Y + frameHeight) % (frameHeight * Main.npcFrameCount[Type]);
		}

		public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			// a thread of light to each bearer it is still supplying, so the relationship is
			// visible and killing the escort reads as cutting something
			int type = ModContent.NPCType<PlateBearer>();
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.type != type || Vector2.Distance(n.Center, NPC.Center) > Reach)
					continue;
				float pulse = 0.10f + 0.06f * (float)Math.Sin(Beat * 0.05f + i);
				RiftDraw.Line(NPC.Center, n.Center, RiftDraw.Glow(230, 130, 235, pulse), 2.2f);
			}
			RiftDraw.Bloom(NPC.Center, RiftDraw.Glow(255, 154, 217, 0.22f), 0.7f);
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 40 : 5); i++)
				Dust.NewDust(NPC.position, NPC.width, NPC.height,
					Main.rand.NextBool(3) ? DustID.PurpleTorch : DustID.Stone,
					hit.HitDirection, -1f, 100, default, 1.3f);
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
			=> npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<PlateShard>(), 1, 10, 18));
	}
}
