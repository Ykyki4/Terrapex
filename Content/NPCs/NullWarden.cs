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
	/// Plan mob #14. It cannot be hurt until it opens its eye, and the eye is open for ninety
	/// ticks at a time.
	///
	/// The window is telegraphed for a full second before it arrives, because a mob you can
	/// only damage sometimes is a puzzle, and a puzzle with no tell is just a mob that wastes
	/// your ammunition.
	/// </summary>
	public class NullWarden : ModNPC
	{
		private const int Cycle = 60 * 5;
		private const int Warn = 60;
		private const int OpenFor = 90;

		private ref float Timer => ref NPC.ai[0];

		private bool Open => Timer >= Cycle - OpenFor;
		private bool Warning => !Open && Timer >= Cycle - OpenFor - Warn;

		public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 4;

		public override void SetDefaults()
		{
			NPC.width = 44;
			NPC.height = 54;
			NPC.damage = 88;
			NPC.defense = 48;
			NPC.lifeMax = 2600;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.value = Item.buyPrice(gold: 1);
			NPC.knockBackResist = 0.1f;
			NPC.noGravity = true;
			NPC.noTileCollide = false;
			NPC.aiStyle = -1;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Caverns,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.NullWarden.Bestiary")
			});
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.PlayerInTown || spawnInfo.Water || !Terraria.NPC.downedPlantBoss)
				return 0f;
			return spawnInfo.Player.ZoneRockLayerHeight ? 0.035f : 0f;
		}

		public override void AI()
		{
			if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
				NPC.TargetClosest();
			Player target = Main.player[NPC.target];

			Timer++;
			if (Timer >= Cycle)
				Timer = 0f;

			NPC.dontTakeDamage = !Open;

			Vector2 want = target.Center - NPC.Center;
			float dist = want.Length();
			// it closes in while shut and holds still while open, so the window is also the
			// moment it stops chasing
			float speed = Open ? 0.6f : 3.4f;
			if (dist > 8f)
				NPC.velocity = Vector2.Lerp(NPC.velocity, want / dist * speed, 0.05f);

			NPC.spriteDirection = NPC.direction = target.Center.X < NPC.Center.X ? -1 : 1;

			if (Warning && Main.rand.NextBool(2))
			{
				Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(22f, 22f),
					DustID.Vortex, Vector2.Zero, 90, default, 1.2f);
				d.noGravity = true;
				d.velocity = Vector2.Normalize(NPC.Center - d.position) * 2.2f;
			}
			if (Open)
				Lighting.AddLight(NPC.Center, 0.5f, 0.9f, 0.85f);
		}

		public override void FindFrame(int frameHeight)
		{
			// frames 0-1 shut, 2 warning, 3 open: the state is on the sprite, not only in dust
			int frame = Open ? 3 : (Warning ? 2 : (Timer % 40f < 20f ? 0 : 1));
			NPC.frame.Y = frame * frameHeight;
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
			=> npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<VoidThread>(), 1, 1, 3));

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 24 : 5); i++)
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Vortex,
					hit.HitDirection, -1f, 110, default, 1.1f);
				d.noGravity = true;
			}
		}
	}
}
