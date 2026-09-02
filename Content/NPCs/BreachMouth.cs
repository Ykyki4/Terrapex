using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.GlobalNPCs;
using Terrapex.Common.Systems;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.NPCs
{
	/// <summary>
	/// A mouth of the Breach: a crack in the stone that the event pours out of. See
	/// <c>EVENT_BREACH.md</c>.
	///
	/// It is the event's dial, and the reason it is a dial rather than a chore is that it pays
	/// in both directions. Every mouth standing raises how fast the waves come, raises what the
	/// event drops, and — once Heave exists — makes the finale bigger. So killing one is how a
	/// careful player survives and leaving one up is how a greedy player earns, and neither is
	/// the obviously correct answer. That is the test <c>CLAUDE.md</c> sets for any add in this
	/// mod: is there ever a reason to leave it standing?
	///
	/// It deals no contact damage on purpose. A mouth is a target, not a threat — what comes out
	/// of it is the threat, and a spawner that also bites would make standing next to it strictly
	/// wrong, which collapses the dial back into a chore.
	/// </summary>
	public class BreachMouth : ModNPC
	{
		/// <summary>Harmless while it tears itself open, so a mouth cannot appear on top of you and spawn.</summary>
		private const int OpenTicks = 50;

		/// <summary>Base gap between spawns, before the dial's multiplier.</summary>
		private const int SpawnInterval = 60 * 6;

		/// <summary>Nothing spawns while this many of its own children are already alive.</summary>
		private const int Brood = 4;

		private ref float Timer => ref NPC.ai[0];
		private ref float SpawnTimer => ref NPC.ai[1];
		private ref float Spawned => ref NPC.ai[2];

		private bool Opening => Timer < OpenTicks;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 4;
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			NPCID.Sets.CantTakeLunchMoney[Type] = true;
			NPCID.Sets.NPCBestiaryDrawModifiers value = new() { Hide = true };
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
		}

		public override void SetDefaults()
		{
			NPC.width = 40;
			NPC.height = 40;
			NPC.damage = 0;
			NPC.defense = 0;
			NPC.lifeMax = 900;
			NPC.HitSound = SoundID.NPCHit41;
			NPC.DeathSound = SoundID.NPCDeath43;
			NPC.knockBackResist = 0f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.dontTakeDamageFromHostiles = true;
			NPC.aiStyle = -1;
			NPC.value = 0f;
			NPC.npcSlots = 0f;
			NPC.dontCountMe = true;
			NPC.netAlways = true;
		}

		/// <summary>
		/// Held down for the same reason the Weaver's anchors are: six mouths are a price the
		/// player chooses to pay, and at the default scaling closing one would cost more than
		/// the pressure it removes is worth, so nobody would ever do it.
		/// </summary>
		public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
			=> NPC.lifeMax = (int)(NPC.lifeMax * 0.6f * balance);

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
			=> bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Caverns,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.BreachMouth.Bestiary")
			});

		// ------------------------------------------------------------------ what comes out

		/// <summary>
		/// The wave tables from EVENT_BREACH.md. Every one of these already exists and is drawn;
		/// the event buys its variety from the tiers around it rather than from new art.
		/// </summary>
		private static int[] Pool(int wave) => wave switch
		{
			1 => new[] { ModContent.NPCType<Riftling>(), ModContent.NPCType<Spall>(),
						 ModContent.NPCType<RiftReaper>() },
			2 => new[] { ModContent.NPCType<Riftling>(), ModContent.NPCType<Spall>(),
						 ModContent.NPCType<RiftReaper>(), ModContent.NPCType<PlateBearer>(),
						 ModContent.NPCType<Mirrorling>() },
			_ => new[] { ModContent.NPCType<RiftReaper>(), ModContent.NPCType<PlateBearer>(),
						 ModContent.NPCType<Mirrorling>(), ModContent.NPCType<PlateShepherd>(),
						 ModContent.NPCType<RiftColossus>() },
		};

		public override void AI()
		{
			Timer++;

			// the crack has to be visibly tearing before anything can come out of it: a spawner
			// that appears and immediately delivers is the same unfairness as a hazard with no
			// telegraph, which this mod does not ship
			if (Opening)
			{
				float grow = Timer / OpenTicks;
				Lighting.AddLight(NPC.Center, 0.45f * grow, 0.15f * grow, 0.55f * grow);
				if (Main.rand.NextBool(2))
				{
					Dust d = Dust.NewDustPerfect(
						NPC.Center + Main.rand.NextVector2Circular(20f, 20f) * grow,
						DustID.PurpleTorch, Vector2.Zero, 120, default, 1.1f);
					d.noGravity = true;
					d.velocity = (NPC.Center - d.position) * 0.05f;
				}
				if (Timer == 1f)
					SoundEngine.PlaySound(SoundID.Item62 with { Pitch = -0.6f }, NPC.Center);
				return;
			}

			Lighting.AddLight(NPC.Center, 0.55f, 0.2f, 0.7f);
			if (Main.rand.NextBool(4))
			{
				Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(16f, 16f),
					DustID.PurpleTorch, -Vector2.UnitY * Main.rand.NextFloat(0.4f, 1.6f),
					120, default, 1f);
				d.noGravity = true;
			}

			if (Main.netMode == NetmodeID.MultiplayerClient || !BreachSystem.Active)
				return;

			// the dial: more mouths standing means every one of them spawns faster
			SpawnTimer += BreachSystem.SpawnMultiplier;
			if (SpawnTimer < SpawnInterval || Spawned >= Brood)
				return;

			SpawnTimer = 0f;
			Emit();
		}

		private void Emit()
		{
			int[] pool = Pool(BreachSystem.Wave);
			int type = pool[Main.rand.Next(pool.Length)];

			int index = NPC.NewNPC(NPC.GetSource_FromAI(),
				(int)NPC.Center.X, (int)NPC.Center.Y, type);
			if (index < 0 || index >= Main.maxNPCs)
				return;

			NPC born = Main.npc[index];
			born.velocity = new Vector2(Main.rand.NextFloat(-2f, 2f), -Main.rand.NextFloat(2f, 5f));
			BreachGlobalNPC mark = born.GetGlobalNPC<BreachGlobalNPC>();
			mark.FromBreach = true;
			mark.Parent = NPC.whoAmI;
			Spawned++;

			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.SyncNPC, number: index);

			for (int i = 0; i < 14; i++)
			{
				Dust d = Dust.NewDustPerfect(NPC.Center, DustID.PurpleTorch,
					Main.rand.NextVector2Circular(4f, 4f), 100, default, 1.2f);
				d.noGravity = true;
			}
			SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
		}

		/// <summary>A mouth's brood is counted, not remembered, so a killed child frees its slot.</summary>
		public void Recount()
		{
			int n = 0;
			for (int i = 0; i < Main.maxNPCs; i++)
				if (Main.npc[i].active
					&& Main.npc[i].GetGlobalNPC<BreachGlobalNPC>().Parent == NPC.whoAmI)
					n++;
			Spawned = n;
		}

		// ------------------------------------------------------------------ presentation

		public override void FindFrame(int frameHeight)
		{
			if (++NPC.frameCounter >= 9.0)
			{
				NPC.frameCounter = 0.0;
				NPC.frame.Y = (NPC.frame.Y + frameHeight) % (frameHeight * Main.npcFrameCount[Type]);
			}
		}

		public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			float open = Opening ? Timer / OpenTicks : 1f;
			float breathe = 0.85f + (float)Math.Sin(Timer * 0.05f) * 0.15f;
			RiftDraw.Bloom(NPC.Center, RiftDraw.Glow(230, 120, 220, 0.5f * open), 0.7f * open * breathe);
			RiftDraw.Ring(NPC.Center, RiftDraw.Glow(255, 170, 240, 0.35f * open),
				0.5f * open * breathe, Timer * 0.01f);
		}

		public override bool CheckActive() => false;

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 34 : 6); i++)
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height,
					DustID.PurpleTorch, hit.HitDirection, -1f, 110, default, 1.2f);
				d.noGravity = true;
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
			=> npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Ejecta>(), 1, 4, 9));

		/// <summary>
		/// The dial pays here. `ModifyNPCLoot` cannot see how many mouths were standing when this
		/// one died, so the multiplier is handed out separately rather than folded into the rule.
		/// </summary>
		public override void OnKill()
		{
			SoundEngine.PlaySound(SoundID.Item62 with { Pitch = 0.4f }, NPC.Center);

			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			// +1 because this mouth is already gone from the count by the time OnKill runs
			int extra = (int)((BreachSystem.EjectaRate[
				Math.Clamp(BreachSystem.MouthsOpen() + 1, 0, BreachSystem.MaxMouths)] - 1f) * 6f);
			if (extra > 0)
				Item.NewItem(NPC.GetSource_Death(), NPC.Hitbox,
					ModContent.ItemType<Ejecta>(), extra);
		}
	}
}
