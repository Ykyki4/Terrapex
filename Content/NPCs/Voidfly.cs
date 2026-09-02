using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.NPCs
{
	/// <summary>
	/// Plan mob #4. Harmless, and it runs. The only creature in the mod that does not want
	/// anything from the player.
	///
	/// It is the tier's dust supply and it is deliberately annoying to catch: a night walk with
	/// a torch turns into a chase, which is the closest thing T0 has to an activity that is not
	/// swinging at something.
	/// </summary>
	public class Voidfly : ModNPC
	{
		private const float Flee = 190f;
		private ref float Drift => ref NPC.ai[0];

		public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 4;

		public override void SetDefaults()
		{
			NPC.width = 18;
			NPC.height = 18;
			NPC.damage = 0;
			NPC.defense = 0;
			NPC.lifeMax = 20;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.value = Item.buyPrice(copper: 50);
			NPC.knockBackResist = 0.9f;
			NPC.noGravity = true;
			NPC.noTileCollide = false;
			NPC.aiStyle = -1;
			NPC.dontTakeDamageFromHostiles = true;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.Voidfly.Bestiary")
			});
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.PlayerInTown || spawnInfo.Water || Main.dayTime)
				return 0f;
			return spawnInfo.Player.ZoneOverworldHeight ? 0.13f : 0f;
		}

		public override void AI()
		{
			NPC.TargetClosest(false);
			Player target = Main.player[NPC.target];
			Drift++;

			Vector2 away = NPC.Center - target.Center;
			float dist = away.Length();

			if (target.active && !target.dead && dist < Flee)
			{
				// it bolts, and the closer you get the harder it bolts
				float panic = 1f - dist / Flee;
				Vector2 run = away.SafeNormalize(Vector2.UnitY) * (2.2f + panic * 4.2f);
				NPC.velocity = Vector2.Lerp(NPC.velocity, run, 0.09f);
			}
			else
			{
				Vector2 wander = new Vector2((float)Math.Cos(Drift * 0.02f),
					(float)Math.Sin(Drift * 0.035f)) * 1.1f;
				NPC.velocity = Vector2.Lerp(NPC.velocity, wander, 0.03f);
			}

			NPC.spriteDirection = NPC.direction = NPC.velocity.X > 0f ? 1 : -1;
			Lighting.AddLight(NPC.Center, 0.28f, 0.10f, 0.36f);

			if (Main.rand.NextBool(6))
			{
				Dust d = Dust.NewDustPerfect(NPC.Center, DustID.PurpleTorch,
					-NPC.velocity * 0.1f, 150, default, 0.7f);
				d.noGravity = true;
			}
		}

		public override void FindFrame(int frameHeight)
		{
			if (++NPC.frameCounter >= 4.0)
			{
				NPC.frameCounter = 0.0;
				NPC.frame.Y = (NPC.frame.Y + frameHeight) % (frameHeight * Main.npcFrameCount[Type]);
			}
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 14 : 2); i++)
			{
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.PurpleTorch,
					hit.HitDirection, -1f, 130, default, 0.9f);
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
			=> npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RiftDust>(), 1, 1, 3));
	}
}
