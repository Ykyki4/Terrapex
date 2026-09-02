using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Content.Items.Placeable;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Items.Consumables;
using Terrapex.Content.Items.Armor;
using Terrapex.Content.Items.Accessories;
using Terrapex.Common.Systems;
using Terrapex.Content.Items.Weapons;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.NPCs.Bosses
{
	[AutoloadBossHead]
	public class KeeperOfTheRift : ModNPC
	{
		// KeeperOfTheRift.png is 42 frames of 96px:
		private const int FrameP1Closed = 0;   // 0-7   phase 1, shell shut
		private const int FrameP2 = 8;         // 8-15  phase 2
		private const int FrameBreak = 16;     // 16-25 phase transition
		private const int FrameP3 = 26;        // 26-33 phase 3
		private const int FrameP1Open = 34;    // 34-41 phase 1, eye open
		private const int LoopLength = 8;
		private const int BreakFrameCount = 10;
		private const int BreakTicksPerFrame = 6;
		private const int BreakDuration = BreakFrameCount * BreakTicksPerFrame;

		private const float Phase2Threshold = 0.70f;
		private const float Phase3Threshold = 0.35f;
		private const float DesperationThreshold = 0.12f;
		private const int ClosedDefense = 44;
		private const int OpenDefense = 16;

		private enum AIState
		{
			Hover = 0,
			ShardFan,
			PlateSpear,
			CrossLaser,
			Charge,
			SpiralBarrage,
			MineField,
			SweepLaser,
			DashCombo,
			SummonAdds,
			RotorLasers,
			ShardWallSpin,
			TearField,
			Blink,
			PhaseBreak
		}

		// designed rotations rather than pure randomness, so the fight has a rhythm
		private static readonly AIState[] PatternP1 = {
			AIState.ShardFan, AIState.PlateSpear, AIState.CrossLaser, AIState.Charge
		};
		private static readonly AIState[] PatternP2 = {
			AIState.SpiralBarrage, AIState.MineField, AIState.SweepLaser,
			AIState.DashCombo, AIState.PlateSpear, AIState.SummonAdds
		};
		private static readonly AIState[] PatternP3 = {
			AIState.RotorLasers, AIState.ShardWallSpin, AIState.MineField, AIState.Blink,
			AIState.TearField, AIState.DashCombo, AIState.PlateSpear
		};

		private AIState State
		{
			get => (AIState)(int)NPC.ai[0];
			set
			{
				NPC.ai[0] = (int)value;
				NPC.ai[1] = 0f;
				NPC.ai[2] = 0f;
				NPC.netUpdate = true;
			}
		}

		private ref float Timer => ref NPC.ai[1];
		private ref float Counter => ref NPC.ai[2];
		private ref float Phase => ref NPC.ai[3];
		private ref float PatternIndex => ref NPC.localAI[1];

		private bool dashing;
		private bool enraged;
		private int loopFrame;
		private double loopCounter;

		private bool Desperate => NPC.life / (float)NPC.lifeMax <= DesperationThreshold;

		// The shell only covers the eye while the Keeper is idle in phase 1 and still has plates.
		// Break the plates, or punish the attack windows — both open it up.
		private bool ShellClosed => Phase <= 1f && State == AIState.Hover && CountPlates() > 0;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 42;
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			NPCID.Sets.BossBestiaryPriority.Add(Type);
			NPCID.Sets.TrailCacheLength[Type] = 12;
			NPCID.Sets.TrailingMode[Type] = 3;
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
		}

		public override void SetDefaults()
		{
			NPC.width = 84;
			NPC.height = 84;
			NPC.damage = 60;
			NPC.defense = ClosedDefense;
			NPC.lifeMax = 28000;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.knockBackResist = 0f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.boss = true;
			NPC.npcSlots = 12f;
			NPC.aiStyle = -1;
			NPC.value = Item.buyPrice(gold: 15);
			NPC.SpawnWithHigherTime(30);
			Music = MusicID.Boss2;
		}

		public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
		{
			NPC.lifeMax = (int)(NPC.lifeMax * 0.7f * balance);
			NPC.damage = (int)(NPC.damage * 0.85f);
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				new MoonLordPortraitBackgroundProviderBestiaryInfoElement(),
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.KeeperOfTheRift.Bestiary")
			});
		}

		/// <summary>The throes. Shared timing, own dust: the shell lets go and the eye burns out.</summary>
		private readonly BossDeath death = new();

		public override bool CheckDead()
			=> death.CheckDead(NPC, 150, () =>
			{
				ClearPlates();
			});

		public override bool CheckActive() => false;

		// ------------------------------------------------------------------ plates

		private static int PlateType => ModContent.NPCType<KeeperPlate>();

		private int CountPlates()
		{
			int n = 0;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC other = Main.npc[i];
				if (other.active && other.type == PlateType && (int)other.ai[0] == NPC.whoAmI)
					n++;
			}
			return n;
		}

		private List<NPC> OrbitingPlates()
		{
			var list = new List<NPC>();
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC other = Main.npc[i];
				if (other.active && other.type == PlateType && (int)other.ai[0] == NPC.whoAmI
					&& (int)other.ai[3] == KeeperPlate.ModeOrbit)
					list.Add(other);
			}
			return list;
		}

		private void ClearPlates()
		{
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC other = Main.npc[i];
				if (other.active && other.type == PlateType && (int)other.ai[0] == NPC.whoAmI)
				{
					other.life = 0;
					other.HitEffect();
					other.active = false;
					if (Main.netMode == NetmodeID.Server)
						NetMessage.SendData(MessageID.SyncNPC, number: i);
				}
			}
		}

		private void SpawnPlates(int count, float radius, int life)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			for (int i = 0; i < count; i++)
			{
				float angle = MathHelper.TwoPi / count * i;
				Vector2 pos = NPC.Center + angle.ToRotationVector2() * radius;
				int idx = NPC.NewNPC(NPC.GetSource_FromAI(), (int)pos.X, (int)pos.Y, PlateType, 0,
					NPC.whoAmI, angle, radius, KeeperPlate.ModeOrbit);

				if (idx >= 0 && idx < Main.maxNPCs)
				{
					Main.npc[idx].lifeMax = Main.npc[idx].life = life;
					if (Main.netMode == NetmodeID.Server)
						NetMessage.SendData(MessageID.SyncNPC, number: idx);
				}
			}
		}

		// ------------------------------------------------------------------ damage

		private void ApplyShellReduction(ref NPC.HitModifiers modifiers)
		{
			if (ShellClosed)
				modifiers.FinalDamage *= 0.35f;
		}

		public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
			=> ApplyShellReduction(ref modifiers);

		public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
			=> ApplyShellReduction(ref modifiers);

		// ------------------------------------------------------------------ main loop

		public override void AI()
		{
			// nothing else runs while it is coming apart: no attacks, no targeting,
			// no phase checks. The NPC may be gone the moment Tick returns true.
			if (death.Dying)
			{
				death.Tick(NPC, DustID.PurpleTorch, new Color(200, 90, 230));
				return;
			}

			if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
				NPC.TargetClosest();

			Player target = Main.player[NPC.target];

			if (target.dead || !target.active)
			{
				ClearPlates();
				NPC.velocity.Y -= 0.35f;
				NPC.EncourageDespawn(60);
				return;
			}

			if (Phase < 1f)
			{
				Phase = 1f;
				SpawnPlates(8, 150f, 900);
			}

			enraged = NPC.Distance(target.Center) > 1800f;
			dashing = false;

			float lifeRatio = NPC.life / (float)NPC.lifeMax;
			if (State != AIState.PhaseBreak)
			{
				bool shellBroken = Phase == 1f && CountPlates() == 0;
				if (Phase == 1f && (lifeRatio <= Phase2Threshold || shellBroken))
				{
					StartBreak(2f);
					return;
				}
				if (Phase == 2f && lifeRatio <= Phase3Threshold)
				{
					StartBreak(3f);
					return;
				}
			}

			NPC.defense = ShellClosed ? ClosedDefense : OpenDefense;
			NPC.damage = enraged ? 120 : 60;
			Timer++;

			switch (State)
			{
				case AIState.Hover: DoHover(target); break;
				case AIState.ShardFan: DoShardFan(target); break;
				case AIState.PlateSpear: DoPlateSpear(target); break;
				case AIState.CrossLaser: DoCrossLaser(target); break;
				case AIState.Charge: DoCharge(target, 3, 60, 21f); break;
				case AIState.SpiralBarrage: DoSpiralBarrage(target); break;
				case AIState.MineField: DoMineField(target); break;
				case AIState.SweepLaser: DoSweepLaser(target); break;
				case AIState.DashCombo: DoCharge(target, 4, 42, 25f); break;
				case AIState.SummonAdds: DoSummonAdds(target); break;
				case AIState.RotorLasers: DoRotorLasers(target); break;
				case AIState.ShardWallSpin: DoShardWallSpin(target); break;
				case AIState.TearField: DoTearField(target); break;
				case AIState.Blink: DoBlink(target); break;
				case AIState.PhaseBreak: DoPhaseBreak(); break;
			}
		}

		// ------------------------------------------------------------------ helpers

		private static int ProjDamage(int damage) => Main.expertMode ? damage / 2 : damage;

		private void Fire(Vector2 velocity, int type, int damage, float knockBack = 3f)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity, type,
				ProjDamage(damage), knockBack, Main.myPlayer);
		}

		private void FireLaser(float angle, float angularVelocity, int lifetime, int damage)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			int idx = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center,
				angle.ToRotationVector2(), ModContent.ProjectileType<RiftLaser>(),
				ProjDamage(damage), 4f, Main.myPlayer, NPC.whoAmI, angularVelocity);
			if (idx >= 0 && idx < Main.maxProjectiles)
			{
				Main.projectile[idx].timeLeft = lifetime;
				if (Main.netMode == NetmodeID.Server)
					NetMessage.SendData(MessageID.SyncProjectile, number: idx);
			}
		}

		private void Shake(float strength, int frames)
		{
			if (Main.netMode == NetmodeID.Server)
				return;
			Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center,
				Main.rand.NextVector2Unit(), strength, 6f, frames, 2400f, FullName));
		}

		private void EyeFlash(int count = 16)
		{
			for (int i = 0; i < count; i++)
			{
				Dust d = Dust.NewDustPerfect(NPC.Center, DustID.PurpleTorch,
					Main.rand.NextVector2Circular(4f, 4f), 100, default, 1.5f);
				d.noGravity = true;
			}
		}

		private static void Announce(string key)
		{
			var color = new Color(214, 107, 255);
			if (Main.netMode == NetmodeID.SinglePlayer)
				Main.NewText(Language.GetTextValue(key), color);
			else if (Main.netMode == NetmodeID.Server)
				ChatHelper.BroadcastChatMessage(NetworkText.FromKey(key), color);
		}

		private void Drift(Player target, Vector2 offset, float speed, float lerp = 0.045f)
		{
			if (enraged)
				speed *= 1.7f;

			Vector2 destination = target.Center + offset;
			Vector2 desired = (destination - NPC.Center).SafeNormalize(Vector2.UnitY) * speed;
			float distance = Vector2.Distance(destination, NPC.Center);
			if (distance < 90f)
				desired *= distance / 90f;
			NPC.velocity = Vector2.Lerp(NPC.velocity, desired, lerp);
		}

		private void EndAttack() => State = AIState.Hover;

		// ------------------------------------------------------------------ idle / selection

		private void DoHover(Player target)
		{
			float speed = Phase >= 3f ? 9f : (Phase >= 2f ? 7f : 5f);
			Drift(target, new Vector2(0f, -190f), speed);

			int wait = Desperate ? 24 : (Phase >= 3f ? 40 : (Phase >= 2f ? 60 : 90));
			if (Timer >= wait)
				PickAttack();
		}

		private void PickAttack()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			AIState[] pattern = Phase >= 3f ? PatternP3 : (Phase >= 2f ? PatternP2 : PatternP1);
			int index = (int)PatternIndex;

			AIState next = pattern[index % pattern.Length];

			// PlateSpear needs plates in orbit; fall through to the next entry if there are none
			if (next == AIState.PlateSpear && OrbitingPlates().Count == 0)
			{
				index++;
				next = pattern[index % pattern.Length];
			}

			PatternIndex = index + 1;
			State = next;
		}

		// ------------------------------------------------------------------ phase break

		private void StartBreak(float nextPhase)
		{
			State = AIState.PhaseBreak;
			Counter = nextPhase;
			NPC.velocity *= 0.3f;
			ClearPlates();

			SoundEngine.PlaySound(SoundID.Roar with { Pitch = -0.2f }, NPC.Center);
			Shake(16f, 40);
			Announce(nextPhase >= 3f ? "Mods.Terrapex.Chat.KeeperPhase3" : "Mods.Terrapex.Chat.KeeperPhase2");

			for (int i = 0; i < 50; i++)
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.PurpleTorch,
					Main.rand.NextFloat(-7f, 7f), Main.rand.NextFloat(-7f, 7f), 100, default, 2f);
				d.noGravity = true;
			}
		}

		private void DoPhaseBreak()
		{
			// stunned and fully exposed: the reward for forcing the phase
			NPC.velocity *= 0.92f;
			NPC.defense = 0;

			if (Timer == BreakTicksPerFrame * 4)
			{
				SoundEngine.PlaySound(SoundID.Item14, NPC.Center);
				Shake(22f, 30);
				for (int i = 0; i < 80; i++)
				{
					Dust d = Dust.NewDustPerfect(NPC.Center, DustID.PinkTorch,
						Main.rand.NextVector2CircularEdge(11f, 11f), 100, default, 2.2f);
					d.noGravity = true;
				}
			}

			if (Timer >= BreakDuration)
			{
				Phase = Counter;
				PatternIndex = 0f;

				if (Phase == 2f)
				{
					SpawnPlates(5, 200f, 1100);
					if (Main.netMode != NetmodeID.MultiplayerClient)
					{
						for (int i = 0; i < 3; i++)
						{
							Vector2 pos = NPC.Center + Main.rand.NextVector2CircularEdge(240f, 180f);
							NPC.NewNPC(NPC.GetSource_FromAI(), (int)pos.X, (int)pos.Y,
								ModContent.NPCType<Riftling>());
						}
					}
				}
				else
				{
					SpawnPlates(10, 250f, 700);
				}

				EndAttack();
			}
		}

		// ------------------------------------------------------------------ attacks

		private void DoShardFan(Player target)
		{
			NPC.velocity *= 0.94f;

			if (Timer == 20f)
				EyeFlash(24);

			if (Timer is 26f or 38f or 50f)
			{
				SoundEngine.PlaySound(SoundID.Item92, NPC.Center);
				Vector2 aim = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitY) * 5.8f;
				float drift = MathHelper.ToRadians(6f) * (Counter - 1f);
				for (int i = -3; i <= 3; i++)
					Fire(aim.RotatedBy(MathHelper.ToRadians(11f) * i + drift),
						ModContent.ProjectileType<RiftShard>(), 60);
				Counter++;
			}

			if (Timer >= 72f)
				EndAttack();
		}

		private void DoPlateSpear(Player target)
		{
			Drift(target, new Vector2(0f, -200f), 4f, 0.03f);

			if (Timer == 10f)
			{
				var plates = OrbitingPlates();
				int throwCount = Phase >= 3f ? 4 : (Phase >= 2f ? 3 : 2);
				for (int i = 0; i < Math.Min(throwCount, plates.Count); i++)
				{
					if (plates[i].ModNPC is KeeperPlate plate)
						plate.BeginSpear();
				}
			}

			if (Timer >= 150f)
				EndAttack();
		}

		private void DoCrossLaser(Player target)
		{
			Drift(target, new Vector2(0f, -200f), 3.5f, 0.03f);

			if (Timer == 8f)
			{
				EyeFlash(20);
				float baseAngle = (target.Center - NPC.Center).ToRotation();
				float spin = Main.rand.NextBool() ? 0.0055f : -0.0055f;
				FireLaser(baseAngle, spin, 170, 90);
				FireLaser(baseAngle + MathHelper.PiOver2, spin, 170, 90);
				FireLaser(baseAngle + MathHelper.Pi, spin, 170, 90);
				FireLaser(baseAngle + MathHelper.Pi + MathHelper.PiOver2, spin, 170, 90);
			}

			if (Timer >= 180f)
				EndAttack();
		}

		private void DoCharge(Player target, int dashes, int cycle, float speed)
		{
			int aim = cycle - 34;
			float local = Timer % cycle;

			if (local < aim)
			{
				// stop dead and line up — this pause is the whole tell
				NPC.velocity *= 0.86f;
				if (local == aim - 8f)
					EyeFlash(18);
			}
			else if (local == aim)
			{
				NPC.velocity = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitY)
					* (enraged ? speed * 1.35f : speed);
				SoundEngine.PlaySound(SoundID.ForceRoar with { Pitch = 0.3f }, NPC.Center);
				Shake(10f, 18);

				if (Phase >= 2f)
				{
					Vector2 side = NPC.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2) * 5f;
					Fire(side, ModContent.ProjectileType<RiftShard>(), 55);
					Fire(-side, ModContent.ProjectileType<RiftShard>(), 55);
				}
			}
			else
			{
				dashing = true;
				NPC.damage = 120;
				NPC.velocity *= 0.985f;
			}

			if (Timer >= cycle * dashes)
				EndAttack();
		}

		private void DoSpiralBarrage(Player target)
		{
			Drift(target, new Vector2(0f, -210f), 4f, 0.028f);

			if (Timer == 16f)
				EyeFlash(22);

			if (Timer >= 20f && Timer < 142f && Timer % 3f == 0f)
			{
				float baseAngle = (target.Center - NPC.Center).ToRotation();
				float step = MathHelper.ToRadians(17f) * Counter;
				// two arms, opposite directions, so the safe lane keeps moving
				Fire((baseAngle + step).ToRotationVector2() * 5.2f, ModContent.ProjectileType<RiftShard>(), 55);
				Fire((baseAngle - step + MathHelper.Pi).ToRotationVector2() * 5.2f,
					ModContent.ProjectileType<RiftShard>(), 55);
				Counter++;
				if (Counter % 5f == 0f)
					SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
			}

			if (Timer >= 162f)
				EndAttack();
		}

		private void DoMineField(Player target)
		{
			Drift(target, new Vector2(0f, -220f), 5f, 0.03f);

			if (Timer == 22f)
			{
				SoundEngine.PlaySound(SoundID.Item103, NPC.Center);
				EyeFlash(26);

				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					int count = Phase >= 3f ? 9 : 7;
					float spin = Main.rand.NextFloat(MathHelper.TwoPi);
					for (int i = 0; i < count; i++)
					{
						float angle = MathHelper.TwoPi / count * i + spin;
						Vector2 pos = target.Center + angle.ToRotationVector2() * Main.rand.NextFloat(150f, 300f);
						Vector2 vel = angle.ToRotationVector2() * 3.5f;
						// staggered fuses make the field pop as a cascade, not a wall
						Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, vel,
							ModContent.ProjectileType<RiftMine>(), ProjDamage(58), 2f, Main.myPlayer, i * 9);
					}
				}
			}

			if (Timer >= 95f)
				EndAttack();
		}

		private void DoSweepLaser(Player target)
		{
			Drift(target, new Vector2(0f, -240f), 3f, 0.025f);

			if (Timer == 10f)
			{
				EyeFlash(20);
				float start = (target.Center - NPC.Center).ToRotation();
				bool clockwise = Main.rand.NextBool();
				float arc = MathHelper.ToRadians(150f);
				float ticks = 170f;
				FireLaser(start + (clockwise ? -arc * 0.5f : arc * 0.5f),
					(clockwise ? arc : -arc) / ticks, 200, 100);
			}

			if (Timer >= 210f)
				EndAttack();
		}

		private void DoSummonAdds(Player target)
		{
			NPC.velocity *= 0.95f;

			if (Timer == 30f)
			{
				EyeFlash(26);
				SoundEngine.PlaySound(SoundID.Item44, NPC.Center);
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					for (int i = 0; i < 3; i++)
					{
						Vector2 pos = NPC.Center + Main.rand.NextVector2CircularEdge(200f, 150f);
						NPC.NewNPC(NPC.GetSource_FromAI(), (int)pos.X, (int)pos.Y,
							ModContent.NPCType<Riftling>());
					}
				}
			}

			if (Timer >= 62f)
				EndAttack();
		}

		private void DoRotorLasers(Player target)
		{
			Drift(target, new Vector2(0f, -230f), 2.5f, 0.02f);

			if (Timer == 12f)
			{
				EyeFlash(30);
				Shake(8f, 20);
				float baseAngle = (target.Center - NPC.Center).ToRotation() + MathHelper.PiOver4;
				float spin = Main.rand.NextBool() ? 0.0085f : -0.0085f;
				int arms = Desperate ? 4 : 3;
				for (int i = 0; i < arms; i++)
					FireLaser(baseAngle + MathHelper.TwoPi / arms * i, spin, 250, 105);
			}

			if (Timer >= 265f)
				EndAttack();
		}

		private void DoShardWallSpin(Player target)
		{
			Drift(target, new Vector2(0f, -200f), 4f, 0.03f);

			if (Timer >= 20f && Timer < 130f && (Timer - 20f) % 26f == 0f)
			{
				SoundEngine.PlaySound(SoundID.Item92 with { Pitch = -0.3f }, NPC.Center);
				float gap = (target.Center - NPC.Center).ToRotation() + Counter * 0.75f;
				const int spokes = 18;
				for (int i = 0; i < spokes; i++)
				{
					float angle = MathHelper.TwoPi / spokes * i;
					// one rotating opening keeps every ring dodgeable
					if (Math.Abs(MathHelper.WrapAngle(angle - gap)) < 0.40f)
						continue;
					Fire(angle.ToRotationVector2() * 5f, ModContent.ProjectileType<RiftShard>(), 62);
				}
				Counter++;
			}

			if (Timer >= 155f)
				EndAttack();
		}

		private void DoTearField(Player target)
		{
			Drift(target, new Vector2(0f, -230f), 5f, 0.03f);

			// spaced out so each rift gets its telegraph to itself instead of three overlapping
			if (Timer is 20f or 42f or 64f or 86f && Main.netMode != NetmodeID.MultiplayerClient)
			{
				Vector2 pos = target.Center + Main.rand.NextVector2Circular(300f, 210f);
				Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, Vector2.Zero,
					ModContent.ProjectileType<RiftTear>(), ProjDamage(64), 2f, Main.myPlayer);
			}

			if (Timer >= 140f)
				EndAttack();
		}

		private void DoBlink(Player target)
		{
			float local = Timer % 55f;

			if (local < 18f)
			{
				NPC.velocity *= 0.85f;
				NPC.alpha = (int)(local / 18f * 255f);
			}
			else if (local == 18f)
			{
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					Vector2 offset = Main.rand.NextVector2CircularEdge(240f, 190f);
					NPC.Center = target.Center + offset;
					NPC.velocity = Vector2.Zero;
					NPC.netUpdate = true;
				}
				SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
				EyeFlash(30);
			}
			else if (local == 26f)
			{
				// point blank ring: punishes standing still after the teleport
				SoundEngine.PlaySound(SoundID.Item92, NPC.Center);
				Shake(8f, 16);
				float spin = Main.rand.NextFloat(MathHelper.TwoPi);
				for (int i = 0; i < 16; i++)
					Fire((MathHelper.TwoPi / 16f * i + spin).ToRotationVector2() * 4.8f,
						ModContent.ProjectileType<RiftShard>(), 58);
			}
			else
			{
				NPC.alpha = (int)MathHelper.Clamp(255f - (local - 18f) / 16f * 255f, 0f, 255f);
			}

			if (Timer >= 110f)
			{
				NPC.alpha = 0;
				EndAttack();
			}
		}

		// ------------------------------------------------------------------ visuals

		public override void FindFrame(int frameHeight)
		{
			int frame;

			if (State == AIState.PhaseBreak)
			{
				int f = (int)(Timer / BreakTicksPerFrame);
				frame = FrameBreak + Math.Clamp(f, 0, BreakFrameCount - 1);
			}
			else
			{
				loopCounter += Desperate ? 1.6 : 1.0;
				if (loopCounter >= 6.0)
				{
					loopCounter = 0.0;
					loopFrame = (loopFrame + 1) % LoopLength;
				}

				int start = Phase >= 3f ? FrameP3
					: Phase >= 2f ? FrameP2
					: (ShellClosed ? FrameP1Closed : FrameP1Open);
				frame = start + loopFrame;
			}

			NPC.frame.Y = frame * frameHeight;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			if (dashing)
			{
				Texture2D tex = TextureAssets.Npc[Type].Value;
				var trail = new Color(178, 96, 255);
				for (int i = 1; i < NPC.oldPos.Length; i++)
				{
					float fade = (1f - i / (float)NPC.oldPos.Length) * 0.5f;
					Vector2 pos = NPC.oldPos[i] + NPC.Size * 0.5f - screenPos;
					spriteBatch.Draw(tex, pos, NPC.frame, trail * fade, NPC.rotation,
						NPC.frame.Size() * 0.5f, NPC.scale, SpriteEffects.None, 0f);
				}
			}
			return true;
		}

		public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			if (!ModContent.HasAsset(Texture + "_Glow"))
				return;

			Texture2D glow = ModContent.Request<Texture2D>(Texture + "_Glow").Value;
			spriteBatch.Draw(glow, NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY),
				NPC.frame, Color.White * (1f - NPC.alpha / 255f), NPC.rotation,
				NPC.frame.Size() * 0.5f, NPC.scale, SpriteEffects.None, 0f);
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 70 : 6); i++)
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height,
					Main.rand.NextBool() ? DustID.PurpleTorch : DustID.PinkTorch,
					hit.HitDirection * 2f, -1.5f, 100, default, NPC.life <= 0 ? 2.2f : 1.2f);
				d.noGravity = true;
			}
		}

		// ------------------------------------------------------------------ loot

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			// expert players get the bag instead of the loose drops, the vanilla contract
			npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<KeeperBag>()));

			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<KeeperTrophy>(), 10));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<KeeperMask>(), 7));

			LeadingConditionRule notExpert = new LeadingConditionRule(new Conditions.NotExpert());
			notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<RiftEssence>(), 1, 18, 26));
			notExpert.OnSuccess(ItemDropRule.OneFromOptions(1,
				ModContent.ItemType<RiftshardCleaver>(),
				ModContent.ItemType<Rib>(),
				ModContent.ItemType<Riftflow>(),
				ModContent.ItemType<WardenPlate>(),
				ModContent.ItemType<OrbitLash>(),
				ModContent.ItemType<ShardCaster>()));
			notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<CompanionEye>()));
			npcLoot.Add(notExpert);
		}

		public override void OnKill()
		{
			ClearPlates();

			if (!DownedBossSystem.downedKeeper)
			{
				DownedBossSystem.downedKeeper = true;
				if (Main.netMode == NetmodeID.Server)
					NetMessage.SendData(MessageID.WorldData);
			}
		}
	}
}
