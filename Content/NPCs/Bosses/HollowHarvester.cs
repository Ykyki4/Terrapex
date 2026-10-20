using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Particles;
using Terrapex.Common.Systems;
using Terrapex.Content.Items.Accessories;
using Terrapex.Content.Items.Armor;
using Terrapex.Content.Items.Consumables;
using Terrapex.Content.Items.Weapons.Reaper;
using Terrapex.Content.Projectiles.Harvester;

namespace Terrapex.Content.NPCs.Bosses
{
	/// <summary>
	/// The Hollow Harvester - the reaper class's boss. See <c>BOSS_HARVESTER.md</c>; the numbers
	/// there and here are kept in step.
	///
	/// It is the class's mechanic turned on the player. A reach is drawn round it, its cuts land
	/// only inside that circle, and like a reaper's they hit harder the further out they land.
	/// Standing close makes the marks weaker and puts you in the way of the dash; standing far
	/// makes them hurt. There is no safe spot, only a priced one - the same price a player with a
	/// scythe pays later from the other side.
	///
	/// HollowHarvester.png is four frames of the cloak fluttering; the scythe is its own texture
	/// (HarvesterScythe), drawn in the hand and thrown in phase two.
	/// </summary>
	[AutoloadBossHead]
	public class HollowHarvester : ModNPC
	{
		public const float ReachRadius = 440f;
		public const float PhaseTwoAt = 0.6f;
		public const float PhaseThreeAt = 0.25f;
		public const float NearShare = 0.6f, RimShare = 1.4f;

		public const int MarkDamage = 24;
		public const int CrowDamage = 16;
		public const int ScytheDamage = 26;
		public const int PumpkinDamage = 28;
		public const int FieldDamage = 34;

		private const int Marks = 0, Sweep = 1, Crows = 2, Throw = 3, Furrow = 4, Pumpkins = 5, Field = 6;
		// fixed rotations, one per phase: a boss you can learn
		private static readonly int[] RotationOne = { Marks, Sweep, Crows, Furrow };
		private static readonly int[] RotationTwo = { Marks, Throw, Pumpkins, Sweep, Crows, Furrow };
		private static readonly int[] RotationThree = { Field, Marks, Throw, Pumpkins, Sweep, Furrow, Crows };

		private ref float Timer => ref NPC.ai[0];
		private ref float Step => ref NPC.ai[1];
		private ref float Phase => ref NPC.ai[2];
		private ref float Side => ref NPC.ai[3];

		/// <summary>The committed direction of a dash or a throw, decided by the server and synced.</summary>
		private float aimAngle;
		private bool scytheOut;

		private readonly BossDeath death = new();

		private int[] Rotation => Phase == 0f ? RotationOne : Phase == 1f ? RotationTwo : RotationThree;
		private int Move => Rotation[(int)Step % Rotation.Length];
		private int Pause => Phase == 0f ? 40 : Phase == 1f ? 22 : 14;

		/// <summary>Where the furrow or the closing field was cast from - fixed for the move, synced.</summary>
		private Vector2 castFrom;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 4;
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			NPCID.Sets.BossBestiaryPriority.Add(Type);
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
			NPCID.Sets.TrailCacheLength[Type] = 7;
			NPCID.Sets.TrailingMode[Type] = 0;
		}

		public override void SetDefaults()
		{
			NPC.width = 56;
			NPC.height = 84;
			NPC.damage = 30;
			NPC.defense = 12;
			NPC.lifeMax = 5000;
			NPC.HitSound = SoundID.NPCHit54;
			NPC.DeathSound = SoundID.NPCDeath52;
			NPC.knockBackResist = 0f;
			NPC.value = Item.buyPrice(gold: 5);
			NPC.aiStyle = -1;
			NPC.boss = true;
			NPC.npcSlots = 8f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			Music = MusicID.Boss2;
		}

