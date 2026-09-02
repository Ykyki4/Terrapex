using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Systems;
using Terrapex.Content.Items.Accessories;
using Terrapex.Content.Items.Armor;
using Terrapex.Content.Items.Consumables;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Items.Placeable;
using Terrapex.Content.Items.Weapons;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.NPCs.Bosses
{
	/// <summary>
	/// The Dormant Eye — the mod's first boss and the Keeper's younger sibling.
	///
	/// It exists to teach one thing before hardmode: <b>the shell comes off, and the
	/// open eye is the window</b>. So it is the Keeper's fight with everything else
	/// stripped away — four plates instead of eight, one orbit instead of three, and
	/// the eye opens exactly once, at 25%, which is also the whole of phase two.
	///
	/// DormantEye.png is 12 frames of 64 px: 0-5 the shell shut, 6-11 the eye open.
	/// </summary>
	[AutoloadBossHead]
	public class DormantEye : ModNPC
	{
		private const int FrameShut = 0;
		private const int FrameOpen = 6;
		private const int LoopLength = 6;

		private const float PhaseTwoThreshold = 0.25f;
		private const int ShutDefense = 14;
		private const int OpenDefense = 6;
		private const int BreakDuration = 90;

		private const int PlateCount = 4;
		private const float PlateRadius = 92f;
		private const int PlateLife = 250;

		private ref float Timer => ref NPC.ai[0];
		private ref float Phase => ref NPC.ai[1];        // 0 shell, 1 broken open
		private ref float BreakTimer => ref NPC.ai[2];
		private ref float Spawned => ref NPC.ai[3];

		private int frame;
		private double frameCounter;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 12;
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			NPCID.Sets.BossBestiaryPriority.Add(Type);
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
		}

		public override void SetDefaults()
		{
			NPC.width = 64;
			NPC.height = 64;
			NPC.damage = 24;
			NPC.defense = ShutDefense;
			NPC.lifeMax = 3200;
			NPC.HitSound = SoundID.NPCHit42;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.knockBackResist = 0f;
			NPC.value = Item.buyPrice(gold: 3);
			NPC.aiStyle = -1;
			NPC.boss = true;
			NPC.npcSlots = 6f;
			NPC.noGravity = false;      // it is a rock: it rolls, it does not fly
			NPC.noTileCollide = false;
			Music = MusicID.Boss1;
		}

		public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
		{
			NPC.lifeMax = (int)(NPC.lifeMax * 0.7f * balance);
			NPC.damage = (int)(NPC.damage * 0.85f);
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Underground,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.DormantEye.Bestiary")
			});
		}

		private bool ShellIntact
		{
			get
			{
				int plate = ModContent.NPCType<EyePlate>();
				for (int i = 0; i < Main.maxNPCs; i++)
				{
					NPC n = Main.npc[i];
					if (n.active && n.type == plate && ((EyePlate)n.ModNPC).BelongsTo(NPC))
						return true;
				}
				return false;
			}
		}

		private void SpawnPlates()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			for (int i = 0; i < PlateCount; i++)
			{
				int idx = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y,
					ModContent.NPCType<EyePlate>(), 0, NPC.whoAmI,
					MathHelper.TwoPi * i / PlateCount, PlateRadius);
				if (idx >= Main.maxNPCs)
					continue;
				Main.npc[idx].lifeMax = Main.npc[idx].life = PlateLife;
				if (Main.netMode == NetmodeID.Server)
					NetMessage.SendData(MessageID.SyncNPC, number: idx);
			}
		}

		private void KillPlates()
		{
			int plate = ModContent.NPCType<EyePlate>();
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.type != plate || !((EyePlate)n.ModNPC).BelongsTo(NPC))
					continue;
				n.life = 0;
				n.HitEffect();
				n.checkDead();
				n.active = false;
			}
		}

		// While a plate is up, the core takes half. That is the entire lesson: hit the
		// shell or wait for the eye, but do not just chew on the middle.
		public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
		{
			if (Phase == 0f && ShellIntact)
				modifiers.FinalDamage *= 0.5f;
		}

		public override void AI()
		{
			// nothing else runs while it is coming apart: no attacks, no targeting,
			// no phase checks. The NPC may be gone the moment Tick returns true.
			if (death.Dying)
			{
				death.Tick(NPC, DustID.Stone, new Color(150, 120, 200));
				return;
			}

			if (Spawned == 0f)
			{
				Spawned = 1f;
				SpawnPlates();
				NPC.TargetClosest();
			}

			NPC.TargetClosest();
			Player target = Main.player[NPC.target];
			if (!target.active || target.dead)
			{
				NPC.TargetClosest();
				target = Main.player[NPC.target];
				if (!target.active || target.dead)
				{
					NPC.velocity.Y -= 0.4f;
					NPC.EncourageDespawn(30);
					return;
				}
			}

			// ---- the one phase change, at a quarter health
			if (Phase == 0f && NPC.life <= NPC.lifeMax * PhaseTwoThreshold)
			{
				Phase = 1f;
				BreakTimer = BreakDuration;
				NPC.defense = OpenDefense;
				KillPlates();
				NPC.velocity.X *= 0.2f;
				SoundEngine.PlaySound(SoundID.NPCDeath43, NPC.Center);
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(
					NPC.Center, Main.rand.NextVector2Unit(), 12f, 6f, 24, 1000f, "DormantEye"));
				for (int i = 0; i < 40; i++)
				{
					Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height,
						DustID.Stone, Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-6f, 6f), 60, default, 1.5f);
					d.noGravity = true;
				}
				if (Main.netMode != NetmodeID.MultiplayerClient)
					ChatHelper.BroadcastChatMessage(
						NetworkText.FromKey("Mods.Terrapex.Chat.DormantEyeOpens"), new Color(200, 120, 240));
			}

			if (BreakTimer > 0f)
			{
				// the lid splitting: a free window, the reward for getting it this far
				BreakTimer--;
				NPC.velocity.X *= 0.9f;
				NPC.defense = 0;
				if (Main.rand.NextBool(2))
				{
					Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height,
						DustID.PurpleTorch, 0f, 0f, 80, default, 1.3f);
					d.noGravity = true;
				}
				return;
			}
			NPC.defense = Phase == 0f ? ShutDefense : OpenDefense;

			Timer++;
			Roll(target);

			if (Phase == 0f)
			{
				if (Timer >= 230f)
				{
					Timer = 0f;
					Rockfall(target);
				}
			}
			else if (Timer >= 170f)
			{
				Timer = 0f;
				Beam(target);
			}
		}

		/// <summary>Chase along the ground, hopping when a ledge or wall gets in the way.</summary>
		private void Roll(Player target)
		{
			float top = Phase == 0f ? 3.6f : 5.2f;
			// running away is not an out: it gets faster the further you go
			if (NPC.Distance(target.Center) > 900f)
				top *= 1.6f;

			float dir = Math.Sign(target.Center.X - NPC.Center.X);
			NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X + dir * 0.10f, -top, top);

			bool onGround = NPC.velocity.Y == 0f;
			if (onGround)
			{
				NPC.velocity.X *= 0.99f;
				bool blocked = NPC.collideX;
				bool playerAbove = target.Center.Y < NPC.Center.Y - 60f;
				if (blocked || (playerAbove && Main.rand.NextBool(40)))
					NPC.velocity.Y = -8.2f;
			}

			NPC.rotation += NPC.velocity.X * 0.045f;

			if (Math.Abs(NPC.velocity.X) > 1.5f && NPC.velocity.Y == 0f && Main.rand.NextBool(4))
			{
				Dust d = Dust.NewDustDirect(new Vector2(NPC.position.X, NPC.position.Y + NPC.height - 6f),
					NPC.width, 6, DustID.Stone, -NPC.velocity.X * 0.3f, -1f, 120, default, 1f);
				d.velocity *= 0.6f;
			}
		}

		/// <summary>Five stones queued over the player, each announcing its own landing spot.</summary>
		private void Rockfall(Player target)
		{
			SoundEngine.PlaySound(SoundID.Item14, NPC.Center);
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			int damage = NPC.damage / 3;
			for (int i = 0; i < 5; i++)
			{
				float x = target.Center.X + (i - 2) * 58f + Main.rand.NextFloat(-16f, 16f);
				Vector2 pos = new Vector2(x, target.Center.Y - 300f - Main.rand.NextFloat(0f, 40f));
				Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, Vector2.Zero,
					ModContent.ProjectileType<Rockfall>(), damage, 3f, Main.myPlayer);
			}
		}

		/// <summary>The beam out of the split lid: the Keeper's laser, thinner and slower.</summary>
		private void Beam(Player target)
		{
			SoundEngine.PlaySound(SoundID.Item33, NPC.Center);
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			float angle = (target.Center - NPC.Center).ToRotation();
			int damage = NPC.damage / 2;
			int p = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center,
				angle.ToRotationVector2(), ModContent.ProjectileType<RiftLaser>(),
				damage, 4f, Main.myPlayer, NPC.whoAmI, 0.004f);
			if (p < Main.maxProjectiles)
			{
				// scale drives both the beam's width and how far it starts from the core
				Main.projectile[p].scale = 0.55f;
				Main.projectile[p].netUpdate = true;
			}
		}

		public override void FindFrame(int frameHeight)
		{
			int start = Phase == 0f && BreakTimer <= 0f ? FrameShut : FrameOpen;
			frameCounter += BreakTimer > 0f ? 0.30 : 0.16;
			if (frameCounter >= LoopLength)
				frameCounter = 0;
			frame = start + (int)frameCounter;
			NPC.frame.Y = frame * frameHeight;
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			int count = NPC.life <= 0 ? 30 : 5;
			for (int i = 0; i < count; i++)
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height,
					DustID.Stone, hit.HitDirection, -1f, 90, default, 1.2f);
				d.velocity *= 1.3f;
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<DormantEyeBag>()));

			// everything below is normal-mode only: in expert it all comes out of the bag
			LeadingConditionRule notExpert = new LeadingConditionRule(new Conditions.NotExpert());
			notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<DormantCornea>(), 1, 8, 14));
			notExpert.OnSuccess(ItemDropRule.OneFromOptions(1,
				ModContent.ItemType<Lid>(),
				ModContent.ItemType<StoneEye>(),
				ModContent.ItemType<RockfallStaff>(),
				ModContent.ItemType<SleepersRod>()));
			notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<PlateShield>(), 3));
			npcLoot.Add(notExpert);

			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DormantEyeMask>(), 7));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DormantEyeTrophy>(), 10));
		}

		public override void OnKill()
		{
			if (!DownedBossSystem.downedDormantEye)
			{
				DownedBossSystem.downedDormantEye = true;
				if (Main.netMode != NetmodeID.MultiplayerClient)
					ChatHelper.BroadcastChatMessage(
						NetworkText.FromKey("Mods.Terrapex.Chat.DormantEyeDown"), new Color(200, 120, 240));
				if (Main.netMode == NetmodeID.Server)
					NetMessage.SendData(MessageID.WorldData);
			}
		}

		/// <summary>The throes. Shared timing, own dust: the lid finally stops fighting the stone and the boulder rolls to a halt.</summary>
		private readonly BossDeath death = new();

		public override bool CheckDead()
			=> death.CheckDead(NPC, 110);

		public override bool CheckActive() => false;
	}
}
