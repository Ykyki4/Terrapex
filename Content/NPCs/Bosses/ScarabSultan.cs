using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terrapex.Content.Items.Accessories.Reaper;
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
using Terrapex.Common.Particles;
using Terrapex.Common.Systems;
using Terrapex.Content.Items.Accessories;
using Terrapex.Content.Items.Armor;
using Terrapex.Content.Items.Consumables;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Items.Weapons.Scarab;
using Terrapex.Content.Projectiles.Scarab;

namespace Terrapex.Content.NPCs.Bosses
{
	/// <summary>
	/// The Scarab Sultan - the desert's boss, after the Eater of Worlds / Brain of Cthulhu. See
	/// <c>BOSS_SCARAB.md</c>; the numbers there and here are kept in step.
	///
	/// A golden scarab king that flies over the dunes with its sun held between its legs. The sun
	/// is the fight: kicked, it falls, bounces and rolls by <see cref="BallPhysics"/>, and before
	/// every kick the Sultan draws the ball's whole path on the floor - the same arithmetic the
	/// ball then runs, so the dotted line is a promise. In phase two the sun cracks: smaller, and
	/// at the end of its fuse it bursts along eight spokes it draws first.
	///
	/// ScarabSultan.png is four frames of the wings beating, stacked. The sun is its own texture
	/// (<see cref="SunBall"/>), drawn in the Sultan's legs whenever it is not rolling.
	/// </summary>
	[AutoloadBossHead]
	public class ScarabSultan : ModNPC
	{
		public const float PhaseTwoAt = 0.5f;

		public const int BallDamage = 26;
		public const int BeetlingDamage = 15;
		public const int PillarDamage = 20;

		public const float KickSpeedOne = 7.5f, KickSpeedTwo = 9f;
		public const int KickTell = 40;
		public const int DiveTell = 40, DiveDash = 26, DiveRecover = 30;
		public const float DiveSpeed = 17f;
		public const int FanTell = 24;
		public const int PillarSpacing = 112;

		private const int Kick = 0, Fan = 1, Dive = 2, Pillars = 3;
		private static readonly int[] RotationOne = { Kick, Fan, Dive, Pillars, Kick, Dive };
		private static readonly int[] RotationTwo = { Kick, Dive, Dive, Pillars, Fan, Kick, Pillars, Dive };

		private ref float Timer => ref NPC.ai[0];
		private ref float Step => ref NPC.ai[1];
		private ref float Phase => ref NPC.ai[2];
		private ref float Side => ref NPC.ai[3];

		/// <summary>A dive's committed angle, or a kick's direction (+1/-1). Decided by the server, synced.</summary>
		private float aimAngle;

		private readonly BossDeath death = new();

		// ---- cosmetic state, client-side, never synced

		/// <summary>The phase this client last saw - the roar's effects key off the change, not off
		/// <see cref="EnterPhaseTwo"/>, which a multiplayer client may never run itself.</summary>
		private float seenPhase = -1f;

		/// <summary>Afterimages of the dive: where the body was, and how long each echo has left.</summary>
		private const int Ghosts = 8, GhostLife = 14;
		private readonly Vector2[] ghostAt = new Vector2[Ghosts];
		private readonly float[] ghostRot = new float[Ghosts];
		private readonly int[] ghostLife = new int[Ghosts];
		private int ghostNext;

		private int[] Rotation => Phase == 0f ? RotationOne : RotationTwo;
		private int Move => Rotation[(int)Step % Rotation.Length];
		private int BallKind => Phase == 0f ? SunBall.Sun : SunBall.Cracked;

		/// <summary>Where the sun sits in the Sultan's legs, and where a kick launches it from.</summary>
		private Vector2 HeldBall => NPC.Center + new Vector2(8f * NPC.spriteDirection, 42f);

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 4;
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			NPCID.Sets.BossBestiaryPriority.Add(Type);
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
		}

