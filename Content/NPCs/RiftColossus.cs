using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Items.Weapons;

namespace Terrapex.Content.NPCs
{
	/// <summary>
	/// Plan mob #12. Slow, enormous, and it walks through the wall instead of around it.
	///
	/// Block breaking is what makes it frightening rather than merely tanky: you cannot pillar
	/// away from it, and the arena it leaves behind is one it dug. It only breaks what is in
	/// front of its own head, so it carves a corridor instead of erasing a cavern.
	/// </summary>
	public class RiftColossus : ModNPC
	{
		private const int DigEvery = 22;

		private ref float DigTimer => ref NPC.ai[0];
		private ref float Stomp => ref NPC.ai[1];

		public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 6;

		public override void SetDefaults()
		{
			NPC.width = 48;
			NPC.height = 62;
			NPC.damage = 78;
			NPC.defense = 34;
			NPC.lifeMax = 2600;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.value = Item.buyPrice(gold: 2);
			NPC.knockBackResist = 0f;
			NPC.noGravity = false;
			NPC.noTileCollide = false;
			NPC.aiStyle = -1;
			NPC.behindTiles = true;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Caverns,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.RiftColossus.Bestiary")
			});
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.PlayerInTown || spawnInfo.Water || !Terraria.NPC.downedMechBossAny)
				return 0f;
			return spawnInfo.Player.ZoneRockLayerHeight ? 0.022f : 0f;
		}

		public override void AI()
		{
			if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
				NPC.TargetClosest();

			Player target = Main.player[NPC.target];
			int dir = target.Center.X < NPC.Center.X ? -1 : 1;
			NPC.direction = NPC.spriteDirection = dir;

			// slow enough that it is always a decision to stand and fight rather than a chase
			NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, dir * 1.7f, 0.04f);

			// step up over a ledge, since it is too heavy to jump
			if (NPC.collideX && NPC.velocity.Y == 0f)
				NPC.velocity.Y = -6.4f;

			Stomp++;
			if (Stomp % 46f == 0f && NPC.velocity.Y == 0f)
			{
				Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.35f, Pitch = -0.8f }, NPC.Center);
				for (int i = 0; i < 8; i++)
				{
					Dust d = Dust.NewDustPerfect(NPC.Bottom + new Vector2(Main.rand.NextFloat(-24f, 24f), 0f),
						DustID.Stone, new Vector2(Main.rand.NextFloat(-1.4f, 1.4f), -Main.rand.NextFloat(1f, 3f)),
						90, default, 1.2f);
					d.noGravity = false;
				}
			}

			Dig();
			Lighting.AddLight(NPC.Center, 0.16f, 0.34f, 0.30f);
		}

		/// <summary>Chews through whatever is directly ahead of it, one tile at a time.</summary>
		private void Dig()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			if (++DigTimer < DigEvery)
				return;
			DigTimer = 0f;

			int x = (int)((NPC.Center.X + NPC.direction * (NPC.width * 0.5f + 8f)) / 16f);
			int yTop = (int)((NPC.Center.Y - NPC.height * 0.35f) / 16f);

			for (int y = yTop; y <= yTop + 2; y++)
			{
				if (!WorldGen.InWorld(x, y, 10))
					continue;
				Tile tile = Main.tile[x, y];
				if (!tile.HasTile || Main.tileDungeon[tile.TileType] || TileID.Sets.BasicChest[tile.TileType])
					continue;

				WorldGen.KillTile(x, y);
				if (Main.netMode == NetmodeID.Server)
					NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, x, y);
			}
		}

		public override void FindFrame(int frameHeight)
		{
			if (++NPC.frameCounter >= 9.0)
			{
				NPC.frameCounter = 0.0;
				NPC.frame.Y = (NPC.frame.Y + frameHeight) % (frameHeight * Main.npcFrameCount[Type]);
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<VoidThread>(), 1, 2, 5));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RiftScythe>(), 25));
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 22 : 6); i++)
			{
				Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Stone,
					hit.HitDirection, -1f, 90, default, 1.15f);
			}
		}
	}
}