		public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
		{
			NPC.lifeMax = (int)(NPC.lifeMax * 0.75f * balance);
			NPC.damage = (int)(NPC.damage * 0.85f);
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
			=> bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.HollowHarvester.Bestiary")
			});

		public override void SendExtraAI(BinaryWriter writer)
		{
			writer.Write(aimAngle);
			writer.WriteVector2(castFrom);
		}

		public override void ReceiveExtraAI(BinaryReader reader)
		{
			aimAngle = reader.ReadSingle();
			castFrom = reader.ReadVector2();
		}

		// ------------------------------------------------------------------ AI

		public override void AI()
		{
			if (death.Dying)
			{
				Vector2 lastCenter = NPC.Center;
				DeathCosmetics();
				if (death.Tick(NPC, DustID.Hay, new Color(190, 236, 200)))
					DeathBurst(lastCenter);
				return;
			}

			if (!Main.dedServ)
				Cosmetics();

			NPC.TargetClosest();
			Player target = Main.player[NPC.target];
			if (!target.active || target.dead || Main.dayTime)
			{
				// a night thing: at dawn, or with nobody left, it rises and goes
				NPC.velocity.Y -= 0.3f;
				NPC.EncourageDespawn(20);
				return;
			}

			if (Phase == 0f && NPC.life <= NPC.lifeMax * PhaseTwoAt)
				EnterPhaseTwo();
			else if (Phase == 1f && NPC.life <= NPC.lifeMax * PhaseThreeAt)
				EnterPhaseThree();

			NPC.spriteDirection = NPC.direction = target.Center.X >= NPC.Center.X ? 1 : -1;
			Timer++;

			// the pause between moves: it only drifts
			if (Timer <= 0f)
			{
				Hover(target, 0.06f);
				return;
			}

			switch (Move)
			{
				case Marks: DoMarks(target); break;
				case Sweep: DoSweep(target); break;
				case Crows: DoCrows(target); break;
				case Throw: DoThrow(target); break;
				case Furrow: DoFurrow(target); break;
				case Pumpkins: DoPumpkins(target); break;
				case Field: DoField(target); break;
			}
		}

		private void Hover(Player target, float pull)
		{
			if (Side == 0f)
				Side = 1f;
			Vector2 want = target.Center + new Vector2(Side * 280f, -140f);
			Vector2 toward = (want - NPC.Center) * 0.05f;
			if (toward.Length() > 9f)
				toward = Vector2.Normalize(toward) * 9f;
			NPC.velocity = Vector2.Lerp(NPC.velocity, toward, pull);
		}

		private void NextMove()
		{
			Timer = -Pause;
			Step++;
			Side = -Side;
			NPC.netUpdate = true;
		}

		private void EnterPhaseTwo()
		{
			Phase = 1f;
			Timer = -30f;
			Step = 0f;
			NPC.netUpdate = true;
			SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
			for (int i = 0; i < 40; i++)
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Hay,
					Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-6f, 6f), 60, default, 1.4f);
				d.noGravity = true;
			}
			if (!Main.dedServ)
			{
				// the head catches: straw bursts out of the seams, embers lift, and the pumpkin
				// lantern lights for the rest of the fight
				Vector2 head = Head;
				ParticleSystem.Spawn(new Bloom(head, HarvestPalette.Pumpkin, 0.6f, 2.2f, 40));
				ParticleSystem.Spawn(new Bloom(head, HarvestPalette.Ember, 0.3f, 1.0f, 28));
				ParticleSystem.Spawn(new Shockwave(NPC.Center, HarvestPalette.Ghost * 0.7f, 0.2f, 2.4f, 34));
				ParticleSystem.Spawn(new Shockwave(head, HarvestPalette.Pumpkin * 0.6f, 0.1f, 1.2f, 26));
				for (int i = 0; i < 46; i++)
					ParticleSystem.Spawn(new Straw(NPC.Center + Main.rand.NextVector2Circular(24f, 36f),
						Main.rand.NextVector2Circular(7f, 6f) - new Vector2(0f, 2f), HarvestPalette.RandomStraw(),
						Main.rand.NextFloat(5f, 10f), Main.rand.Next(60, 100)));
				for (int i = 0; i < 30; i++)
					ParticleSystem.Spawn(new Spark(head + Main.rand.NextVector2Circular(10f, 10f),
						new Vector2(Main.rand.NextFloat(-2.5f, 2.5f), -Main.rand.NextFloat(1f, 4.5f)),
						Main.rand.NextBool(2) ? HarvestPalette.Pumpkin : HarvestPalette.Ember, 0.9f, Main.rand.Next(30, 55), -0.03f)
						{ Drag = 0.96f });
				for (int i = 0; i < 10; i++)
					ParticleSystem.Spawn(new Smoke(NPC.Center + Main.rand.NextVector2Circular(20f, 30f),
						Main.rand.NextVector2Circular(2f, 2f), new Color(40, 30, 40), Main.rand.NextFloat(0.4f, 0.7f), 50));
			}
			if (Main.netMode != NetmodeID.MultiplayerClient)
				ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Mods.Terrapex.Chat.HarvesterPhase2"),
					new Color(190, 236, 200));
		}

		/// <summary>
		/// The Last Harvest, at a quarter: the cloak tears open, the lantern flares white-hot, and
		/// from now on the rotation opens with its own reach closing on the player as a blade.
		/// </summary>
		private void EnterPhaseThree()
		{
			Phase = 2f;
			Timer = -40f;
			Step = 0f;
			NPC.netUpdate = true;
			SoundEngine.PlaySound(SoundID.Roar with { Pitch = -0.4f }, NPC.Center);
			SoundEngine.PlaySound(SoundID.Item74 with { Pitch = -0.6f }, NPC.Center);
			if (!Main.dedServ)
			{
				Vector2 head = Head;
				ParticleSystem.Spawn(new Bloom(head, Color.White, 0.5f, 2.6f, 36));
				ParticleSystem.Spawn(new Bloom(head, HarvestPalette.Pumpkin, 0.7f, 3.2f, 50));
				ParticleSystem.Spawn(new Shockwave(NPC.Center, HarvestPalette.Ghost, 0.2f, ReachRadius * 2f / 64f, 40));
				ParticleSystem.Spawn(new Shockwave(NPC.Center, HarvestPalette.Pumpkin * 0.8f, 0.1f, 2.6f, 30));
				for (int i = 0; i < 70; i++)
					ParticleSystem.Spawn(new Straw(NPC.Center + Main.rand.NextVector2Circular(24f, 36f),
						Main.rand.NextVector2Circular(9f, 8f) - new Vector2(0f, 2f), HarvestPalette.RandomStraw(),
						Main.rand.NextFloat(5f, 11f), Main.rand.Next(70, 120)));
				for (int i = 0; i < 16; i++)
					ParticleSystem.Spawn(new Feather(NPC.Center, Main.rand.NextVector2Circular(6f, 5f), Main.rand.NextFloat(0.8f, 1.3f),
						Main.rand.Next(70, 110)));
				FX.Sparks(head, HarvestPalette.Ember, 30, 9f, 1.1f, 40, -0.02f);
				if (Main.LocalPlayer.Distance(NPC.Center) < 1400f)
					Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
						NPC.Center, Main.rand.NextVector2Unit(), 10f, 6f, 26, 1400f, "HarvesterThree"));
			}
			if (Main.netMode != NetmodeID.MultiplayerClient)
				ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Mods.Terrapex.Chat.HarvesterPhase3"),
					new Color(255, 170, 80));
		}

		// ------------------------------------------------------------------ cosmetics (client only, never synced)

		/// <summary>Where the eyes sit in the sprite - the tell flash, and the lantern in phase two.</summary>
		private Vector2 Head => NPC.Center + new Vector2(6f * NPC.spriteDirection, -30f + NPC.gfxOffY);

		private void Cosmetics()
		{
			Vector2 c = NPC.Center;

			// chaff blowing round the reach: the field is alive even between moves
			if (Main.rand.NextBool(2))
			{
				float a = Main.rand.NextFloat(MathHelper.TwoPi);
				Vector2 n = a.ToRotationVector2();
				Vector2 at = c + n * ReachRadius * Main.rand.NextFloat(0.93f, 1.03f);
				if (Vector2.DistanceSquared(at, Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f) < 1400f * 1400f)
					ParticleSystem.Spawn(new Chaff(at, n.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(0.4f, 1.2f)
						+ new Vector2(0.3f, 0f), HarvestPalette.RandomStraw(), Main.rand.Next(80, 140)));
			}

			// phase two: the pumpkin lantern in the head, guttering, shedding embers
			if (Phase >= 1f)
			{
				float flicker = 0.8f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 13f) * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5.3f);
				Lighting.AddLight(Head, 0.9f * flicker, 0.5f * flicker, 0.15f * flicker);
				if (Main.rand.NextBool(5))
					ParticleSystem.Spawn(new Spark(Head + Main.rand.NextVector2Circular(6f, 4f),
						new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), -Main.rand.NextFloat(0.8f, 2f)),
						HarvestPalette.Ember, 0.7f, Main.rand.Next(20, 36), -0.02f) { Drag = 0.97f });
			}

			if (Timer <= 0f)
				return;

			// the eye tell: ghost-light gathers into the head before marks and crows
			bool eyeTell = (Move == Marks && Timer < 24f) || (Move == Crows && Timer < 20f);
			if (eyeTell && Main.rand.NextBool(2))
				ParticleSystem.Spawn(new Mote(Head + Main.rand.NextVector2Unit() * Main.rand.NextFloat(40f, 70f), Head,
					HarvestPalette.Ghost, 0.9f, 30, 0.8f));

			Vector2 dir = aimAngle.ToRotationVector2();
			if (Move == Sweep)
			{
				if (Timer <= 40f && Main.rand.NextBool(2))
				{
					// chaff drawn out along the aim line, ahead of the dash
					float along = Main.rand.NextFloat(60f, 700f);
					ParticleSystem.Spawn(new Chaff(c + dir * along + dir.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-10f, 10f),
						dir * Main.rand.NextFloat(1.5f, 3.5f), HarvestPalette.RandomStraw(), 40));
				}
				else if (Timer == 41f)
				{
					ParticleSystem.Spawn(new Shockwave(c, HarvestPalette.Ghost * 0.8f, 0.2f, 1.4f, 22, 0.6f) { Rotation = aimAngle + MathHelper.PiOver2 });
					FX.Sparks(c, HarvestPalette.Ghost, 10, 7f, 1f, 18, 0f, 1.2f, aimAngle + MathHelper.Pi);
				}
				else if (Timer > 40f && Timer <= 64f)
				{
					// straw torn out of the cloak in its wake
					Vector2 back = -dir.RotatedByRandom(0.5f) * Main.rand.NextFloat(1f, 3f);
					ParticleSystem.Spawn(new Straw(c + Main.rand.NextVector2Circular(20f, 30f), back, HarvestPalette.RandomStraw(),
						Main.rand.NextFloat(5f, 8f), Main.rand.Next(40, 70)));
					if (Main.rand.NextBool(2))
						ParticleSystem.Spawn(new Spark(c + Main.rand.NextVector2Circular(20f, 30f), -dir * Main.rand.NextFloat(3f, 7f),
							HarvestPalette.Ghost, 0.9f, 14));
				}
			}
			else if (Move == Throw && Timer == 37f)
			{
				Vector2 hand = c + new Vector2(18f * NPC.spriteDirection, 8f);
				ParticleSystem.Spawn(new Bloom(hand, HarvestPalette.Ghost, 0.3f, 1f, 16));
				FX.Sparks(hand, HarvestPalette.Ghost, 10, 6f, 1f, 18, 0f, 1.4f, aimAngle);
			}
		}

		/// <summary>The throes: the lantern sputters, straw falls out of it in handfuls, crows leave.</summary>
		private void DeathCosmetics()
		{
			if (Main.dedServ)
				return;
			float f = death.Progress;
			Vector2 c = NPC.Center;
			int straw = 1 + (int)(f * 3f);
			for (int i = 0; i < straw; i++)
				ParticleSystem.Spawn(new Straw(c + Main.rand.NextVector2Circular(24f, 36f),
					Main.rand.NextVector2Circular(2.5f, 1.5f), HarvestPalette.RandomStraw(), Main.rand.NextFloat(5f, 9f),
					Main.rand.Next(60, 100)));
			if (Main.rand.NextFloat() < 0.3f + f * 0.5f)
				ParticleSystem.Spawn(new Spark(Head + Main.rand.NextVector2Circular(8f, 6f),
					new Vector2(Main.rand.NextFloat(-1f, 1f), -Main.rand.NextFloat(1f, 3f)),
					Main.rand.NextBool(2) ? HarvestPalette.Pumpkin : HarvestPalette.Ember, 0.8f, Main.rand.Next(24, 44), -0.03f)
					{ Drag = 0.96f });
			if (Main.rand.NextBool(12))
				ParticleSystem.Spawn(new Feather(c + Main.rand.NextVector2Circular(20f, 30f),
					new Vector2(Main.rand.NextFloat(-3f, 3f), -Main.rand.NextFloat(1f, 3f)), 1f, 70));
			float sputter = Main.rand.NextBool(6) ? 0.2f : 1f;
			Lighting.AddLight(Head, 1f * sputter, 0.55f * sputter, 0.15f * sputter);
		}

		private static void DeathBurst(Vector2 c)
		{
			if (Main.dedServ)
				return;
			ParticleSystem.Spawn(new Bloom(c, HarvestPalette.Pumpkin, 0.8f, 3f, 40));
			ParticleSystem.Spawn(new Bloom(c, HarvestPalette.Ghost, 0.5f, 2f, 30));
			ParticleSystem.Spawn(new Shockwave(c, HarvestPalette.Ghost, 0.2f, 3f, 40));
			ParticleSystem.Spawn(new Shockwave(c, HarvestPalette.Pumpkin * 0.7f, 0.1f, 1.8f, 30));
			for (int i = 0; i < 70; i++)
				ParticleSystem.Spawn(new Straw(c + Main.rand.NextVector2Circular(20f, 30f),
					Main.rand.NextVector2Circular(9f, 8f) - new Vector2(0f, 2f), HarvestPalette.RandomStraw(),
					Main.rand.NextFloat(5f, 11f), Main.rand.Next(80, 130)));
			for (int i = 0; i < 24; i++)
				ParticleSystem.Spawn(new Feather(c, Main.rand.NextVector2Circular(6f, 5f), Main.rand.NextFloat(0.8f, 1.3f),
					Main.rand.Next(70, 120)));
			FX.Sparks(c, HarvestPalette.Pumpkin, 30, 9f, 1.2f, 40, -0.02f);
			FX.Sparks(c, HarvestPalette.Ghost, 16, 7f, 1f, 26);
			for (int i = 0; i < 14; i++)
				ParticleSystem.Spawn(new Smoke(c + Main.rand.NextVector2Circular(20f, 30f), Main.rand.NextVector2Circular(3f, 3f),
					new Color(40, 30, 40), Main.rand.NextFloat(0.5f, 0.9f), 60));
		}

		/// <summary>
		/// Where a mark lands for a wanted point, and how much it carries: clamped into the reach,
		/// harder toward the rim - the class's own rule, from the other side.
		/// </summary>
		private int MarkAt(Vector2 wanted, out Vector2 point)
		{
			Vector2 to = wanted - NPC.Center;
			float len = to.Length();
			if (len > ReachRadius)
			{
				to *= ReachRadius / len;
				len = ReachRadius;
			}
			point = NPC.Center + to;
			float share = len / ReachRadius;
			return NPC.GetAttackDamage_ForProjectiles(MarkDamage * MathHelper.Lerp(NearShare, RimShare, share), MarkDamage);
		}

		private void SpawnMark(Vector2 wanted)
		{
			int damage = MarkAt(wanted, out Vector2 point);
			Projectile.NewProjectile(NPC.GetSource_FromAI(), point, Vector2.Zero,
				ModContent.ProjectileType<HarvesterCut>(), damage, 0f, Main.myPlayer);
		}

		private void DoMarks(Player target)
		{
			Hover(target, 0.05f);
			const int tell = 24;
			if (Timer == tell && Main.netMode != NetmodeID.MultiplayerClient)
			{
				int count = Phase == 0f ? 3 : 5;
				SpawnMark(target.Center);
				for (int i = 1; i < count; i++)
					SpawnMark(target.Center + Main.rand.NextVector2Circular(170f, 120f));
			}
			if (Phase >= 1f && Timer == tell + 32 && Main.netMode != NetmodeID.MultiplayerClient)
			{
				for (int i = 0; i < 3; i++)
					SpawnMark(target.Center + target.velocity * 20f + Main.rand.NextVector2Circular(140f, 100f));
			}
			if (Timer >= (Phase == 0f ? 100 : 124))
				NextMove();
		}

		private void DoSweep(Player target)
		{
			const int tell = 40, dash = 24, recover = 26;
			if (Timer == 1f && Main.netMode != NetmodeID.MultiplayerClient)
			{
				aimAngle = (target.Center - NPC.Center).ToRotation();
				NPC.netUpdate = true;
			}
			if (Timer <= tell)
			{
				NPC.velocity *= 0.9f;
				return;
			}
			if (Timer <= tell + dash)
			{
				if (Timer == tell + 1)
					SoundEngine.PlaySound(SoundID.Item71 with { Pitch = -0.4f, Volume = 1.2f }, NPC.Center);
				NPC.velocity = aimAngle.ToRotationVector2() * 17f;
				if (Main.rand.NextBool(2))
					Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Hay, 0f, 0f, 80, default, 1.1f).noGravity = true;
				return;
			}
			NPC.velocity *= 0.9f;
			if (Timer >= tell + dash + recover)
				NextMove();
		}

		private void DoCrows(Player target)
		{
			Hover(target, 0.05f);
			const int tell = 20;
			if (Timer == tell && Main.netMode != NetmodeID.MultiplayerClient)
			{
				int count = Phase == 0f ? 5 : 7;
				Vector2 at = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
				int damage = NPC.GetAttackDamage_ForProjectiles(CrowDamage, CrowDamage);
				for (int i = 0; i < count; i++)
				{
					float t = i / (count - 1f) - 0.5f;
					Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, at.RotatedBy(t * 0.9f) * 7f,
						ModContent.ProjectileType<HarvesterCrow>(), damage, 0f, Main.myPlayer);
				}
				SoundEngine.PlaySound(SoundID.NPCHit11 with { Pitch = 0.3f }, NPC.Center);
			}
			if (Timer >= 60)
				NextMove();
		}

		private void DoThrow(Player target)
		{
			const int tell = 36;
			if (Timer == 1f && Main.netMode != NetmodeID.MultiplayerClient)
			{
				aimAngle = (target.Center - NPC.Center).ToRotation();
				NPC.netUpdate = true;
			}
			if (Timer <= tell)
			{
				NPC.velocity *= 0.9f;
				return;
			}
			if (Timer == tell + 1)
			{
				scytheOut = true;
				if (Main.netMode != NetmodeID.MultiplayerClient)
					Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, aimAngle.ToRotationVector2() * 16f,
						ModContent.ProjectileType<HarvesterScythe>(),
						NPC.GetAttackDamage_ForProjectiles(ScytheDamage, ScytheDamage), 0f, Main.myPlayer, NPC.whoAmI);
				SoundEngine.PlaySound(SoundID.Item71 with { Pitch = -0.6f }, NPC.Center);
			}
			Hover(target, 0.03f);
			if (Timer >= 110)
			{
				scytheOut = false;
				NextMove();
			}
		}

		private const int FurrowTell = 32;
		private const int FurrowStep = 5;
		private const float FurrowSpacing = 58f;

		private int FurrowRows => Phase == 0f ? 1 : Phase == 1f ? 2 : 3;

		private float FurrowAngle(int row)
			=> aimAngle + (row - (FurrowRows - 1) / 2f) * 0.55f;

		/// <summary>
		/// The furrow: one swing reaping a row. Aim lines are drawn out from it through the player for
		/// <see cref="FurrowTell"/> ticks, then marks are laid down each line one after another from
		/// its feet to the rim of the reach, every <see cref="FurrowStep"/> ticks - each mark keeps its
		/// own 42-tick tell, so the row is a wave of cuts walking outward. Step off the line. Two rows
		/// in phase two, a fan of three in phase three.
		/// </summary>
		private void DoFurrow(Player target)
		{
			NPC.velocity *= 0.85f;
			if (Timer == 1f && Main.netMode != NetmodeID.MultiplayerClient)
			{
				aimAngle = (target.Center - NPC.Center).ToRotation();
				castFrom = NPC.Center;
				NPC.netUpdate = true;
			}
			int perRow = (int)((ReachRadius - 70f) / FurrowSpacing) + 1;
			int since = (int)Timer - FurrowTell;
			if (since > 0 && since % FurrowStep == 0 && since / FurrowStep <= perRow)
			{
				int k = since / FurrowStep - 1;
				if (Main.netMode != NetmodeID.MultiplayerClient)
					for (int row = 0; row < FurrowRows; row++)
						SpawnMark(castFrom + FurrowAngle(row).ToRotationVector2() * (70f + k * FurrowSpacing));
				SoundEngine.PlaySound(SoundID.Item7 with { Pitch = -0.3f + k * 0.08f, Volume = 0.6f }, NPC.Center);
			}
			if (Timer >= FurrowTell + perRow * FurrowStep + HarvesterCut.Tell + 18)
				NextMove();
		}

		/// <summary>
		/// Jack-o'-lanterns, phase two on: conjured in a row over its head, each with its whole arc
		/// and burst ring drawn, then lobbed onto the floor round the player. Three, five in phase three.
		/// </summary>
		private void DoPumpkins(Player target)
		{
			Hover(target, 0.04f);
			const int tell = 20;
			if (Timer == tell)
			{
				SoundEngine.PlaySound(SoundID.Item20 with { Pitch = -0.5f }, NPC.Center);
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					int count = Phase >= 2f ? 5 : 3;
					int damage = NPC.GetAttackDamage_ForProjectiles(PumpkinDamage, PumpkinDamage);
					for (int i = 0; i < count; i++)
					{
						float off = i - (count - 1) / 2f;
						Vector2 from = NPC.Center + new Vector2(off * 36f, -78f + Math.Abs(off) * 8f);
						Vector2 to = Landing(target.Center + new Vector2(off * 150f, 0f));
						Projectile.NewProjectile(NPC.GetSource_FromAI(), from, Vector2.Zero, ModContent.ProjectileType<HarvesterPumpkin>(),
							damage, 0f, Main.myPlayer, to.X, to.Y);
					}
				}
			}
			if (Timer >= tell + HarvesterPumpkin.Hold + HarvesterPumpkin.Flight + 16)
				NextMove();
		}

		/// <summary>Where a lobbed pumpkin should burst: the floor (or a platform) under the point, else the point itself.</summary>
		private static Vector2 Landing(Vector2 at)
		{
			int tx = (int)(at.X / 16f);
			int ty = (int)(at.Y / 16f);
			for (int i = 0; i < 24; i++)
			{
				int y = ty + i;
				if (!WorldGen.InWorld(tx, y, 2))
					break;
				Tile t = Main.tile[tx, y];
				if (t.HasUnactuatedTile && (Main.tileSolid[t.TileType] || Main.tileSolidTop[t.TileType]))
					return new Vector2(at.X, y * 16f - 12f);
			}
			return at;
		}

		/// <summary>
		/// The Last Harvest, phase three: it stops, and its own reach becomes the blade - the circle
		/// stands with a door in it, then closes on where it was cast. The door is never where the
		/// player stands, so the answer is always a run.
		/// </summary>
		private void DoField(Player target)
		{
			NPC.velocity *= 0.8f;
			if (Timer == 1f)
			{
				SoundEngine.PlaySound(SoundID.Roar with { Pitch = -0.8f, Volume = 0.8f }, NPC.Center);
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					castFrom = NPC.Center;
					float toPlayer = (target.Center - NPC.Center).ToRotation();
					float door = toPlayer + (Main.rand.NextBool() ? 1f : -1f) * Main.rand.NextFloat(0.9f, 1.7f);
					Projectile.NewProjectile(NPC.GetSource_FromAI(), castFrom, Vector2.Zero, ModContent.ProjectileType<HarvesterField>(),
						NPC.GetAttackDamage_ForProjectiles(FieldDamage, FieldDamage), 0f, Main.myPlayer, door);
					NPC.netUpdate = true;
				}
			}
			if (Timer >= HarvesterField.Tell + HarvesterField.Close + 12)
				NextMove();
		}

		// ------------------------------------------------------------------ death and loot

		public override bool CheckDead()
			=> death.CheckDead(NPC, 130, () =>
			{
				int cut = ModContent.ProjectileType<HarvesterCut>();
				int crow = ModContent.ProjectileType<HarvesterCrow>();
				int scythe = ModContent.ProjectileType<HarvesterScythe>();
				foreach (Projectile p in Main.ActiveProjectiles)
				{
					if (p.type == cut || p.type == crow || p.type == scythe
					|| p.type == ModContent.ProjectileType<HarvesterPumpkin>() || p.type == ModContent.ProjectileType<HarvesterField>())
						p.Kill();
				}
			});

		public override void OnKill()
		{
			if (!DownedBossSystem.downedHarvester)
			{
				DownedBossSystem.downedHarvester = true;
				if (Main.netMode != NetmodeID.MultiplayerClient)
					ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Mods.Terrapex.Chat.HarvesterDown"),
						new Color(190, 236, 200));
				if (Main.netMode == NetmodeID.Server)
					NetMessage.SendData(MessageID.WorldData);
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<HollowHarvesterBag>()));

			LeadingConditionRule notExpert = new(new Conditions.NotExpert());
			notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<HollowHarvest>()));
			notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<CrowCharm>(), 2));
			notExpert.OnSuccess(ItemDropRule.Common(ItemID.Hay, 1, 30, 50));
			npcLoot.Add(notExpert);
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<HollowHarvesterMask>(), 7));
		}

		public override void BossLoot(ref int potionType) => potionType = ItemID.HealingPotion;

		// ------------------------------------------------------------------ drawing

		public override void FindFrame(int frameHeight)
		{
			NPC.frameCounter++;
			if (NPC.frameCounter >= 8)
			{
				NPC.frameCounter = 0;
				NPC.frame.Y = (NPC.frame.Y + frameHeight) % (frameHeight * Main.npcFrameCount[Type]);
			}
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			if (!NPC.IsABestiaryIconDummy)
				DrawReach(spriteBatch, screenPos);

			// the drawn aim line before a dash or a throw, brightening as it commits
			if (Move == Furrow && Timer > 0f && Timer <= FurrowTell && castFrom != Vector2.Zero)
			{
				// the rows it is about to reap: one line per row out to the rim, with the run of light
				// going outward the way the marks will
				float f = Timer / FurrowTell;
				for (int row = 0; row < FurrowRows; row++)
				{
					Vector2 dir = FurrowAngle(row).ToRotationVector2();
					Vector2 a = castFrom + dir * 60f, b = castFrom + dir * ReachRadius;
					RiftDraw.Line(a, b, new Color(14, 10, 20) * (0.35f * f), 8f);
					RiftDraw.Line(a, b, new Color(190, 255, 210) * (0.3f + 0.6f * f), 2f + 2f * f);
					float t = Timer * 0.04f % 1f;
					RiftDraw.Bloom(Vector2.Lerp(a, b, t), new Color(190, 255, 210, 0) * (0.6f * f), 0.25f);
				}
			}

			bool aiming = (Move == Sweep && Timer > 0f && Timer <= 40f) || (Move == Throw && Timer > 0f && Timer <= 36f);
			if (aiming)
			{
				float f = Timer / (Move == Sweep ? 40f : 36f);
				Vector2 dir = aimAngle.ToRotationVector2();
				RiftDraw.Line(NPC.Center, NPC.Center + dir * 900f, new Color(14, 10, 20) * (0.35f * f), 6f + 4f * f);
				RiftDraw.Line(NPC.Center, NPC.Center + dir * 900f, new Color(190, 236, 200) * (0.25f + 0.6f * f), 2f + 2f * f);
				// light running out along it, faster as it commits
				for (int k = 0; k < 3; k++)
				{
					float t = (Timer * (0.02f + 0.03f * f) + k / 3f) % 1f;
					RiftDraw.Bloom(NPC.Center + dir * (900f * t), new Color(190, 236, 200, 0) * (0.5f * f * (1f - t)), 0.22f);
				}
			}

			Texture2D body = TextureAssets.Npc[Type].Value;
			SpriteEffects fx = NPC.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Vector2 origin = NPC.frame.Size() / 2f;
			Vector2 center = NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY);

			// the dash leaves ghosts of itself along the line
			bool dashing = Move == Sweep && Timer > 40f && Timer <= 72f && !NPC.IsABestiaryIconDummy;
			if (dashing)
			{
				float fade = Timer <= 64f ? 1f : 1f - (Timer - 64f) / 8f;
				for (int i = NPC.oldPos.Length - 1; i >= 1; i--)
				{
					float f = 1f - i / (float)NPC.oldPos.Length;
					Vector2 at = NPC.oldPos[i] + NPC.Size * 0.5f - screenPos + new Vector2(0f, NPC.gfxOffY);
					spriteBatch.Draw(body, at, NPC.frame, new Color(150, 220, 170, 0) * (0.35f * f * fade), NPC.rotation, origin, 1f, fx, 0f);
				}
			}

			spriteBatch.Draw(body, center, NPC.frame, drawColor, NPC.rotation, origin, 1f, fx, 0f);

			if (!scytheOut)
				DrawScythe(spriteBatch, center, drawColor);

			// phase two and the throes: the pumpkin lantern in the head
			if (Phase >= 1f || death.Dying)
			{
				float time = Main.GlobalTimeWrappedHourly;
				float flicker = 0.75f + 0.25f * MathF.Sin(time * 13f) * MathF.Sin(time * 5.3f);
				if (death.Dying)
					flicker *= (1f - death.Progress * 0.6f) * (Main.rand.NextBool(6) ? 0.3f : 1f);
				Vector2 head = Head;
				RiftDraw.Bloom(head, new Color(255, 120, 40, 0) * (0.45f * flicker), 0.7f);
				RiftDraw.Bloom(head, new Color(255, 200, 110, 0) * (0.6f * flicker), 0.28f);
			}

			// the eyes flash before every move: the tell
			bool tell = (Move == Marks && Timer > 0f && Timer < 24f) || (Move == Crows && Timer > 0f && Timer < 20f)
				|| (Move == Pumpkins && Timer > 0f && Timer < 20f);
			if (tell)
			{
				Vector2 eye = center + new Vector2(6f * NPC.spriteDirection, -30f);
				RiftDraw.Bloom(eye + screenPos, new Color(150, 255, 170, 0) * 0.8f, 0.45f);
			}
			return false;
		}

		private const int Stalks = 96;

		/// <summary>Cheap stable hash in [0,1) for per-stalk variation that does not flicker.</summary>
		private static float Hash(int i, int salt)
		{
			uint h = (uint)(i * 374761393 + salt * 668265263);
			h = (h ^ (h >> 13)) * 1274126177u;
			return (h ^ (h >> 16)) % 10000u / 10000f;
		}

		/// <summary>
		/// The reach, drawn as a field's edge rather than a dashed line: a dark band just inside the
		/// rim, a thin ghost-light line with three brighter pulses running round it, and a border of
		/// straw stalks rooted on the rim, leaning in one wind. The rim itself stays a continuous
		/// bright line, so the rule it carries - marks land inside, hardest at the edge - is no less
		/// readable than the dashes it replaces.
		/// </summary>
		private void DrawReach(SpriteBatch sb, Vector2 screenPos)
		{
			Texture2D px = TextureAssets.MagicPixel.Value;
			Rectangle src = new(0, 0, 1, 1);
			Vector2 c = NPC.Center;
			float time = Main.GlobalTimeWrappedHourly;
			float R = ReachRadius;

			const int segs = 120;
			for (int i = 0; i < segs; i++)
			{
				float a0 = MathHelper.TwoPi * i / segs, a1 = MathHelper.TwoPi * (i + 1) / segs;
				Vector2 n0 = a0.ToRotationVector2(), n1 = a1.ToRotationVector2();

				// the dark band: the edge of the field, where the cut is heaviest
				Vector2 b0 = c + n0 * (R - 10f), b1 = c + n1 * (R - 10f);
				Vector2 seg = b1 - b0;
				sb.Draw(px, b0 - screenPos, src, new Color(10, 6, 16) * 0.26f, seg.ToRotation(), new Vector2(0f, 0.5f),
					new Vector2(seg.Length() + 1f, 18f), SpriteEffects.None, 0f);

				// the rim line, with three pulses travelling round
				float pulse = 0f;
				for (int k = 0; k < 3; k++)
				{
					float d = MathHelper.WrapAngle(a0 - (time * 0.35f + k * MathHelper.TwoPi / 3f));
					pulse = Math.Max(pulse, MathF.Exp(-d * d * 18f));
				}
				Vector2 r0 = c + n0 * R, r1 = c + n1 * R;
				seg = r1 - r0;
				sb.Draw(px, r0 - screenPos, src, new Color(190, 236, 200, 0) * (0.16f + 0.4f * pulse), seg.ToRotation(),
					new Vector2(0f, 0.5f), new Vector2(seg.Length() + 1f, 2f), SpriteEffects.None, 0f);
				if (pulse > 0.05f)
					sb.Draw(px, r0 - screenPos, src, new Color(190, 236, 200, 0) * (0.12f * pulse), seg.ToRotation(),
						new Vector2(0f, 0.5f), new Vector2(seg.Length() + 1f, 7f), SpriteEffects.None, 0f);
			}

			// the stalks: rooted on the rim, pointing out, bending with a wind that runs round the ring
			for (int i = 0; i < Stalks; i++)
			{
				float a = MathHelper.TwoPi * (i + Hash(i, 1) * 0.7f) / Stalks;
				Vector2 n = a.ToRotationVector2();
				Vector2 root = c + n * (R - 3f);
				float h = 12f + Hash(i, 2) * 12f;
				float sway = 0.35f + 0.3f * MathF.Sin(time * 1.6f + a * 3f + Hash(i, 3) * 2f);
				Vector2 p = root;
				for (int k = 0; k < 3; k++)
				{
					float bend = sway * (k + 1) / 3f;
					Vector2 d = n.RotatedBy(bend);
					Vector2 q = p + d * (h / 3f);
					Color col = k == 0 ? HarvestPalette.StrawDark : k == 1 ? HarvestPalette.StrawMid : HarvestPalette.Straw;
					Color lit = HarvestPalette.Lit(q, col, 0.42f) * 0.85f;
					Vector2 s = q - p;
					sb.Draw(px, p - screenPos, src, lit, s.ToRotation(), new Vector2(0f, 0.5f),
						new Vector2(s.Length() + 1f, k == 2 ? 1f : 2f), SpriteEffects.None, 0f);
					p = q;
				}
				// the ear of grain, with the faintest ghost-light on it
				if (Hash(i, 4) < 0.6f)
				{
					Vector2 d = n.RotatedBy(sway);
					sb.Draw(px, p - screenPos, src, HarvestPalette.Lit(p, HarvestPalette.StrawLight, 0.42f) * 0.85f,
						d.ToRotation(), new Vector2(0f, 0.5f), new Vector2(4f, 2.5f), SpriteEffects.None, 0f);
					sb.Draw(px, p - screenPos, src, new Color(190, 236, 200, 0) * 0.12f, d.ToRotation(), new Vector2(0f, 0.5f),
						new Vector2(5f, 4f), SpriteEffects.None, 0f);
				}
			}
		}

		private void DrawScythe(SpriteBatch spriteBatch, Vector2 center, Color drawColor)
		{
			Texture2D tex = TextureAssets.Projectile[ModContent.ProjectileType<HarvesterScythe>()].Value;
			int dir = NPC.spriteDirection;
			float angle = -0.9f + 0.08f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.4f);
			if (Move == Sweep && Timer > 40f && Timer <= 64f)
				angle = -0.9f + (Timer - 40f) / 24f * MathHelper.Pi * 1.5f;
			Vector2 hand = center + new Vector2(18f * dir, 8f);
			SpriteEffects fx = dir < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Vector2 origin = dir < 0 ? new Vector2(tex.Width, tex.Height) : new Vector2(0f, tex.Height);
			float rot = dir < 0 ? -angle : angle;
			spriteBatch.Draw(tex, hand, null, drawColor, rot, origin, 1f, fx, 0f);
		}
	}
}
