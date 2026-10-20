using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.GameContent;
using Terrapex.Common.Particles;
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
using Terrapex.Content.Items.Weapons.Reaper;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.NPCs.Bosses
{
	/// <summary>
	/// The Dormant Eye — the mod's first boss and the Keeper's younger sibling.
	///
	/// It exists to teach one thing before hardmode: <b>the shell comes off, and the
	/// open eye is the window</b>. So it is the Keeper's fight with everything else
	/// stripped away — four plates instead of eight, one orbit instead of three, and
	/// the eye opens exactly once, at 40%, which is also the whole of phase two.
	///
	/// Health was 3200 behind a flat 50% shell, about twice the Eye of Cthulhu in practice; it is
	/// 2300 now, with a shell that is a dial (12% per standing plate). The time saved went into
	/// moves: a fixed rotation per phase - rockfall, a committed bowl along the floor, a leap onto
	/// a marked spot that sends tremors both ways, and with the eye open a beam, a sweeping glare
	/// and flat volleys of shards. Every one of them is drawn before it can hurt.
	///
	/// DormantEye.png is 12 frames of 64 px: 0-5 the shell shut, 6-11 the eye open.
	/// </summary>
	[AutoloadBossHead]
	public class DormantEye : ModNPC
	{
		private const int FrameShut = 0;
		private const int FrameOpen = 6;
		private const int LoopLength = 6;

		// the eye opens at 40%: phase two is where most of the moves are, so it gets a real share
		private const float PhaseTwoThreshold = 0.4f;
		private const int ShutDefense = 10;
		private const int OpenDefense = 6;
		private const int BreakDuration = 90;

		private const int PlateCount = 4;
		private const float PlateRadius = 92f;
		private const int PlateLife = 120;
		private const float PlateShelter = 0.12f;

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
			NPC.lifeMax = 2300;
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

		/// <summary>Plates of this Eye still standing. Every client can count them, so nothing is synced.</summary>
		private int PlatesStanding
		{
			get
			{
				int plate = ModContent.NPCType<EyePlate>();
				int n = 0;
				for (int i = 0; i < Main.maxNPCs; i++)
				{
					NPC other = Main.npc[i];
					if (other.active && other.type == plate && ((EyePlate)other.ModNPC).BelongsTo(NPC))
						n++;
				}
				return n;
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

		// A dial, not a gate: every plate standing takes 12% off what the core receives. It used to
		// be a flat half for as long as any plate was up, which with the break at 25% came to
		// about twice the Eye of Cthulhu's health in practice. Breaking the shell is now a price
		// the player sets, and the core is never a wall.
		public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
		{
			if (Phase == 0f)
				modifiers.FinalDamage *= 1f - PlateShelter * PlatesStanding;
		}

		public override void AI()
		{
			// nothing else runs while it is coming apart: no attacks, no targeting,
			// no phase checks. The NPC may be gone the moment Tick returns true.
			if (death.Dying)
			{
				NPC.noGravity = false;
				Vector2 lastCenter = NPC.Center;
				DeathCosmetics();
				if (death.Tick(NPC, DustID.Stone, new Color(150, 120, 200)))
					DeathBurst(lastCenter);
				return;
			}

			if (!Main.dedServ)
				Cosmetics();

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

			// ---- the one phase change
			if (Phase == 0f && NPC.life <= NPC.lifeMax * PhaseTwoThreshold)
			{
				Phase = 1f;
				BreakTimer = BreakDuration;
				NPC.defense = OpenDefense;
				KillPlates();
				NPC.velocity.X *= 0.2f;
				NPC.noGravity = false;
				move = Move.Roll;
				moveTimer = 0;
				rotationIndex = 0;
				repeats = 0;
				rest = 0;
				SoundEngine.PlaySound(SoundID.NPCDeath43, NPC.Center);
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(
					NPC.Center, Main.rand.NextVector2Unit(), 12f, 6f, 24, 1000f, "DormantEye"));
				for (int i = 0; i < 40; i++)
				{
					Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height,
						DustID.Stone, Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-6f, 6f), 60, default, 1.5f);
					d.noGravity = true;
				}
				if (!Main.dedServ)
					CrackOpen();
				if (Main.netMode != NetmodeID.MultiplayerClient)
					ChatHelper.BroadcastChatMessage(
						NetworkText.FromKey("Mods.Terrapex.Chat.DormantEyeOpens"), new Color(200, 120, 240));
				NPC.netUpdate = true;
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
				if (!Main.dedServ)
					BreakCosmetics();
				return;
			}
			NPC.defense = Phase == 0f ? ShutDefense : OpenDefense;

			// recovery after every move: it slows, grinds to a stop, and only then picks the next
			if (rest > 0)
			{
				rest--;
				NPC.velocity.X *= 0.92f;
				NPC.rotation += NPC.velocity.X * 0.045f;
				return;
			}

			switch (move)
			{
				case Move.Roll:
					Roll(target, 1f);
					if (++moveTimer >= (Phase == 0f ? 100 : 64))
						NextMove();
					break;
				case Move.Rockfall:
					Rockfall(target, Phase == 0f ? 5 : 7);
					EndMove(10);
					break;
				case Move.Bowl:
					Bowl(target);
					break;
				case Move.Leap:
					Leap(target);
					break;
				case Move.Beam:
					Roll(target, 0.5f);
					if (moveTimer == 0)
						Beam(target, 0.004f, 0f);
					if (++moveTimer >= 236)
						EndMove(20);
					break;
				case Move.Glare:
					Glare(target);
					break;
				case Move.Volley:
					Volley(target);
					break;
			}
		}

		// ------------------------------------------------------------------ the rotation
		//
		// Fixed per phase, never random: a boss you can learn. Between any two moves it rolls at
		// you for a while, which is the breathing room and the time to break plates.

		private enum Move : byte { Roll, Rockfall, Bowl, Leap, Beam, Glare, Volley }

		private static readonly Move[] ShellRotation = { Move.Rockfall, Move.Bowl, Move.Rockfall, Move.Leap };
		private static readonly Move[] OpenRotation = { Move.Beam, Move.Leap, Move.Glare, Move.Bowl, Move.Volley, Move.Leap };

		private const int BowlWind = 50;
		private const int BowlRun = 75;
		private const int LeapFlightMax = 130;
		private const float LeapGravity = 0.32f;
		private const int GlareWind = 34;
		private const int VolleyFlash = 20;
		private const int VolleyGap = 30;

		private Move move = Move.Roll;
		private int moveTimer;
		private int rotationIndex;
		private int repeats;
		private int rest;
		private int hopCooldown;
		private int bowlDir = 1;
		private Vector2 leapSpot;
		private float leapPrevVY;

		public override void SendExtraAI(System.IO.BinaryWriter writer)
		{
			writer.Write((byte)move);
			writer.Write(moveTimer);
			writer.Write(rotationIndex);
			writer.Write((byte)repeats);
			writer.Write((short)rest);
			writer.Write((sbyte)bowlDir);
			writer.WriteVector2(leapSpot);
		}

		public override void ReceiveExtraAI(System.IO.BinaryReader reader)
		{
			move = (Move)reader.ReadByte();
			moveTimer = reader.ReadInt32();
			rotationIndex = reader.ReadInt32();
			repeats = reader.ReadByte();
			rest = reader.ReadInt16();
			bowlDir = reader.ReadSByte();
			leapSpot = reader.ReadVector2();
		}

		private void NextMove()
		{
			Move[] rot = Phase == 0f ? ShellRotation : OpenRotation;
			move = rot[rotationIndex % rot.Length];
			rotationIndex++;
			moveTimer = 0;
			repeats = 0;
			NPC.netUpdate = true;
		}

		private void EndMove(int recovery)
		{
			move = Move.Roll;
			moveTimer = 0;
			repeats = 0;
			rest = recovery;
			NPC.noGravity = false;
			NPC.netUpdate = true;
		}

		/// <summary>Chase along the ground, hopping when a ledge or wall gets in the way.</summary>
		private void Roll(Player target, float speed)
		{
			float top = (Phase == 0f ? 3.6f : 5.2f) * speed;
			// running away is not an out: it gets faster the further you go
			if (NPC.Distance(target.Center) > 900f)
				top *= 1.6f;

			float dir = Math.Sign(target.Center.X - NPC.Center.X);
			NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X + dir * 0.10f, -top, top);

			if (hopCooldown > 0)
				hopCooldown--;
			bool onGround = NPC.velocity.Y == 0f;
			if (onGround)
			{
				NPC.velocity.X *= 0.99f;
				// a player standing above is never safe: once it is close enough underneath, it jumps
				// exactly high enough to land on their level - platforms let it through on the way up
				float above = NPC.Bottom.Y - target.Bottom.Y;
				float dx = target.Center.X - NPC.Center.X;
				if (above > 40f && Math.Abs(dx) < 360f && hopCooldown <= 0)
					HopUp(above, dx);
				else if (NPC.collideX)
					NPC.velocity.Y = -8.2f;
			}

			NPC.rotation += NPC.velocity.X * 0.045f;

			if (Math.Abs(NPC.velocity.X) > 1.5f && NPC.velocity.Y == 0f && Main.rand.NextBool(4))
			{
				Dust d = Dust.NewDustDirect(new Vector2(NPC.position.X, NPC.position.Y + NPC.height - 6f),
					NPC.width, 6, DustID.Stone, -NPC.velocity.X * 0.3f, -1f, 120, default, 1f);
				d.velocity *= 0.6f;
			}

			// the roll grinding the floor: grit kicked up behind it and the odd chip of shell
			if (!Main.dedServ && Math.Abs(NPC.velocity.X) > 1.5f && NPC.velocity.Y == 0f)
				Grind(1f);
		}

		private const float NpcGravity = 0.3f;

		private void HopUp(float above, float dx)
		{
			float h = Math.Min(above + 30f, 360f);
			float vy = -MathF.Sqrt(2f * NpcGravity * h);
			float air = -vy / NpcGravity + 8f;
			NPC.velocity.Y = vy;
			NPC.velocity.X = MathHelper.Clamp(dx / air, -7f, 7f);
			hopCooldown = 40;
			NPC.netUpdate = true;
			SoundEngine.PlaySound(SoundID.Item69 with { Pitch = -0.8f, Volume = 0.5f }, NPC.Center);
			if (!Main.dedServ)
				HarvestFX.MatteSlam(NPC.Bottom, Grit[1], 0.5f);
		}

		/// <summary>
		/// Platforms are no hiding place in either direction: it drops through them when the
		/// player is below, and stands on them when the player is on them or above. The leap
		/// always lands where it was aimed.
		/// </summary>
		public override bool? CanFallThroughPlatforms()
		{
			if (move == Move.Leap)
				return false;
			Player t = Main.player[NPC.target];
			return t.active && !t.dead && t.Bottom.Y > NPC.Bottom.Y + 20f;
		}

		/// <summary>A solid floor or a platform top: anything the player could be standing on.</summary>
		public static bool Standable(int x, int y)
		{
			if (!WorldGen.InWorld(x, y, 2))
				return false;
			Tile t = Main.tile[x, y];
			return t.HasTile && !t.IsActuated && (Main.tileSolid[t.TileType] || Main.tileSolidTop[t.TileType]);
		}

		private void Grind(float amount)
		{
			Vector2 contact = NPC.Bottom + new Vector2(-Math.Sign(NPC.velocity.X == 0f ? bowlDir : NPC.velocity.X) * 10f, -2f);
			if (Main.rand.NextFloat() < 0.33f * amount)
				ParticleSystem.Spawn(new Smoke(contact + new Vector2(Main.rand.NextFloat(-6f, 6f), 0f),
					new Vector2(-NPC.velocity.X * Main.rand.NextFloat(0.2f, 0.45f), -Main.rand.NextFloat(0.3f, 1.1f)),
					Grit[Main.rand.Next(Grit.Length)], Main.rand.NextFloat(0.16f, 0.3f), Main.rand.Next(28, 44)));
			if (Main.rand.NextFloat() < 0.14f * amount)
				ParticleSystem.Spawn(new Chip(contact, new Vector2(-NPC.velocity.X * Main.rand.NextFloat(0.3f, 0.7f),
					-Main.rand.NextFloat(1.5f, 3.5f)), Stone[Main.rand.Next(Stone.Length)], Main.rand.NextFloat(0.7f, 1.2f), 50));
		}

		/// <summary>Stones queued over the player, each announcing its own landing spot.</summary>
		private void Rockfall(Player target, int count)
		{
			SoundEngine.PlaySound(SoundID.Item14, NPC.Center);
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			int damage = NPC.damage / 3;
			for (int i = 0; i < count; i++)
			{
				float x = target.Center.X + (i - (count - 1) / 2f) * 58f + Main.rand.NextFloat(-16f, 16f);
				DropStone(new Vector2(x, target.Center.Y - 300f - Main.rand.NextFloat(0f, 40f)), damage);
			}
		}

		private void DropStone(Vector2 pos, int damage)
		{
			// ai[2] tags the stone as this Eye's, so its effects can find it; Rockfall never reads it
			Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, Vector2.Zero,
				ModContent.ProjectileType<Rockfall>(), damage, 3f, Main.myPlayer, 0f, 0f, RockTag);
		}

		/// <summary>
		/// The bowl: it stops, spins up in place while a line runs out along the floor in the
		/// direction it chose on the first tick, then commits to that line at full speed. Jump it.
		/// A wall ends it in a crash that shakes three stones loose from the ceiling above.
		/// Twice in a row with the eye open, re-aimed in between.
		/// </summary>
		private void Bowl(Player target)
		{
			if (moveTimer == 0)
			{
				bowlDir = target.Center.X >= NPC.Center.X ? 1 : -1;
				SoundEngine.PlaySound(SoundID.Item22 with { Pitch = -0.6f }, NPC.Center);
				NPC.netUpdate = true;
			}
			moveTimer++;

			if (moveTimer <= BowlWind)
			{
				float t = moveTimer / (float)BowlWind;
				NPC.velocity.X *= 0.85f;
				NPC.rotation += bowlDir * (0.08f + 0.45f * t);
				if (!Main.dedServ && NPC.velocity.Y == 0f)
				{
					// spinning on the spot: grit sprays backwards off the floor, harder as it winds
					Grind(0.6f + 1.6f * t);
					if (Main.rand.NextFloat() < t * 0.5f)
						ParticleSystem.Spawn(new Spark(NPC.Bottom + new Vector2(-bowlDir * 12f, -2f),
							new Vector2(-bowlDir * Main.rand.NextFloat(2f, 5f), -Main.rand.NextFloat(0.5f, 2.5f)),
							Stone[0], 0.7f, Main.rand.Next(10, 16)));
				}
				if (moveTimer == BowlWind)
					SoundEngine.PlaySound(SoundID.Item70, NPC.Center);
				return;
			}

			if (moveTimer <= BowlWind + BowlRun)
			{
				NPC.velocity.X = bowlDir * (Phase == 0f ? 10.5f : 12.5f);
				NPC.rotation += NPC.velocity.X * 0.05f;
				if (!Main.dedServ && NPC.velocity.Y == 0f)
					Grind(2.2f);
				if (NPC.collideX && moveTimer > BowlWind + 2)
				{
					WallCrash();
					moveTimer = BowlWind + BowlRun;
				}
				return;
			}

			NPC.velocity.X *= 0.9f;
			NPC.rotation += NPC.velocity.X * 0.045f;
			if (moveTimer >= BowlWind + BowlRun + 30)
			{
				if (Phase == 1f && repeats == 0)
				{
					repeats = 1;
					moveTimer = 0;
				}
				else
					EndMove(24);
			}
		}

		private void WallCrash()
		{
			SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.4f }, NPC.Center);
			NPC.velocity = new Vector2(-bowlDir * 3f, -4f);
			if (!Main.dedServ)
			{
				Vector2 face = NPC.Center + new Vector2(bowlDir * 30f, 0f);
				HarvestFX.Rubble(face, Stone, 14, 5f, 1.1f, 2.5f);
				HarvestFX.MatteCrack(face, new Vector2(0f, -1f), 30f, Fissure, 60, 2f);
				HarvestFX.MatteCrack(face, new Vector2(0f, 1f), 22f, Fissure, 60, 2f);
				if (Main.LocalPlayer.Distance(NPC.Center) < 1000f)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitX * bowlDir,
						7f, 8f, 16, 1000f, "DormantEyeWall"));
			}
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			int damage = NPC.damage / 3;
			for (int i = 0; i < 3; i++)
				DropStone(NPC.Center + new Vector2(-bowlDir * (30f + i * 62f), -250f - i * 20f), damage);
		}

		/// <summary>
		/// The leap: the spot under the player is marked on the floor on the first tick, it
		/// crouches, then flies a fixed arc that lands exactly there and sends a tremor along
		/// the ground each way. Twice with the eye open.
		/// </summary>
		private void Leap(Player target)
		{
			int crouch = Phase == 0f ? 36 : 26;
			if (moveTimer == 0)
			{
				leapSpot = FloorUnder(target.Center);
				SoundEngine.PlaySound(SoundID.Item69 with { Pitch = -0.5f }, NPC.Center);
				NPC.netUpdate = true;
			}
			moveTimer++;

			if (moveTimer < crouch)
			{
				NPC.velocity.X *= 0.8f;
				NPC.rotation += NPC.velocity.X * 0.045f;
				if (!Main.dedServ && Main.rand.NextBool(3))
					Grind(0.8f);
				return;
			}

			if (moveTimer == crouch)
			{
				Launch();
				SoundEngine.PlaySound(SoundID.Item74 with { Pitch = -0.7f, Volume = 0.6f }, NPC.Center);
				if (!Main.dedServ)
					HarvestFX.MatteSlam(NPC.Bottom, Grit[1], 0.6f);
				leapPrevVY = NPC.velocity.Y;
				return;
			}

			// in flight on its own gravity, so the arc is the one the launch solved for
			NPC.noGravity = true;
			NPC.velocity.Y = Math.Min(NPC.velocity.Y + LeapGravity, 13f);
			NPC.rotation += NPC.velocity.X * 0.06f;

			bool landed = leapPrevVY > 0f && NPC.velocity.Y >= 0f && NPC.collideY;
			leapPrevVY = NPC.velocity.Y;
			if (landed || moveTimer > crouch + LeapFlightMax)
			{
				NPC.noGravity = false;
				Land();
				if (Phase == 1f && repeats == 0)
				{
					repeats = 1;
					moveTimer = 0;
					rest = 18;
				}
				else
					EndMove(30);
			}
		}

		private void Launch()
		{
			// how high it can go: the ceiling above, less room for the body
			float headroom = 240f;
			int tx = (int)(NPC.Center.X / 16f);
			int ty = (int)(NPC.position.Y / 16f);
			for (int i = 1; i < 16; i++)
			{
				if (WorldGen.InWorld(tx, ty - i, 2) && WorldGen.SolidTile(tx, ty - i))
				{
					headroom = (i - 1) * 16f;
					break;
				}
			}
			float rise = NPC.Bottom.Y - leapSpot.Y;          // positive when the spot is higher
			float apex = MathHelper.Clamp(Math.Max(140f, rise + 70f), 40f, Math.Max(40f, headroom));
			float vy = -MathF.Sqrt(2f * LeapGravity * apex);
			float up = -vy / LeapGravity;
			float drop = Math.Max(8f, apex - rise);
			float down = MathF.Sqrt(2f * drop / LeapGravity);
			float vx = MathHelper.Clamp((leapSpot.X - NPC.Center.X) / (up + down), -10f, 10f);
			NPC.velocity = new Vector2(vx, vy);
			NPC.noGravity = true;
		}

		private void Land()
		{
			SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.7f }, NPC.Center);
			NPC.velocity.X *= 0.2f;
			if (!Main.dedServ)
			{
				HarvestFX.MatteSlam(NPC.Bottom, Grit[0], 1.4f);
				HarvestFX.Rubble(NPC.Bottom - new Vector2(0f, 4f), Stone, 12, 4f, 1f, 3f);
				HarvestFX.MatteCrack(NPC.Bottom, new Vector2(1f, 0.1f), 40f, Fissure, 50, 2f);
				HarvestFX.MatteCrack(NPC.Bottom, new Vector2(-1f, 0.1f), 40f, Fissure, 50, 2f);
				if (Main.LocalPlayer.Distance(NPC.Center) < 1100f)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Bottom, Vector2.UnitY,
						8f, 8f, 18, 1100f, "DormantEyeLeap"));
			}
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			int damage = NPC.damage / 3;
			float speed = Phase == 0f ? 5f : 6f;
			for (int s = -1; s <= 1; s += 2)
				Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Bottom + new Vector2(s * 30f, -16f),
					new Vector2(s * speed, 0f), ModContent.ProjectileType<EyeTremor>(), damage, 2f, Main.myPlayer);
		}

		private static Vector2 FloorUnder(Vector2 at)
		{
			int tx = (int)(at.X / 16f);
			int ty = (int)(at.Y / 16f);
			for (int i = 0; i < 40; i++)
			{
				if (Standable(tx, ty + i))
					return new Vector2(at.X, (ty + i) * 16f);
			}
			return at + new Vector2(0f, 24f);
		}

		/// <summary>The beam out of the split lid: the Keeper's laser, thinner and slower.</summary>
		private void Beam(Player target, float spin, float offset)
		{
			SoundEngine.PlaySound(SoundID.Item33, NPC.Center);
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			float angle = (target.Center - NPC.Center).ToRotation() + offset;
			int damage = NPC.damage / 2;
			int p = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center,
				angle.ToRotationVector2(), ModContent.ProjectileType<RiftLaser>(),
				damage, 4f, Main.myPlayer, NPC.whoAmI, spin);
			if (p < Main.maxProjectiles)
			{
				// scale drives both the beam's width and how far it starts from the core
				Main.projectile[p].scale = 0.55f;
				Main.projectile[p].netUpdate = true;
			}
		}

		/// <summary>
		/// The glare: it stops dead and stares. The beam starts well wide of the player and
		/// sweeps across them at a fixed rate - it sweeps, it never tracks. Standing still is the
		/// window: the open eye is a target for the whole sweep.
		/// </summary>
		private void Glare(Player target)
		{
			NPC.velocity.X *= 0.85f;
			NPC.rotation += NPC.velocity.X * 0.045f;
			if (moveTimer == 0)
			{
				bowlDir = target.Center.X >= NPC.Center.X ? 1 : -1;
				SoundEngine.PlaySound(SoundID.Item15 with { Pitch = -0.5f }, NPC.Center);
				NPC.netUpdate = true;
			}
			if (moveTimer == GlareWind)
				Beam(target, -bowlDir * 0.0095f, bowlDir * 1.05f);
			if (++moveTimer >= GlareWind + 236)
				EndMove(20);
		}

		/// <summary>
		/// The volley: the eye flashes, then a flat fan of five shards leaves for where the player
		/// stood that instant. Three of them, a beat apart, while it keeps rolling. Lead the shot.
		/// </summary>
		private void Volley(Player target)
		{
			Roll(target, 0.45f);
			int beat = moveTimer % VolleyGap;
			if (beat == VolleyFlash)
			{
				SoundEngine.PlaySound(SoundID.Item8 with { Pitch = -0.3f }, NPC.Center);
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					Vector2 aim = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
					int damage = NPC.damage / 3;
					for (int i = -2; i <= 2; i++)
						Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + aim * 30f,
							aim.RotatedBy(i * 0.2f) * 4.6f, ModContent.ProjectileType<RiftShard>(), damage, 2f, Main.myPlayer);
				}
				if (!Main.dedServ)
					ParticleSystem.Spawn(new Bloom(NPC.Center, EyeLight, 0.3f, 1.1f, 14));
			}
			if (++moveTimer >= VolleyGap * 3)
				EndMove(24);
		}

		// ------------------------------------------------------------------ cosmetics (client only, never synced)
		//
		// T0 stone stays matte: dust, chips, grit and dark cracks, all lit by the world. The only
		// light in this fight is the eye - the crack leaking it, the beam it fires - so the one
		// bright thing on screen is the window the fight is about.

		private static readonly Color[] Stone =
			{ new(0xc9, 0xbd, 0xd1), new(0x93, 0x84, 0x9f), new(0x63, 0x54, 0x78), new(0x3f, 0x33, 0x57) };
		private static readonly Color[] Dense =
			{ new(0xba, 0xac, 0xc4), new(0x84, 0x76, 0x96), new(0x58, 0x4a, 0x6e), new(0x3b, 0x30, 0x50) };
		private static readonly Color[] Grit =
			{ new(0x93, 0x84, 0x9f), new(0x63, 0x54, 0x78), new(0x84, 0x76, 0x96) };
		private static readonly Color Fissure = new(0x1f, 0x19, 0x30);
		private static readonly Color EyeLight = new(210, 120, 255);

		private float RockTag => NPC.whoAmI + 1f;

		private float prevVY;
		private float[] crackDirs;

		private sealed class RockInfo
		{
			public Vector2 Pos;
			public bool Falling;
			public uint Seen;
			public Vector2 Ceiling;
			public bool HasCeiling;
		}

		private readonly Dictionary<int, RockInfo> rocks = new();
		private readonly List<int> gone = new();
		private uint stamp;

		private void Cosmetics()
		{
			// a heavy landing: the floor jolts, grit rings out both ways, chips skip off
			if (prevVY > 5f && NPC.velocity.Y == 0f)
			{
				float size = 0.6f + prevVY / 12f;
				HarvestFX.MatteSlam(NPC.Bottom, Grit[0], size);
				HarvestFX.Rubble(NPC.Bottom - new Vector2(0f, 4f), Stone, 6, 3f, 0.9f, 2.5f);
				if (Main.LocalPlayer.Distance(NPC.Center) < 900f)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Bottom, Vector2.UnitY,
						2f + prevVY * 0.3f, 8f, 12, 900f, "DormantEyeLand"));
			}
			else if (prevVY == 0f && NPC.velocity.Y < -6f)
				HarvestFX.MatteSlam(NPC.Bottom, Grit[1], 0.5f);
			prevVY = NPC.velocity.Y;

			// a client can learn of the break from a sync instead of from its own phase check
			if (Phase == 1f && crackDirs == null)
				CrackOpen();

			TrackRocks();

			// phase two: the beam gathers before it is even fired
			bool gathering = move == Move.Glare && moveTimer < GlareWind
				|| move == Move.Volley && moveTimer % VolleyGap < VolleyFlash;
			if (Phase == 1f && BreakTimer <= 0f && gathering && Main.rand.NextBool(2))
				ParticleSystem.Spawn(new Mote(NPC.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(90f, 170f),
					NPC.Center, EyeLight, Main.rand.NextFloat(0.8f, 1.2f), 50, 0.5f));

			Projectile laser = MyLaser();
			if (laser != null)
			{
				Vector2 dir = laser.rotation.ToRotationVector2();
				Vector2 muzzle = laser.Center + dir * 42f * laser.scale;
				if (laser.localAI[0] < RiftLaser.TelegraphTime)
				{
					// charge: motes converge on the muzzle from a ring, tighter as it nears firing
					float t = laser.localAI[0] / RiftLaser.TelegraphTime;
					if (Main.rand.NextFloat() < 0.5f + 0.5f * t)
						ParticleSystem.Spawn(new Mote(muzzle + Main.rand.NextVector2Unit() * MathHelper.Lerp(110f, 50f, t),
							muzzle, Main.rand.NextBool(3) ? Color.White : EyeLight, 0.9f, 36, 0.6f + t));
				}
				else if (laser.timeLeft > 10)
				{
					if (laser.localAI[0] == RiftLaser.TelegraphTime)
					{
						ParticleSystem.Spawn(new Bloom(muzzle, EyeLight, 0.4f, 1.6f, 18));
						ParticleSystem.Spawn(new Shockwave(muzzle, EyeLight * 0.8f, 0.1f, 1.1f, 20));
					}
					// sparks spat out of the muzzle along the beam
					if (Main.rand.NextBool(2))
						ParticleSystem.Spawn(new Spark(muzzle, dir.RotatedByRandom(0.6f) * Main.rand.NextFloat(4f, 9f),
							Main.rand.NextBool(3) ? Color.White : EyeLight, 0.9f, Main.rand.Next(10, 18)));
				}
			}

			// the open eye: a soft violet light from the crack
			if (Phase == 1f)
				Lighting.AddLight(NPC.Center, 0.35f, 0.15f, 0.5f);
		}

		private Projectile MyLaser()
		{
			int type = ModContent.ProjectileType<RiftLaser>();
			Projectile best = null;
			foreach (Projectile p in Main.ActiveProjectiles)
			{
				if (p.type == type && (int)p.ai[0] == NPC.whoAmI && (best == null || p.timeLeft > best.timeLeft))
					best = p;
			}
			return best;
		}

		/// <summary>
		/// The rockfall's dressing, without touching Rockfall (the Heave drops the same stone). Each
		/// stone this Eye dropped is followed by identity: the ceiling above it cracks and sifts grit
		/// and pebbles during its 40 harmless ticks, and the tick it is gone it lands in chips.
		/// </summary>
		private void TrackRocks()
		{
			stamp++;
			int type = ModContent.ProjectileType<Rockfall>();
			foreach (Projectile p in Main.ActiveProjectiles)
			{
				if (p.type != type || p.ai[2] != RockTag)
					continue;
				if (!rocks.TryGetValue(p.identity, out RockInfo r))
				{
					r = new RockInfo();
					rocks[p.identity] = r;
					FindCeiling(p.Center, r);
					Vector2 top = r.HasCeiling ? r.Ceiling : p.Center;
					HarvestFX.MatteCrack(top, new Vector2(1f, 0.2f), 26f, Fissure, Projectiles.Rockfall.Telegraph + 20, 2f);
					HarvestFX.MatteCrack(top, new Vector2(-1f, 0.2f), 26f, Fissure, Projectiles.Rockfall.Telegraph + 20, 2f);
				}
				r.Seen = stamp;
				r.Pos = p.Center;
				r.Falling = p.ai[0] >= Projectiles.Rockfall.Telegraph;

				if (!r.Falling)
				{
					Vector2 src = r.HasCeiling ? r.Ceiling : p.Center;
					if (Main.rand.NextBool(3))
						ParticleSystem.Spawn(new Smoke(src + new Vector2(Main.rand.NextFloat(-12f, 12f), 2f),
							new Vector2(0f, Main.rand.NextFloat(0.6f, 1.6f)), Grit[Main.rand.Next(Grit.Length)],
							Main.rand.NextFloat(0.1f, 0.2f), Main.rand.Next(30, 50)));
					if (Main.rand.NextBool(7))
						ParticleSystem.Spawn(new Chip(src + new Vector2(Main.rand.NextFloat(-8f, 8f), 2f),
							new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), 0.5f), Stone[Main.rand.Next(Stone.Length)],
							Main.rand.NextFloat(0.6f, 1f), 60));
				}
				else if (Main.rand.NextBool(2))
					ParticleSystem.Spawn(new Smoke(p.Center - p.velocity * 0.5f, -p.velocity * 0.05f,
						Grit[Main.rand.Next(Grit.Length)], Main.rand.NextFloat(0.12f, 0.2f), 24));
			}

			gone.Clear();
			foreach (var kv in rocks)
				if (kv.Value.Seen != stamp)
					gone.Add(kv.Key);
			foreach (int id in gone)
			{
				RockInfo r = rocks[id];
				rocks.Remove(id);
				if (!r.Falling)
					continue;
				Vector2 floor = r.Pos + new Vector2(0f, 7f);
				HarvestFX.Rubble(r.Pos, Stone, 8, 3.5f, 1f, 2.5f);
				HarvestFX.MatteSlam(floor, Grit[0], 0.7f);
				HarvestFX.MatteCrack(floor, new Vector2(1f, 0.15f), 18f, Fissure, 40, 2f);
				HarvestFX.MatteCrack(floor, new Vector2(-1f, 0.15f), 18f, Fissure, 40, 2f);
			}
		}

		private static void FindCeiling(Vector2 from, RockInfo r)
		{
			int tx = (int)(from.X / 16f);
			int ty = (int)(from.Y / 16f);
			for (int i = 0; i < 25; i++)
			{
				int y = ty - i;
				if (!WorldGen.InWorld(tx, y, 2))
					break;
				if (WorldGen.SolidTile(tx, y))
				{
					r.HasCeiling = true;
					r.Ceiling = new Vector2(from.X, (y + 1) * 16f);
					return;
				}
			}
		}

		/// <summary>
		/// The eye opening: the shell splits along five cracks and light leaks out of every one.
		/// The cracks are world-space and the boulder is held still for the whole break, so they
		/// stay laid across the sprite while the lid comes apart.
		/// </summary>
		private void CrackOpen()
		{
			Vector2 c = NPC.Center;
			crackDirs = new float[5];
			float a0 = Main.rand.NextFloat(MathHelper.TwoPi);
			for (int i = 0; i < crackDirs.Length; i++)
			{
				crackDirs[i] = a0 + MathHelper.TwoPi * i / crackDirs.Length + Main.rand.NextFloat(-0.35f, 0.35f);
				Vector2 d = crackDirs[i].ToRotationVector2();
				FX.Crack(c + d * 4f, d, Main.rand.NextFloat(34f, 46f), EyeLight, BreakDuration, 2f, 2);
			}
			ParticleSystem.Spawn(new Bloom(c, EyeLight, 0.3f, 1.4f, 30));
			HarvestFX.Rubble(c, Dense, 22, 6f, 1.3f, 3f);
			HarvestFX.Rubble(c, Stone, 12, 4f, 1f, 2f);
		}

		private void BreakCosmetics()
		{
			Vector2 c = NPC.Center;
			if (crackDirs == null)
				return;
			float f = 1f - BreakTimer / BreakDuration;
			// the cracks keep running as the lid gives
			if ((int)BreakTimer % 15 == 0)
			{
				float a = crackDirs[Main.rand.Next(crackDirs.Length)] + Main.rand.NextFloat(-0.3f, 0.3f);
				Vector2 d = a.ToRotationVector2();
				FX.Crack(c + d * 10f, d, Main.rand.NextFloat(18f, 28f), EyeLight, 40, 1.5f, 1);
			}
			// flakes of shell falling off the edge
			if (Main.rand.NextBool(3))
			{
				Vector2 d = Main.rand.NextVector2Unit();
				ParticleSystem.Spawn(new Chip(c + d * 28f, d * Main.rand.NextFloat(0.5f, 2f), Dense[Main.rand.Next(Dense.Length)],
					Main.rand.NextFloat(0.8f, 1.5f), 60));
			}
			Lighting.AddLight(c, 0.4f + 0.5f * f, 0.15f + 0.2f * f, 0.6f + 0.5f * f);
		}

		private void DeathCosmetics()
		{
			if (Main.dedServ)
				return;
			float f = death.Progress;
			Vector2 c = NPC.Center;
			// more and more light breaking out through new cracks as the stone gives up
			int every = Math.Max(5, 16 - (int)(f * 11f));
			if (Main.GameUpdateCount % every == 0)
			{
				Vector2 d = Main.rand.NextVector2Unit();
				FX.Crack(c + d * 4f, d, Main.rand.NextFloat(26f, 42f), EyeLight, 40, 2f, 2);
			}
			if (Main.rand.NextFloat() < 0.3f + f * 0.5f)
			{
				Vector2 d = Main.rand.NextVector2Unit();
				ParticleSystem.Spawn(new Chip(c + d * 28f, d * Main.rand.NextFloat(1f, 3f), Stone[Main.rand.Next(Stone.Length)],
					Main.rand.NextFloat(0.8f, 1.6f), 60));
			}
		}

		private void DeathBurst(Vector2 c)
		{
			if (Main.dedServ)
				return;
			HarvestFX.Rubble(c, Stone, 30, 8f, 1.6f, 4f);
			HarvestFX.Rubble(c, Dense, 20, 6f, 1.3f, 3f);
			HarvestFX.MatteSlam(c + new Vector2(0f, 30f), Grit[0], 1.6f);
			ParticleSystem.Spawn(new Bloom(c, EyeLight, 0.6f, 2.6f, 36));
			ParticleSystem.Spawn(new Shockwave(c, EyeLight, 0.2f, 2.4f, 34));
			FX.Sparks(c, EyeLight, 18, 8f, 1.1f, 26);
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			if (NPC.IsABestiaryIconDummy)
				return true;

			// rock shadows: a dark pool on the floor under each stone, growing and darkening as it
			// comes. Drawn beneath the stone's own landing ring, which stays the primary marker.
			int type = ModContent.ProjectileType<Rockfall>();
			Texture2D flare = ParticleSystem.Flare;
			foreach (Projectile p in Main.ActiveProjectiles)
			{
				if (p.type != type || p.ai[2] != RockTag)
					continue;
				float ground = p.ai[1];
				float t = p.ai[0] < Projectiles.Rockfall.Telegraph
					? p.ai[0] / Projectiles.Rockfall.Telegraph * 0.6f
					: 0.6f + 0.4f * MathHelper.Clamp(1f - (ground - p.Center.Y) / 320f, 0f, 1f);
				float s = MathHelper.Lerp(0.22f, 0.5f, t);
				spriteBatch.Draw(flare, new Vector2(p.Center.X, ground) - screenPos, null, new Color(8, 5, 14) * (0.25f + 0.45f * t),
					0f, flare.Size() * 0.5f, new Vector2(s, s * 0.28f), SpriteEffects.None, 0f);
			}
			DrawTells(spriteBatch, screenPos);
			return true;
		}

		/// <summary>
		/// The tells, drawn under the boulder. Matte stone colours for the stone's own moves - the
		/// only light in this fight is the eye - and eye-violet only for what the eye does.
		/// </summary>
		private void DrawTells(SpriteBatch spriteBatch, Vector2 screenPos)
		{
			if (rest > 0 || BreakTimer > 0f)
				return;
			Texture2D flare = ParticleSystem.Flare;
			float time = Main.GlobalTimeWrappedHourly;

			if (move == Move.Bowl && moveTimer <= BowlWind)
			{
				// the bowl's line: dashes along the floor out to the first wall, crawling the way it
				// will go, brighter and denser as the spin winds up
				float t = moveTimer / (float)BowlWind;
				float y = NPC.Bottom.Y - 3f;
				float reach = WallDistance(bowlDir, 860f);
				float crawl = time * 260f % 28f;
				for (float x = 34f + crawl; x < reach; x += 28f)
				{
					float fade = 1f - x / (reach + 40f);
					Vector2 a = new(NPC.Center.X + bowlDir * x, y);
					RiftDraw.Line(a, a + new Vector2(bowlDir * 14f, 0f), new Color(0xc9, 0xbd, 0xd1) * ((0.25f + 0.55f * t) * fade), 2f);
				}
				// chevrons right in front of it, pointing the way
				for (int k = 0; k < 3; k++)
				{
					Vector2 tip = new(NPC.Center.X + bowlDir * (44f + k * 10f), NPC.Center.Y);
					float a = (0.2f + 0.6f * t) * (k == (int)(time * 8f) % 3 ? 1f : 0.5f);
					RiftDraw.Line(tip, tip + new Vector2(-bowlDir * 6f, -6f), new Color(0xf4, 0xef, 0xe6) * a, 2f);
					RiftDraw.Line(tip, tip + new Vector2(-bowlDir * 6f, 6f), new Color(0xf4, 0xef, 0xe6) * a, 2f);
				}
			}

			if (move == Move.Leap && leapSpot != Vector2.Zero)
			{
				// the landing: a dark pool on the floor that grows as it comes, and a ring closing on it
				int crouch = Phase == 0f ? 36 : 26;
				float t = MathHelper.Clamp(moveTimer / (float)(crouch + 40), 0f, 1f);
				spriteBatch.Draw(flare, leapSpot - screenPos, null, new Color(8, 5, 14) * (0.3f + 0.4f * t), 0f,
					flare.Size() * 0.5f, new Vector2(0.9f, 0.22f) * (0.5f + 0.5f * t), SpriteEffects.None, 0f);
				Texture2D ring = ModContent.Request<Texture2D>("Terrapex/Content/Projectiles/RiftRing").Value;
				spriteBatch.Draw(ring, leapSpot - screenPos, null, new Color(0xc9, 0xbd, 0xd1) * (0.35f + 0.4f * t), 0f,
					ring.Size() * 0.5f, new Vector2(1f, 0.35f) * MathHelper.Lerp(0.9f, 0.35f, t), SpriteEffects.None, 0f);
				// where the tremors will run: short ticks out each side
				for (int sgn = -1; sgn <= 1; sgn += 2)
					for (int k = 1; k <= 4; k++)
					{
						Vector2 a = leapSpot + new Vector2(sgn * (28f + k * 22f), -2f);
						RiftDraw.Line(a, a + new Vector2(sgn * 10f, 0f), new Color(0x93, 0x84, 0x9f) * (0.5f * t * (1f - k / 5f)), 2f);
					}
			}

			bool flash = move == Move.Volley && moveTimer % VolleyGap < VolleyFlash
				|| move == Move.Glare && moveTimer < GlareWind;
			if (flash)
			{
				// the eye flash: the tell for anything the eye fires
				float f = move == Move.Glare ? moveTimer / (float)GlareWind : moveTimer % VolleyGap / (float)VolleyFlash;
				Vector2 c = NPC.Center - screenPos;
				spriteBatch.Draw(flare, c, null, new Color(EyeLight.R, EyeLight.G, EyeLight.B, 0) * (0.25f + 0.5f * f), 0f,
					flare.Size() * 0.5f, 0.5f + 0.5f * f, SpriteEffects.None, 0f);
				spriteBatch.Draw(flare, c, null, new Color(255, 240, 255, 0) * (0.6f * f), time * 3f,
					flare.Size() * 0.5f, new Vector2(1.2f, 0.12f) * f, SpriteEffects.None, 0f);
				spriteBatch.Draw(flare, c, null, new Color(255, 240, 255, 0) * (0.6f * f), time * 3f + MathHelper.PiOver2,
					flare.Size() * 0.5f, new Vector2(1.2f, 0.12f) * f, SpriteEffects.None, 0f);
			}
		}

		private float WallDistance(int dir, float max)
		{
			int ty = (int)((NPC.Bottom.Y - 24f) / 16f);
			for (float x = 32f; x < max; x += 16f)
			{
				int tx = (int)((NPC.Center.X + dir * x) / 16f);
				if (WorldGen.InWorld(tx, ty, 2) && WorldGen.SolidTile(tx, ty))
					return x - 8f;
			}
			return max;
		}

		public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			if (NPC.IsABestiaryIconDummy)
				return;
			Texture2D flare = ParticleSystem.Flare;
			Texture2D px = TextureAssets.MagicPixel.Value;
			Vector2 c = NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY);
			float time = Main.GlobalTimeWrappedHourly;

			// light leaking out of the cracks while the lid splits
			if (BreakTimer > 0f && crackDirs != null)
			{
				float f = 1f - BreakTimer / BreakDuration;
				foreach (float a in crackDirs)
				{
					float len = 20f + 70f * f + 6f * MathF.Sin(time * 9f + a * 3f);
					float rot = a + 0.05f * MathF.Sin(time * 4f + a);
					spriteBatch.Draw(px, c, new Rectangle(0, 0, 1, 1), new Color(150, 70, 230, 0) * (0.35f * (0.4f + f)),
						rot, new Vector2(0f, 0.5f), new Vector2(len, 9f * (0.4f + f)), SpriteEffects.None, 0f);
					spriteBatch.Draw(px, c, new Rectangle(0, 0, 1, 1), new Color(255, 230, 255, 0) * (0.5f * (0.4f + f)),
						rot, new Vector2(0f, 0.5f), new Vector2(len * 0.6f, 2f), SpriteEffects.None, 0f);
				}
				spriteBatch.Draw(flare, c, null, new Color(EyeLight.R, EyeLight.G, EyeLight.B, 0) * (0.3f + 0.5f * f), 0f,
					flare.Size() * 0.5f, 0.6f + 0.6f * f, SpriteEffects.None, 0f);
			}
			else if (Phase == 1f)
			{
				// the open eye glows - the only lit thing on the boulder
				float pulse = 0.8f + 0.2f * MathF.Sin(time * 3f);
				spriteBatch.Draw(flare, c, null, new Color(EyeLight.R, EyeLight.G, EyeLight.B, 0) * (0.25f * pulse), 0f,
					flare.Size() * 0.5f, 0.7f * pulse, SpriteEffects.None, 0f);
			}

			// the beam's muzzle: a churning bloom of three lobes turning against each other
			Projectile laser = MyLaser();
			if (laser != null && laser.localAI[0] >= RiftLaser.TelegraphTime)
			{
				float fade = MathHelper.Clamp((laser.timeLeft - 8f) / 14f, 0f, 1f);
				Vector2 muzzle = laser.Center + laser.rotation.ToRotationVector2() * 42f * laser.scale - screenPos;
				for (int k = 0; k < 3; k++)
				{
					float spin = time * (k % 2 == 0 ? 5f : -4f) + k * 2.1f;
					float sw = 0.55f + 0.12f * MathF.Sin(time * 11f + k * 1.7f);
					spriteBatch.Draw(flare, muzzle, null, new Color(170, 80, 255, 0) * (0.35f * fade), spin,
						flare.Size() * 0.5f, new Vector2(sw, sw * 0.45f), SpriteEffects.None, 0f);
				}
				spriteBatch.Draw(flare, muzzle, null, new Color(255, 235, 255, 0) * (0.5f * fade), 0f,
					flare.Size() * 0.5f, 0.3f + 0.05f * MathF.Sin(time * 17f), SpriteEffects.None, 0f);
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
			// chips knocked off the face it was hit on
			for (int i = 0; i < 2; i++)
				ParticleSystem.Spawn(new Chip(NPC.Center + new Vector2(-hit.HitDirection * 26f, Main.rand.NextFloat(-18f, 18f)),
					new Vector2(-hit.HitDirection * Main.rand.NextFloat(1f, 3f), -Main.rand.NextFloat(1f, 3f)),
					Stone[Main.rand.Next(Stone.Length)], Main.rand.NextFloat(0.8f, 1.3f), 50));
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
				ModContent.ItemType<SleepersRod>(),
				ModContent.ItemType<Lidcutter>()));
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
