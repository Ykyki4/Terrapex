using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Placeable;

namespace Terrapex.Content.NPCs
{
	/// <summary>
	/// Plan mob #3. It eats the ore, so killing one gives you the ore back.
	///
	/// That is its whole reason to exist: fissurite generates in veins and a player who has
	/// picked their cave clean has no way to get more without walking further. The beetle is
	/// the second tap, and it comes to the player rather than the other way round.
	/// </summary>
	public class FacetBeetle : ModNPC
	{
		private ref float Bob => ref NPC.ai[0];

		public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 4;

		public override void SetDefaults()
		{
			NPC.width = 26;
			NPC.height = 22;
			NPC.damage = 12;
			NPC.defense = 4;
			NPC.lifeMax = 38;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.value = Item.buyPrice(copper: 90);
			NPC.knockBackResist = 0.6f;
			NPC.noGravity = true;
			NPC.noTileCollide = false;
			NPC.aiStyle = -1;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Underground,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.FacetBeetle.Bestiary")
			});
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.PlayerInTown || spawnInfo.Water)
				return 0f;
			return spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight
				? 0.07f : 0f;
		}

		public override void AI()
		{
			NPC.TargetClosest();
			Player target = Main.player[NPC.target];
			Bob++;

			// it drifts in rather than charging: at this tier the player has a copper sword and
			// nothing that outruns them yet should also hit hard
			Vector2 want = target.Center - NPC.Center;
			float dist = want.Length();
			if (dist > 6f)
			{
				Vector2 aim = want / dist * 2.6f;
				aim.Y += (float)Math.Sin(Bob * 0.07f) * 0.9f;
				NPC.velocity = Vector2.Lerp(NPC.velocity, aim, 0.035f);
			}

			NPC.spriteDirection = NPC.direction = NPC.velocity.X > 0f ? 1 : -1;
			NPC.rotation = NPC.velocity.X * 0.04f;
			Lighting.AddLight(NPC.Center, 0.15f, 0.06f, 0.20f);
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
			for (int i = 0; i < (NPC.life <= 0 ? 16 : 3); i++)
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.PurpleTorch,
					hit.HitDirection, -1f, 120, default, 1f);
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
			=> npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FissuriteOre>(), 1, 1, 3));
	}
}