		public override void SetDefaults()
		{
			NPC.width = 96;
			NPC.height = 64;
			NPC.damage = 30;
			NPC.defense = 14;
			NPC.lifeMax = 4600;
			NPC.HitSound = SoundID.NPCHit38;
			NPC.DeathSound = SoundID.NPCDeath41;
			NPC.knockBackResist = 0f;
			NPC.value = Item.buyPrice(gold: 4);
			NPC.aiStyle = -1;
			NPC.boss = true;
			NPC.npcSlots = 8f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			Music = MusicID.Boss1;
		}

		public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
		{
			NPC.lifeMax = (int)(NPC.lifeMax * 0.7f * balance);
			NPC.damage = (int)(NPC.damage * 0.85f);
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
			=> bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.DayTime,
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Desert,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.ScarabSultan.Bestiary")
			});

		public override void SendExtraAI(BinaryWriter writer) => writer.Write(aimAngle);

		public override void ReceiveExtraAI(BinaryReader reader) => aimAngle = reader.ReadSingle();

		// ------------------------------------------------------------------ AI

		public override void AI()
		{
			if (death.Dying)
			{
				Vector2 at = NPC.Center;
				float p = death.Progress;
				if (death.Tick(NPC, DustID.GoldFlame, new Color(255, 200, 90)))
					DeathBurstFX(at);
				else
					DyingFX(p);
				return;
			}

			// the roar's effects follow the phase this client sees, so every client gets them
			if (seenPhase < 0f)
				seenPhase = Phase;
			else if (seenPhase != Phase)
			{
				seenPhase = Phase;
				if (Phase == 1f)
					PhaseTwoFX();
			}

			NPC.TargetClosest();
			Player target = Main.player[NPC.target];
			if (!target.active || target.dead || !Main.dayTime)
			{
				// a creature of the sun: at dusk, or with nobody left, it climbs away
				NPC.velocity.Y -= 0.3f;
				NPC.EncourageDespawn(20);
				return;
			}

			if (Phase == 0f && NPC.life <= NPC.lifeMax * PhaseTwoAt)
				EnterPhaseTwo();

			// facing: locked to the committed direction while a kick or a dive is being made
			int face = target.Center.X >= NPC.Center.X ? 1 : -1;
			if (Timer > 0f && Move == Kick)
				face = aimAngle >= 0f ? 1 : -1;
			else if (Timer > 0f && Move == Dive)
				face = MathF.Cos(aimAngle) >= 0f ? 1 : -1;
			NPC.spriteDirection = NPC.direction = face;

			Timer++;
			NPC.rotation = MathHelper.Lerp(NPC.rotation, NPC.velocity.X * 0.025f, 0.15f);
			HeldSunFX();

			if (Timer <= 0f)
			{
				Hover(target, 0.06f);
				return;
			}

			switch (Move)
			{
				case Kick: DoKick(target); break;
				case Fan: DoFan(target); break;
				case Dive: DoDive(target); break;
				case Pillars: DoPillars(target); break;
			}
		}

		private void Hover(Player target, float pull)
		{
			if (Side == 0f)
				Side = 1f;
			Vector2 want = target.Center + new Vector2(Side * 300f, -220f);
			Vector2 toward = (want - NPC.Center) * 0.05f;
			if (toward.Length() > 10f)
				toward = Vector2.Normalize(toward) * 10f;
			NPC.velocity = Vector2.Lerp(NPC.velocity, toward, pull);
		}

		/// <summary>
		/// The pause before the next move. Outside the desert the Sultan stops pausing almost at all:
		/// the fight is meant to be had on its sand, and leaving it is answered, not prevented.
		/// </summary>
		private void NextMove(Player target)
		{
			int pause = Phase == 0f ? 40 : 24;
			if (!target.ZoneDesert)
				pause = 10;
			Timer = -pause;
			Step++;
			Side = -Side;
			NPC.netUpdate = true;
		}

		private void EnterPhaseTwo()
		{
			Phase = 1f;
			Timer = -40f;
			Step = 0f;
			NPC.netUpdate = true;
			SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
			// the sun in its legs cracks - the effects are in PhaseTwoFX, run by every client on the change
			if (Main.netMode != NetmodeID.MultiplayerClient)
				ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Mods.Terrapex.Chat.ScarabPhase2"), new Color(255, 200, 90));
		}

		private static int ActiveBalls()
		{
			int type = ModContent.ProjectileType<SunBall>();
			int n = 0;
			foreach (Projectile p in Main.ActiveProjectiles)
			{
				if (p.type == type && p.hostile && (int)p.ai[0] != SunBall.Dung)
					n++;
			}
			return n;
		}

		private Vector2 KickVelocity => new(aimAngle * (Phase == 0f ? KickSpeedOne : KickSpeedTwo), 0f);

		private void DoKick(Player target)
		{
			if (Timer == 1f && Main.netMode != NetmodeID.MultiplayerClient)
			{
				aimAngle = target.Center.X >= NPC.Center.X ? 1f : -1f;
				NPC.netUpdate = true;
			}
			// the tell: it slows to a near stop and draws the ball's path (see PreDraw)
			if (Timer <= KickTell)
			{
				NPC.velocity *= 0.88f;
				// heat drawn into the sun as the kick winds up
				if (!Main.dedServ && Main.rand.NextFloat() < 0.3f + 0.6f * Timer / KickTell)
				{
					Vector2 dir = Main.rand.NextVector2Unit();
					ParticleSystem.Spawn(new Spark(HeldBall + dir * Main.rand.NextFloat(46f, 70f), -dir * Main.rand.NextFloat(2.5f, 4f),
						SunPalette.Gold, 0.8f, 14));
				}
				return;
			}
			if (Timer == KickTell + 1)
			{
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.6f, Volume = 1.2f }, NPC.Center);
				if (!Main.dedServ)
				{
					// the kick lands: a hot flash at the foot, a ring, sparks thrown along the kick
					FX.Impact(HeldBall, SunPalette.Pale, SunPalette.Gold, 0.8f);
					FX.Sparks(HeldBall, SunPalette.Gold, 12, 9f, 1f, 20, 0.1f, 0.9f, aimAngle >= 0f ? 0f : MathHelper.Pi);
					ParticleSystem.Spawn(new SunGlint(HeldBall, SunPalette.Gold, 60f, 16, 0f, 0.02f));
				}
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					Vector2 v = KickVelocity;
					Projectile.NewProjectile(NPC.GetSource_FromAI(), HeldBall, Vector2.Zero, ModContent.ProjectileType<SunBall>(),
						NPC.GetAttackDamage_ForProjectiles(BallDamage, BallDamage), 0f, Main.myPlayer, BallKind, v.X, v.Y);
				}
				NPC.velocity.X -= aimAngle * 3f;
			}
			Hover(target, 0.04f);
			if (Timer >= KickTell + 36)
				NextMove(target);
		}

		private void DoFan(Player target)
		{
			Hover(target, 0.05f);
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				if (Timer == FanTell)
					ThrowFan(target, Phase == 0f ? 5 : 7, 0f);
				// phase two answers the first fan with a second, its gaps where the first one's beetles were
				if (Phase == 1f && Timer == FanTell + 30)
					ThrowFan(target, 6, 0.5f);
			}
			if (Timer == FanTell || (Phase == 1f && Timer == FanTell + 30))
			{
				SoundEngine.PlaySound(SoundID.Item97 with { Pitch = 0.2f }, NPC.Center);
				if (!Main.dedServ)
				{
					// the gleam peaks as the fan leaves: a star off the jewel, a gold burst at the throw
					ParticleSystem.Spawn(new SunGlint(Jewel, SunPalette.Lapis, 70f, 20, MathHelper.PiOver4 * 0.3f, 0.03f));
					ParticleSystem.Spawn(new Bloom(Jewel, SunPalette.Lapis, 0.3f, 1.1f, 16));
					Vector2 toward = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
					FX.Sparks(NPC.Center, SunPalette.Gold, 14, 7f, 1f, 18, 0f, 1.2f, toward.ToRotation());
				}
			}
			if (Timer >= (Phase == 0f ? 70 : 96))
				NextMove(target);
		}

		private void ThrowFan(Player target, int count, float offset)
		{
			Vector2 at = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
			int damage = NPC.GetAttackDamage_ForProjectiles(BeetlingDamage, BeetlingDamage);
			const float spread = 1.0f;
			for (int i = 0; i < count; i++)
			{
				float t = (i + offset) / (count - 1f) - 0.5f;
				Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, at.RotatedBy(t * spread) * 5f,
					ModContent.ProjectileType<Beetling>(), damage, 0f, Main.myPlayer);
			}
		}

		private void DoDive(Player target)
		{
			int tell = DiveTellNow;
			if (Timer == 1f && Main.netMode != NetmodeID.MultiplayerClient)
			{
				aimAngle = (target.Center - NPC.Center).ToRotation();
				NPC.netUpdate = true;
			}
			if (Timer <= tell)
			{
				// it rears back against the line before it goes
				NPC.velocity = Vector2.Lerp(NPC.velocity, -aimAngle.ToRotationVector2() * 2f, 0.1f);
				// heat shimmer gathering along the line behind it
				if (!Main.dedServ && Main.rand.NextFloat() < Timer / tell * 0.8f)
				{
					Vector2 d = aimAngle.ToRotationVector2();
					ParticleSystem.Spawn(new SunEmber(NPC.Center + d * Main.rand.NextFloat(60f, 500f) + d.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-6f, 6f),
						d * Main.rand.NextFloat(0.5f, 1.5f), 0.7f, 26, 0.3f) { Lift = 0.01f });
				}
				return;
			}
			if (Timer <= tell + DiveDash)
			{
				Vector2 dir = aimAngle.ToRotationVector2();
				if (Timer == tell + 1)
				{
					SoundEngine.PlaySound(SoundID.Item74 with { Pitch = -0.3f, Volume = 0.9f }, NPC.Center);
					if (!Main.dedServ)
					{
						ParticleSystem.Spawn(new Bloom(NPC.Center, SunPalette.Pale, 0.6f, 2f, 16));
						ParticleSystem.Spawn(new Shockwave(NPC.Center, SunPalette.Gold, 0.2f, 1.6f, 20));
						ParticleSystem.Spawn(new SunGlint(NPC.Center + dir * 50f, SunPalette.Gold, 90f, 14, aimAngle));
					}
				}
				NPC.velocity = dir * DiveSpeed;
				if (Main.rand.NextBool(3))
					Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.GoldFlame, 0f, 0f, 80, default, 1.2f).noGravity = true;
				DiveWakeFX(dir);
				return;
			}
			if (Timer == tell + DiveDash + 1 && !Main.dedServ)
			{
				// it pulls up: a burst of dust thrown off the stop
				Vector2 dir = aimAngle.ToRotationVector2();
				ParticleSystem.Spawn(new Shockwave(NPC.Center, SunPalette.Sand, 0.3f, 2.2f, 26));
				for (int i = 0; i < 16; i++)
				{
					Vector2 v = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 6f) + dir * 2f;
					ParticleSystem.Spawn(new Smoke(NPC.Center + Main.rand.NextVector2Circular(30f, 20f), v, SunPalette.Sand * 0.7f,
						Main.rand.NextFloat(0.35f, 0.6f), Main.rand.Next(34, 50)));
				}
				FX.Sparks(NPC.Center + dir * 30f, SunPalette.Gold, 12, 7f, 1f, 20, 0.12f, 1.6f, aimAngle);
			}
			NPC.velocity *= 0.9f;
			if (Timer >= tell + DiveDash + DiveRecover)
				NextMove(target);
		}

		/// <summary>A second dive in a row (phase two) commits faster: the line is the same, the wait shorter.</summary>
		private int DiveTellNow
		{
			get
			{
				bool chained = Phase == 1f && Rotation[((int)Step + Rotation.Length - 1) % Rotation.Length] == Dive;
				return chained ? 30 : DiveTell;
			}
		}

		private void DoPillars(Player target)
		{
			Hover(target, 0.05f);
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				if (Timer == 1f)
					RaisePillars(target, Phase == 0f ? 3 : 5, 0f);
				// phase two: a second row a beat later, in the gaps of the first
				if (Phase == 1f && Timer == 40f)
					RaisePillars(target, 4, 0.5f);
			}
			int last = Phase == 0f ? 1 : 40;
			if (Timer >= last + SandPillar.Tell + SandPillar.Rise + SandPillar.Hold)
				NextMove(target);
		}

		/// <summary>A row of pillars centred on where the player stands, each on its own patch of floor.</summary>
		private void RaisePillars(Player target, int count, float offset)
		{
			int damage = NPC.GetAttackDamage_ForProjectiles(PillarDamage, PillarDamage);
			for (int i = 0; i < count; i++)
			{
				float k = i - (count - 1) * 0.5f + offset;
				Vector2 probe = new(target.Center.X + k * PillarSpacing, target.Center.Y - 120f);
				Vector2 floor = SandPillar.FindGround(probe);
				Projectile.NewProjectile(NPC.GetSource_FromAI(), floor, Vector2.Zero, ModContent.ProjectileType<SandPillar>(),
					damage, 0f, Main.myPlayer);
			}
		}

		// ------------------------------------------------------------------ effects (cosmetic, every client)

		/// <summary>The lapis jewel on its crown - the fan's tell.</summary>
		private Vector2 Jewel => NPC.Center + new Vector2(27f * NPC.spriteDirection, -11f).RotatedBy(NPC.rotation);

		/// <summary>The sun in its legs sheds heat - and, cracked, sparks - whenever it is held.</summary>
		private void HeldSunFX()
		{
			if (Main.dedServ || (Main.GameUpdateCount & 1) != 0 || ActiveBalls() > 0)
				return; // only while the sun is actually in its legs
			Vector2 off = Main.rand.NextVector2Unit() * Main.rand.NextFloat(8f, 18f);
			off.Y = -Math.Abs(off.Y);
			ParticleSystem.Spawn(new SunEmber(HeldBall + off, new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(0.6f, 1.4f)) + NPC.velocity * 0.4f,
				Main.rand.NextFloat(0.7f, 1f), Main.rand.Next(30, 50), 0.15f));
			if (Phase == 1f && Main.rand.NextBool(3))
			{
				Vector2 d = Main.rand.NextVector2Unit();
				ParticleSystem.Spawn(new Spark(HeldBall + d * 14f, d * Main.rand.NextFloat(2f, 4f) + NPC.velocity * 0.5f, SunPalette.Pale, 0.7f, 12));
			}
		}

		/// <summary>During the dash: an afterimage every other tick and a wake of sand torn up behind.</summary>
		private void DiveWakeFX(Vector2 dir)
		{
			if (Main.dedServ)
				return;
			if (((int)Timer & 1) == 0)
			{
				ghostAt[ghostNext] = NPC.Center;
				ghostRot[ghostNext] = NPC.rotation;
				ghostLife[ghostNext] = GhostLife;
				ghostNext = (ghostNext + 1) % Ghosts;
			}
			Vector2 side = dir.RotatedBy(MathHelper.PiOver2);
			ParticleSystem.Spawn(new Smoke(NPC.Center - dir * 34f + side * Main.rand.NextFloat(-22f, 22f),
				-dir * Main.rand.NextFloat(1f, 3f) + side * Main.rand.NextFloat(-1.5f, 1.5f),
				SunPalette.Sand * 0.65f, Main.rand.NextFloat(0.3f, 0.5f), Main.rand.Next(26, 40)));
			ParticleSystem.Spawn(new Spark(NPC.Center - dir * 20f + side * Main.rand.NextFloat(-28f, 28f), -dir * Main.rand.NextFloat(3f, 6f),
				SunPalette.Gold, 0.9f, 14));
			if (Main.rand.NextBool(2))
				ParticleSystem.Spawn(new SandGrain(NPC.Center - dir * 30f + side * Main.rand.NextFloat(-20f, 20f),
					-dir * Main.rand.NextFloat(1f, 3f) + side * Main.rand.NextFloat(-2f, 2f), SunPalette.Sand, 1f, 40));
		}

		public override void PostAI()
		{
			for (int i = 0; i < Ghosts; i++)
			{
				if (ghostLife[i] > 0)
					ghostLife[i]--;
			}
		}

		/// <summary>The roar: the sun in its legs cracks in a sunburst, and a sand storm whirls up around it.</summary>
		private void PhaseTwoFX()
		{
			for (int i = 0; i < 24; i++)
			{
				Dust d = Dust.NewDustDirect(HeldBall - new Vector2(20f), 40, 40, DustID.SolarFlare,
					Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-6f, 6f), 60, default, 1.5f);
				d.noGravity = true;
			}
			if (Main.dedServ)
				return;
			ScarabFX.Sunburst(HeldBall, 1.5f, 16);
			ScarabFX.Sandstorm(NPC.Center, 40, 120f, 1f);
			for (int i = 0; i < 10; i++)
				ParticleSystem.Spawn(new Chip(HeldBall, Main.rand.NextVector2Circular(6f, 6f) - new Vector2(0f, 2f),
					Main.rand.NextBool() ? new Color(255, 200, 74) : new Color(216, 74, 30), 1.2f, 50));
			ParticleSystem.Spawn(new SunGlint(HeldBall, SunPalette.Pale, 160f, 30, 0f, 0.015f));
			Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Main.rand.NextVector2Unit(), 7f, 6f, 24, 2400f, "ScarabRoar"));
		}

		/// <summary>The throes: rays bursting out of it, thicker as it goes, and sand whirled off its shell.</summary>
		private void DyingFX(float p)
		{
			if (Main.dedServ)
				return;
			if (Main.rand.NextFloat() < 0.15f + 0.5f * p)
				ParticleSystem.Spawn(new SunRay(NPC.Center, Main.rand.NextFloat(MathHelper.TwoPi), Main.rand.NextFloat(80f, 140f) + 200f * p,
					Main.rand.NextFloat(4f, 7f), Main.rand.NextBool() ? SunPalette.Gold : SunPalette.Pale, Main.rand.Next(18, 28)));
			if (((int)Main.GameUpdateCount % 20) == 0)
				ParticleSystem.Spawn(new Shockwave(NPC.Center, SunPalette.Gold, 0.2f, 1.4f + 1.2f * p, 24));
			if (Main.rand.NextBool(3))
				ScarabFX.Sandstorm(NPC.Center, 1, 60f + 60f * p, 0.8f);
			if (Main.rand.NextBool(2))
				ParticleSystem.Spawn(new SunEmber(NPC.Center + Main.rand.NextVector2Circular(40f, 28f), Main.rand.NextVector2Circular(1.5f, 1.5f),
					1f, Main.rand.Next(40, 60), 0.1f));
		}

		/// <summary>The final burst: the biggest sunburst in the set, a full storm, and its shell in pieces.</summary>
		private static void DeathBurstFX(Vector2 at)
		{
			if (Main.dedServ)
				return;
			ScarabFX.Sunburst(at, 2.2f, 24);
			ScarabFX.Sandstorm(at, 60, 160f, 1.3f);
			ParticleSystem.Spawn(new SunGlint(at, SunPalette.White, 260f, 40, 0f, 0.01f));
			for (int i = 0; i < 18; i++)
				ParticleSystem.Spawn(new Chip(at + Main.rand.NextVector2Circular(30f, 20f), Main.rand.NextVector2Circular(7f, 7f) - new Vector2(0f, 3f),
					i % 3 == 0 ? new Color(42, 79, 176) : new Color(240, 168, 56), Main.rand.NextFloat(1.2f, 1.8f), 70));
		}

		// ------------------------------------------------------------------ death and loot

		public override bool CheckDead()
			=> death.CheckDead(NPC, 140, () =>
			{
				int ball = ModContent.ProjectileType<SunBall>();
				int shard = ModContent.ProjectileType<SunShard>();
				int beetle = ModContent.ProjectileType<Beetling>();
				int pillar = ModContent.ProjectileType<SandPillar>();
				foreach (Projectile p in Main.ActiveProjectiles)
				{
					if (p.hostile && (p.type == ball || p.type == shard || p.type == beetle || p.type == pillar))
						p.Kill();
				}
			});

		public override void OnKill()
		{
			if (!DownedBossSystem.downedScarab)
			{
				DownedBossSystem.downedScarab = true;
				if (Main.netMode != NetmodeID.MultiplayerClient)
					ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Mods.Terrapex.Chat.ScarabDown"), new Color(255, 200, 90));
				if (Main.netMode == NetmodeID.Server)
					NetMessage.SendData(MessageID.WorldData);
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<ScarabSultanBag>()));

			LeadingConditionRule notExpert = new(new Conditions.NotExpert());
			notExpert.OnSuccess(ItemDropRule.OneFromOptions(1, Weapons));
			notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<GildedChitin>(), 1, 6, 12));
			notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Sunsand>(), 1, 6, 12));
			notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ScarabAmulet>(), 3));
			npcLoot.Add(notExpert);
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ScarabSultanMask>(), 7));
		}

		/// <summary>One per class, and the reaper's scythe - the pool both the kill and the bag draw from.</summary>
		public static int[] Weapons => new[] {
			ModContent.ItemType<SultansKhopesh>(),
			ModContent.ItemType<SunwingBow>(),
			ModContent.ItemType<SunScepter>(),
			ModContent.ItemType<BeetlingStaff>(),
			ModContent.ItemType<KhepriScythe>(),
		};

		public override void BossLoot(ref int potionType) => potionType = ItemID.LesserHealingPotion;

		// ------------------------------------------------------------------ drawing

		public override void FindFrame(int frameHeight)
		{
			NPC.frameCounter++;
			if (NPC.frameCounter >= 4)
			{
				NPC.frameCounter = 0;
				NPC.frame.Y = (NPC.frame.Y + frameHeight) % (frameHeight * Main.npcFrameCount[Type]);
			}
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			bool alive = !death.Dying;

			// the kick's tell: the ball's whole path, from where it sits now, by the ball's own rules
			if (alive && Move == Kick && Timer > 0f && Timer <= KickTell)
			{
				float f = Timer / KickTell;
				BallPhysics.DrawPath(HeldBall, KickVelocity, SunBall.Size, SunBall.Size, 150,
					new Color(255, 200, 90), 0.3f + 0.7f * f, true);
			}

			float time = Main.GlobalTimeWrappedHourly;

			// the dive's tell: a sun-ray lance down the aim line, growing and brightening as it commits
			int diveTell = DiveTellNow;
			if (alive && Move == Dive && Timer > 0f && Timer <= diveTell)
				DrawLance(spriteBatch, Timer / diveTell, time);

			Texture2D body = TextureAssets.Npc[Type].Value;
			SpriteEffects fx = NPC.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Vector2 origin = NPC.frame.Size() / 2f;

			// the dive's afterimages, oldest first, behind the body
			for (int k = 0; k < Ghosts; k++)
			{
				int i = (ghostNext + k) % Ghosts;
				if (ghostLife[i] <= 0)
					continue;
				float g = ghostLife[i] / (float)GhostLife;
				spriteBatch.Draw(body, ghostAt[i] - screenPos, NPC.frame, SunPalette.Glow(Color.Lerp(SunPalette.Orange, SunPalette.Gold, g), 0.5f * g),
					ghostRot[i], origin, 1f + 0.06f * (1f - g), fx, 0f);
			}

			// the sun in its legs, whenever it is not out rolling
			Vector2 center = NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY);
			if (ActiveBalls() == 0)
			{
				Texture2D ball = TextureAssets.Projectile[ModContent.ProjectileType<SunBall>()].Value;
				Rectangle frame = ball.Frame(1, 3, 0, BallKind);
				float spin = time * 1.5f * NPC.spriteDirection;
				bool cracked = BallKind == SunBall.Cracked;
				float r = cracked ? 15f : 20f;
				float charge = Move == Kick && Timer > 0f && Timer <= KickTell ? Timer / KickTell : 0f;
				SunDraw.Halo(spriteBatch, HeldBall, r, (alive ? 0.85f : 1f) + 0.4f * charge, time * (1f + charge), NPC.whoAmI);
				spriteBatch.Draw(ball, HeldBall - screenPos, frame, Color.Lerp(drawColor, Color.White, 0.7f),
					spin, frame.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
				if (cracked)
					SunDraw.CrackLight(spriteBatch, HeldBall, r, spin, 0.2f + 0.3f * charge, 0.5f + 0.5f * (float)Math.Sin(time * 4f), 1f);
				SunDraw.Core(spriteBatch, HeldBall, r, 0.7f + 0.6f * charge);
			}

			spriteBatch.Draw(body, center, NPC.frame, drawColor, NPC.rotation, origin, 1f, fx, 0f);

			// the fan's tell: the jewel on its crown gleams - a bloom (as before) and a star glint
			// swelling to the throw
			if (alive && Move == Fan && Timer > 0f && (Timer < FanTell || (Phase == 1f && Timer > FanTell + 10 && Timer < FanTell + 30)))
			{
				float g = Timer < FanTell ? Timer / FanTell : (Timer - FanTell - 10f) / 20f;
				Vector2 jewel = Jewel;
				RiftDraw.Bloom(jewel, new Color(120, 220, 255, 0) * 0.9f, 0.5f);
				float twinkle = 0.85f + 0.15f * (float)Math.Sin(time * 30f);
				SunDraw.Star(spriteBatch, jewel, SunPalette.Lapis, 20f + 50f * g, MathHelper.PiOver4 * 0.3f + g * 0.4f, (0.45f + 0.55f * g) * twinkle);
				SunDraw.Star(spriteBatch, jewel, SunPalette.White, 10f + 20f * g, MathHelper.PiOver4 + g * 0.4f, 0.4f * g);
			}
			return false;
		}

		/// <summary>
		/// The dive's tell, drawn down the committed angle. A faint guide runs the full 900 px from the
		/// first tick - the line is never shorter than the old one - and over it a lance of sunlight
		/// grows out to the full length: a shimmering heat haze, a hot body, a white core, motes
		/// running down it, and a glint at its point. In the last fifth it flickers hard - the commit.
		/// </summary>
		private void DrawLance(SpriteBatch sb, float f, float time)
		{
			Vector2 dir = aimAngle.ToRotationVector2();
			Vector2 side = dir.RotatedBy(MathHelper.PiOver2);
			Vector2 start = NPC.Center;
			const float full = 900f;
			float grow = 1f - (float)Math.Pow(1f - f, 2.2f);
			float len = full * (0.3f + 0.7f * grow);
			float flick = f > 0.8f ? 0.8f + 0.2f * (float)Math.Sin(time * 60f) : 1f;

			RiftDraw.Line(start, start + dir * full, new Color(255, 170, 70) * (0.25f + 0.6f * f), 2f + 2f * f);

			// the heat haze: a wide, faint band whose segments wave across the line, so the air shimmers
			const int segs = 24;
			Vector2 prev = start;
			for (int i = 1; i <= segs; i++)
			{
				float t = i / (float)segs;
				Vector2 p = start + dir * len * t + side * (float)Math.Sin(t * 18f - time * 14f) * (2f + 3f * f) * t;
				SunDraw.Line(sb, prev, p, SunPalette.Glow(SunPalette.Orange, (0.08f + 0.14f * f) * (1f - t * 0.5f)), 10f + 14f * f);
				prev = p;
			}
			SunDraw.Line(sb, start, start + dir * len, SunPalette.Glow(SunPalette.Gold, (0.3f + 0.55f * f) * flick), 2.5f + 3f * f);
			SunDraw.Line(sb, start, start + dir * len, SunPalette.Glow(SunPalette.White, (0.2f + 0.6f * f) * flick), 1f + 1f * f);

			for (int k = 0; k < 6; k++)
			{
				float s = (time * (0.8f + 0.8f * f) + k / 6f) % 1f;
				SunDraw.Flare(sb, start + dir * len * s, SunPalette.Glow(SunPalette.Pale, (0.25f + 0.5f * f) * (float)Math.Sin(s * MathHelper.Pi)),
					new Vector2(16f, 6f), aimAngle);
			}
			SunDraw.Star(sb, start + dir * len, SunPalette.Gold, 24f + 40f * f, aimAngle, (0.35f + 0.65f * f) * flick);
		}
	}
}
