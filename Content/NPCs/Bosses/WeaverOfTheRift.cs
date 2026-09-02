using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
	/// Boss 3. A loom on legs: the body sits at the hub of the arena, its legs are driven in as
	/// anchors around it, and the space between them is the weapon.
	///
	/// The fight is about lines rather than about projectiles, and every line is announced. A
	/// thread does not travel — it is drawn tight over 40 harmless ticks and then it is simply
	/// *there*, so what the player reads is not a bullet to sidestep but a room whose walls are
	/// about to change. Nine moves run in fixed rotations per phase, which is the point: the
	/// Weaver is a boss you learn, not one you react to.
	///
	/// Two things were deliberately taken back out after playtesting. It no longer sits behind
	/// an invulnerability wall while an anchor stands — six anchors take forty percent off the
	/// damage it receives and cutting each one pays for itself immediately, and an anchor's
	/// death staggers it into an open window. And nothing it fires homes any more: the old
	/// wandering orbs were unreadable, so the volleys are flat fans laid on the player's
	/// position at the moment of firing and they can be led.
	/// </summary>
	[AutoloadBossHead]
	public class WeaverOfTheRift : ModNPC
	{
		// WeaverOfTheRift.png: 6 frames of 112, 0-3 idle loop, 4-5 weaving
		private const int IdleFrames = 4;
		private const int WeaveFrame = 4;

		private const float Phase2At = 0.62f;
		private const float Phase3At = 0.28f;

		/// <summary>The web's outer radius, and the number of corners it has.</summary>
		private const float Ring = 340f;
		private const float InnerRing = 175f;
		private const int Anchors = 6;
		private const int AnchorRegrow = 60 * 22;

		/// <summary>How long an anchor's death buys you before it resumes weaving.</summary>
		private const int StaggerTicks = 70;
		private const float StaggerBonus = 1.35f;

		private enum Move
		{
			Idle = 0,
			SpokeSweep,
			RingClose,
			ShuttleRun,
			Lattice,
			CrossSnap,
			BobbinVolley,
			Tether,
			Collapse,
			Dash
		}

		// designed rotations rather than randomness, so the fight has a rhythm you can learn
		private static readonly Move[] PatternP1 = {
			Move.SpokeSweep, Move.BobbinVolley, Move.ShuttleRun, Move.RingClose
		};
		private static readonly Move[] PatternP2 = {
			Move.Lattice, Move.CrossSnap, Move.ShuttleRun,
			Move.BobbinVolley, Move.RingClose, Move.SpokeSweep
		};
		private static readonly Move[] PatternP3 = {
			Move.Collapse, Move.Dash, Move.Tether, Move.CrossSnap, Move.Dash, Move.BobbinVolley
		};

		private Move State
		{
			get => (Move)(int)NPC.ai[0];
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

		/// <summary>Arena centre, captured where the boss was summoned.</summary>
		private Vector2 Home
		{
			get => new Vector2(NPC.localAI[0], NPC.localAI[1]);
			set { NPC.localAI[0] = value.X; NPC.localAI[1] = value.Y; }
		}

		private float Spin => NPC.localAI[2];

		private int patternIndex;
		private int regrow;
		/// <summary>Where the anchors are told to sit this tick; a move may pull the ring in.</summary>
		private float seatRadius = Ring;
		private bool freezeSpin;

		// telegraph line, client-side only — it is a read, not a hitbox
		private Vector2 aimA, aimB;
		private float aimStrength;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 6;
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			NPCID.Sets.BossBestiaryPriority.Add(Type);
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
		}

		public override void SetDefaults()
		{
			NPC.width = 96;
			NPC.height = 96;
			NPC.damage = 88;
			NPC.defense = 40;
			// sized against Plantera rather than against the plan's spreadsheet: this is the boss
			// after her, and the old 52 000 came out at 100 000 in master — twice her — which is
			// what turned the fight into attrition. 32 000 through the 0.7 below lands near 61 000
			// there, a clear step past her 50 000 without being a different order of thing.
			NPC.lifeMax = 32000;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.knockBackResist = 0f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.boss = true;
			NPC.aiStyle = -1;
			NPC.value = Item.buyPrice(gold: 30);
			NPC.npcSlots = 12f;
			Music = MusicID.Boss3;
		}

		public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
		{
			NPC.lifeMax = (int)(NPC.lifeMax * 0.7f * balance);
			NPC.damage = (int)(NPC.damage * 0.8f);
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Underground,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.WeaverOfTheRift.Bestiary")
			});
		}

		// ------------------------------------------------------------------- damage taken

		/// <summary>
		/// Every anchor standing shelters the body. This replaces the old flat invulnerability:
		/// it is computed from a scan every client can run, so it needs no syncing, and it turns
		/// the anchors from a gate into a dial the player is always free to turn.
		/// </summary>
		public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
		{
			int alive = CountAnchors();
			if (alive > 0)
				modifiers.FinalDamage *= MathHelper.Clamp(1f - WeaverAnchor.Shelter * alive, 0.3f, 1f);
			if (Staggered)
				modifiers.FinalDamage *= StaggerBonus;
		}

		private bool Staggered => State == Move.Idle && Timer < 0f;

		/// <summary>Called by an anchor as it dies: the loom lurches and drops what it was doing.</summary>
		public void Stagger()
		{
			CutThreads();
			State = Move.Idle;
			Timer = -StaggerTicks;
			NPC.velocity *= 0.3f;
			if (Main.netMode != NetmodeID.Server)
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center,
					Main.rand.NextVector2Unit(), 5f, 6f, 14, 2400f, FullName));
			for (int i = 0; i < 26; i++)
			{
				Dust d = Dust.NewDustPerfect(NPC.Center, DustID.Vortex,
					Main.rand.NextVector2Circular(7f, 7f), 90, default, 1.5f);
				d.noGravity = true;
			}
		}

		// ---------------------------------------------------------------------- main loop

		public override void AI()
		{
			// nothing else runs while it is coming apart: no attacks, no targeting,
			// no phase checks. The NPC may be gone the moment Tick returns true.
			if (death.Dying)
			{
				death.Tick(NPC, DustID.Vortex, new Color(120, 240, 220));
				return;
			}

			if (NPC.localAI[3] == 0f)
			{
				NPC.localAI[3] = 1f;
				Home = NPC.Center;
				Phase = 1f;
			}

			if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
				NPC.TargetClosest();
			Player target = Main.player[NPC.target];

			if (target.dead || !target.active)
			{
				// no despawn scramble: it walks off the way it arrived
				CutAnchors();
				NPC.velocity.Y -= 0.4f;
				if (NPC.timeLeft > 60)
					NPC.timeLeft = 60;
				return;
			}

			Timer++;
			aimStrength = 0f;
			seatRadius = Ring;
			freezeSpin = false;
			CheckPhase();

			switch (State)
			{
				case Move.SpokeSweep: DoSpokeSweep(target); break;
				case Move.RingClose: DoRingClose(target); break;
				case Move.ShuttleRun: DoShuttleRun(target); break;
				case Move.Lattice: DoLattice(target); break;
				case Move.CrossSnap: DoCrossSnap(target); break;
				case Move.BobbinVolley: DoBobbinVolley(target); break;
				case Move.Tether: DoTether(target); break;
				case Move.Collapse: DoCollapse(target); break;
				case Move.Dash: DoDash(target); break;
				default: DoIdle(target); break;
			}

			if (!freezeSpin)
				NPC.localAI[2] += Phase >= 3f ? 0f : (Phase >= 2f ? 0.010f : 0.006f);

			if (Phase < 3f)
			{
				SeatAnchors();
				RegrowAnchors();
			}

			Lighting.AddLight(NPC.Center, 0.5f, 1.0f, 0.95f);
		}

		private void CheckPhase()
		{
			float life = NPC.life / (float)NPC.lifeMax;
			if (Phase < 2f && life <= Phase2At)
			{
				Phase = 2f;
				Announce("WeaverPhase2");
				CutThreads();
				CutAnchors();
				SpawnAnchors();
				State = Move.Idle;
				Timer = -60f;
			}
			else if (Phase < 3f && life <= Phase3At)
			{
				Phase = 3f;
				Announce("WeaverPhase3");
				CutThreads();
				CutAnchors();
				State = Move.Idle;
				Timer = -50f;
			}
		}

		private void DoIdle(Player target)
		{
			if (Phase >= 3f)
				Chase(target, Staggered ? 3f : 6.5f, 0.03f);
			else
				Hover(Home, 0.035f, Staggered ? 2f : 5f);

			int wait = Phase >= 3f ? 30 : (Phase >= 2f ? 45 : 60);
			if (Timer >= wait)
				Pick();
		}

		private void Pick()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			Move[] pattern = Phase >= 3f ? PatternP3 : (Phase >= 2f ? PatternP2 : PatternP1);
			Move next = pattern[patternIndex % pattern.Length];

			// the three anchor moves need corners to hang from; skip past them if the player has
			// cut the web down rather than standing there weaving nothing
			if ((next == Move.SpokeSweep || next == Move.RingClose || next == Move.ShuttleRun
				|| next == Move.Lattice) && CountAnchors() < 2)
			{
				patternIndex++;
				next = pattern[patternIndex % pattern.Length];
				if (next == Move.SpokeSweep || next == Move.RingClose || next == Move.ShuttleRun
					|| next == Move.Lattice)
					next = Move.BobbinVolley;
			}

			patternIndex++;
			State = next;
		}

		private void EndMove() => State = Move.Idle;

		// ------------------------------------------------------------------------- moves

		/// <summary>
		/// Alternating spokes out of the hub, in two waves. The simplest thing the web does and
		/// the first thing it teaches: stand in a wedge, and when the wedges swap, move one over.
		/// </summary>
		private void DoSpokeSweep(Player target)
		{
			Hover(Home, 0.035f, 5f);
			const int Wave = 170;
			int t = (int)Timer;

			if (t == 1 || t == Wave + 1)
			{
				int parity = t == 1 ? 0 : 1;
				EyeFlash(18);
				SoundEngine.PlaySound(SoundID.Item84, NPC.Center);
				for (int i = 0; i < Anchors; i++)
				{
					if (i % 2 != parity)
						continue;
					int who = AnchorAt(i);
					if (who < 0)
						continue;
					TensionAt(i);
					if (Main.netMode != NetmodeID.MultiplayerClient)
						RiftThread.Spawn(NPC, Home, Main.npc[who].Center, RiftThread.Pinned, who,
							ThreadDamage, RiftThread.Telegraph + 120);
				}
			}

			if (t >= Wave * 2)
				EndMove();
		}

		/// <summary>
		/// The perimeter is strung, one side is left open, and then the whole ring walks inward.
		/// The door is put a quarter turn from where the player was standing, so getting out is
		/// a journey with a clock on it rather than a step to the side.
		/// </summary>
		private void DoRingClose(Player target)
		{
			Hover(Home, 0.03f, 4f);
			int t = (int)Timer;

			if (t == 1)
			{
				float want = (target.Center - Home).ToRotation() + MathHelper.PiOver2;
				int door = (int)Math.Round((want - Spin) / (MathHelper.TwoPi / Anchors));
				Counter = (door % Anchors + Anchors) % Anchors;

				EyeFlash(22);
				SoundEngine.PlaySound(SoundID.Item84, NPC.Center);
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					for (int i = 0; i < Anchors; i++)
					{
						if (i == (int)Counter)
							continue;
						int a = AnchorAt(i), b = AnchorAt((i + 1) % Anchors);
						if (a < 0 || b < 0)
							continue;
						RiftThread.Spawn(NPC, Main.npc[a].Center, Main.npc[b].Center, a, b,
							ThreadDamage, 300);
					}
				}
				for (int i = 0; i < Anchors; i++)
					TensionAt(i);
			}

			if (t >= 60)
				seatRadius = MathHelper.Lerp(Ring, 140f, MathHelper.Clamp((t - 60) / 180f, 0f, 1f));
			if (t >= 300)
			{
				CutThreads();
				EndMove();
			}
		}

		/// <summary>
		/// It runs the spoke nearest the player, three times, laying silk on the line it takes.
		/// The aim line is drawn for forty-four ticks first — the dash itself is fast enough to
		/// be unfair without it.
		/// </summary>
		private void DoShuttleRun(Player target)
		{
			const int Beat = 84;
			int run = (int)Timer / Beat, t = (int)Timer % Beat;
			if (run >= 3)
			{
				EndMove();
				return;
			}

			if (t == 0)
				Counter = NearestAnchor(target.Center);

			int who = (int)Counter;
			if (who < 0 || who >= Main.maxNPCs || !Main.npc[who].active)
			{
				EndMove();
				return;
			}
			Vector2 to = Main.npc[who].Center;

			if (t < 44)
			{
				Hover(Home, 0.04f, 6f);
				aimA = NPC.Center;
				aimB = to;
				aimStrength = t / 44f;
				if (t == 32)
				{
					EyeFlash(16);
					if (Main.npc[who].ModNPC is WeaverAnchor anchor)
						anchor.Tension = 1f;
				}
			}
			else if (t == 44)
			{
				NPC.velocity = (to - NPC.Center).SafeNormalize(Vector2.UnitX) * 24f;
				SoundEngine.PlaySound(SoundID.Item122, NPC.Center);
				Shake(8f, 14);
				if (Main.netMode != NetmodeID.MultiplayerClient)
					RiftThread.Spawn(NPC, NPC.Center, to, RiftThread.Pinned, who,
						ThreadDamage, RiftThread.Telegraph + 90);
			}
			else if (t < 66)
			{
				NPC.rotation = NPC.velocity.X * 0.02f;
			}
			else
			{
				NPC.velocity *= 0.86f;
			}
		}

		/// <summary>
		/// The web proper: a hub, six radials, and two concentric rings — knitted one strand at a
		/// time so it can be watched going up.
		///
		/// Each ring is missing one chord and the two gaps sit opposite each other, which is what
		/// makes this a route and not a wall. You come in through the outer door, travel half way
		/// round the annulus, and drop through the inner one into the calm at the hub — where the
		/// boss is, which is exactly where a melee player wants to be pushed.
		/// </summary>
		private void DoLattice(Player target)
		{
			Hover(Home, 0.03f, 4f);
			freezeSpin = true;
			int t = (int)Timer;
			const int Step = 12;
			const int Elements = Anchors * 3;
			const int Strand = RiftThread.Telegraph + 400;

			if (t == 1)
			{
				Counter = Main.rand.Next(Anchors);
				EyeFlash(26);
				SoundEngine.PlaySound(SoundID.Item84, NPC.Center);
				for (int i = 0; i < Anchors; i++)
					TensionAt(i);
			}

			if (t >= 1 && t < 1 + Elements * Step && (t - 1) % Step == 0
				&& Main.netMode != NetmodeID.MultiplayerClient)
			{
				int k = (t - 1) / Step;
				int outerDoor = (int)Counter;
				int innerDoor = (outerDoor + Anchors / 2) % Anchors;

				if (k < Anchors)
				{
					// a radial, hub out to the inner ring: the calm centre is still divided
					RiftThread.Spawn(NPC, Home, WebPoint(k, InnerRing),
						RiftThread.Pinned, RiftThread.Pinned, ThreadDamage, Strand);
				}
				else if (k < Anchors * 2)
				{
					int i = k - Anchors;
					if (i != innerDoor)
						RiftThread.Spawn(NPC, WebPoint(i, InnerRing), WebPoint(i + 1, InnerRing),
							RiftThread.Pinned, RiftThread.Pinned, ThreadDamage, Strand);
				}
				else
				{
					int i = k - Anchors * 2;
					if (i != outerDoor)
						RiftThread.Spawn(NPC, WebPoint(i, Ring), WebPoint(i + 1, Ring),
							RiftThread.Pinned, RiftThread.Pinned, ThreadDamage, Strand);
				}
			}

			// once it is up, it is swept: the web is the pressure, the shards are the reason to
			// stop admiring it
			if (t == 280 || t == 350 || t == 420)
				Volley(target, 5, 0.38f);

			if (t >= 470)
			{
				CutThreads();
				EndMove();
			}
		}

		/// <summary>
		/// Four chords laid across the arena through wherever the player happens to be, forty-five
		/// degrees apart and fifty-five ticks apart. No geometry to learn — just keep moving, and
		/// do not still be there in two thirds of a second.
		/// </summary>
		private void DoCrossSnap(Player target)
		{
			Hover(Vector2.Lerp(Home, target.Center, 0.25f), 0.03f, 5f);
			int t = (int)Timer;

			if (t % 55 == 1 && t < 200 && Main.netMode != NetmodeID.MultiplayerClient)
			{
				float angle = Spin + t / 55 * MathHelper.PiOver4;
				Vector2 reach = angle.ToRotationVector2() * 760f;
				RiftThread.Spawn(NPC, target.Center - reach, target.Center + reach,
					RiftThread.Pinned, RiftThread.Pinned, ThreadDamage, RiftThread.Telegraph + 110);
			}
			if (t % 55 == 1 && t < 200)
				EyeFlash(10);

			if (t >= 280)
				EndMove();
		}

		/// <summary>
		/// Three flat fans of shards, twenty ticks after an eye flash each time. Nothing here
		/// turns: the shot is laid on where you are when it leaves, so it can be led.
		/// </summary>
		private void DoBobbinVolley(Player target)
		{
			Hover(Home + new Vector2(0f, -50f), 0.035f, 6f);
			int t = (int)Timer;

			if (t == 20 || t == 80 || t == 140)
				EyeFlash(14);
			if (t == 40 || t == 100 || t == 160)
				Volley(target, Phase >= 3f ? 7 : 5, 0.42f);

			if (t >= 200)
				EndMove();
		}

		/// <summary>
		/// Phase three. Three strands out of the body to points around the player, a triangle you
		/// stand inside and leave through a corner, then the same again rotated sixty degrees.
		/// </summary>
		private void DoTether(Player target)
		{
			Chase(target, 6f, 0.03f);
			int t = (int)Timer;

			if ((t == 1 || t == 110) && Main.netMode != NetmodeID.MultiplayerClient)
			{
				float off = t == 1 ? 0f : MathHelper.Pi / 3f;
				for (int i = 0; i < 3; i++)
				{
					Vector2 to = target.Center + (off + i * MathHelper.TwoPi / 3f).ToRotationVector2() * 300f;
					RiftThread.Spawn(NPC, NPC.Center, to, RiftThread.Pinned, RiftThread.Pinned,
						ThreadDamage, RiftThread.Telegraph + 130);
				}
			}
			if (t == 1 || t == 110)
				EyeFlash(16);

			if (t >= 230)
				EndMove();
		}

		/// <summary>
		/// The signature. A twelve-sided net closes on where the player stood, with two of its
		/// sides never strung — and it turns while it closes, so holding the door is the move.
		/// </summary>
		private void DoCollapse(Player target)
		{
			Hover(Home, 0.02f, 3f);
			int t = (int)Timer;

			if (t == 1)
			{
				EyeFlash(30);
				Shake(6f, 20);
				SoundEngine.PlaySound(SoundID.Item84, NPC.Center);
				if (Main.netMode != NetmodeID.MultiplayerClient)
					WebCollapse.Spawn(NPC, target.Center, ThreadDamage,
						Main.rand.Next(12), Main.rand.NextBool() ? 1f : -1f);
			}
			if (t == 180)
				Volley(target, 6, 0.5f);

			if (t >= WebCollapse.Life + 40)
				EndMove();
		}

		/// <summary>
		/// Phase three. It backs off along your line, shows the line for thirty-six ticks, then
		/// crosses the arena on it and leaves the silk behind — so the arena keeps shrinking
		/// while the thing that made it shrink is still coming at you.
		/// </summary>
		private void DoDash(Player target)
		{
			const int Beat = 68;
			int run = (int)Timer / Beat, t = (int)Timer % Beat;
			if (run >= 3)
			{
				EndMove();
				return;
			}

			if (t < 36)
			{
				Vector2 away = (NPC.Center - target.Center).SafeNormalize(Vector2.UnitX);
				Hover(target.Center + away * 420f, 0.05f, 9f);
				aimA = NPC.Center;
				aimB = NPC.Center + (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX) * 1300f;
				aimStrength = t / 36f;
				if (t == 30)
					EyeFlash(18);
			}
			else if (t == 36)
			{
				NPC.velocity = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX) * 25f;
				SoundEngine.PlaySound(SoundID.Item122, NPC.Center);
				Shake(10f, 16);
				if (Main.netMode != NetmodeID.MultiplayerClient)
					RiftThread.Spawn(NPC, NPC.Center, NPC.Center + NPC.velocity * 46f,
						RiftThread.Pinned, RiftThread.Pinned, ThreadDamage, RiftThread.Telegraph + 90);
			}
			else if (t < 58)
			{
				NPC.rotation = NPC.velocity.X * 0.02f;
			}
			else
			{
				NPC.velocity *= 0.85f;
			}
		}

		// ----------------------------------------------------------------------- movement

		private void Hover(Vector2 at, float ease, float cap)
		{
			Vector2 gap = at - NPC.Center;
			NPC.velocity = Vector2.Lerp(NPC.velocity, gap * ease, 0.15f);
			if (NPC.velocity.Length() > cap)
				NPC.velocity = Vector2.Normalize(NPC.velocity) * cap;
			NPC.rotation = NPC.velocity.X * 0.015f;
		}

		private void Chase(Player target, float speed, float ease)
		{
			Vector2 want = target.Center - NPC.Center;
			float dist = want.Length();
			if (dist > 8f)
				NPC.velocity = Vector2.Lerp(NPC.velocity, want / dist * speed, ease);
			NPC.rotation = NPC.velocity.X * 0.02f;
		}

		// ------------------------------------------------------------------------ shooting

		private static int ProjDamage(int damage) => Main.expertMode ? damage / 2 : damage;

		/// <summary>Raw, not derived from contact damage — see ProjDamage.</summary>
		private const int SilkDamage = 76;
		private const int ShardDamage = 62;

		private static int ThreadDamage => ProjDamage(SilkDamage);

		/// <summary>A flat fan of shards on the player's position. Nothing in it steers.</summary>
		private void Volley(Player target, int count, float spread)
		{
			SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			float baseAngle = (target.Center - NPC.Center).ToRotation();
			for (int i = 0; i < count; i++)
			{
				float f = count == 1 ? 0.5f : i / (float)(count - 1);
				Vector2 aim = (baseAngle + MathHelper.Lerp(-spread, spread, f)).ToRotationVector2() * 5.4f;
				Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, aim,
					ModContent.ProjectileType<RiftShard>(), ProjDamage(ShardDamage), 2f, Main.myPlayer);
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
				Dust d = Dust.NewDustPerfect(NPC.Center, DustID.Vortex,
					Main.rand.NextVector2Circular(5f, 5f), 90, default, 1.5f);
				d.noGravity = true;
			}
		}

		private static void Announce(string key)
		{
			if (Main.netMode == NetmodeID.Server)
				ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Mods.Terrapex.Chat." + key),
					new Color(120, 240, 220));
			else if (Main.netMode == NetmodeID.SinglePlayer)
				Main.NewText(Language.GetTextValue("Mods.Terrapex.Chat." + key), 120, 240, 220);
		}

		// -------------------------------------------------------------------- the anchors

		/// <summary>A corner of the web, as a fixed world point.</summary>
		private Vector2 WebPoint(int i, float radius)
			=> Home + (Spin + i * MathHelper.TwoPi / Anchors).ToRotationVector2() * radius;

		private static int AnchorType => ModContent.NPCType<WeaverAnchor>();

		private bool Mine(NPC a) => a.active && a.type == AnchorType && a.ai[1] == NPC.whoAmI;

		private int CountAnchors()
		{
			int n = 0;
			for (int i = 0; i < Main.maxNPCs; i++)
				if (Mine(Main.npc[i]))
					n++;
			return n;
		}

		private int AnchorAt(int slot)
		{
			for (int i = 0; i < Main.maxNPCs; i++)
				if (Mine(Main.npc[i]) && (int)Main.npc[i].ai[0] == slot)
					return i;
			return -1;
		}

		private int NearestAnchor(Vector2 to)
		{
			int best = -1;
			float dist = float.MaxValue;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				if (!Mine(Main.npc[i]))
					continue;
				float d = Vector2.DistanceSquared(Main.npc[i].Center, to);
				if (d < dist)
				{
					dist = d;
					best = i;
				}
			}
			return best;
		}

		private void TensionAt(int slot)
		{
			int who = AnchorAt(slot);
			if (who >= 0 && Main.npc[who].ModNPC is WeaverAnchor anchor)
				anchor.Tension = 1f;
		}

		private void SeatAnchors()
		{
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				if (!Mine(Main.npc[i]) || Main.npc[i].ModNPC is not WeaverAnchor anchor)
					continue;
				anchor.Seat = WebPoint((int)Main.npc[i].ai[0], seatRadius);
			}
		}

		private void SpawnAnchors()
		{
			for (int i = 0; i < Anchors; i++)
				SpawnAnchor(i);
			SoundEngine.PlaySound(SoundID.Item84, NPC.Center);
		}

		private void SpawnAnchor(int slot)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			Vector2 at = WebPoint(slot, Ring);
			int who = NPC.NewNPC(NPC.GetSource_FromAI(), (int)at.X, (int)at.Y, AnchorType,
				0, slot, NPC.whoAmI);
			if (who < Main.maxNPCs && Main.npc[who].ModNPC is WeaverAnchor anchor)
				anchor.Seat = at;
			if (who < Main.maxNPCs && Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.SyncNPC, number: who);
		}

		/// <summary>
		/// A cut corner grows back, slowly and one at a time. Fast enough that the web is a
		/// standing thing rather than a one-off chore, slow enough that clearing it is a real
		/// window and not a treadmill.
		/// </summary>
		private void RegrowAnchors()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient || CountAnchors() >= Anchors)
			{
				regrow = 0;
				return;
			}
			if (++regrow < AnchorRegrow)
				return;

			regrow = 0;
			for (int slot = 0; slot < Anchors; slot++)
			{
				if (AnchorAt(slot) >= 0)
					continue;
				SpawnAnchor(slot);
				return;
			}
		}

		private void CutAnchors()
		{
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				if (!Mine(Main.npc[i]))
					continue;
				Main.npc[i].life = 0;
				Main.npc[i].HitEffect();
				Main.npc[i].active = false;
			}
		}

		/// <summary>Drops the standing web, so a stagger or a phase break is a real breath.</summary>
		private static void CutThreads()
		{
			int thread = ModContent.ProjectileType<RiftThread>();
			for (int i = 0; i < Main.maxProjectiles; i++)
				if (Main.projectile[i].active && Main.projectile[i].type == thread
					&& Main.projectile[i].timeLeft > 24)
					Main.projectile[i].timeLeft = 24;
		}

		// ---------------------------------------------------------------------- appearance

		public override void FindFrame(int frameHeight)
		{
			bool weaving = State != Move.Idle && State != Move.Dash;
			if (++NPC.frameCounter >= (weaving ? 5.0 : 7.0))
			{
				NPC.frameCounter = 0.0;
				int frame = NPC.frame.Y / frameHeight;
				frame = weaving ? WeaveFrame + (frame + 1) % 2 : (frame + 1) % IdleFrames;
				NPC.frame.Y = frame * frameHeight;
			}
		}

		public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			// the glowmask: only the parts that actually emit, drawn at full brightness over the
			// sprite so the eye stays lit in an unlit arena. Guarded, because the draw exists
			// before the sheet does and a missing texture is a load error, not a build error.
			if (ModContent.HasAsset(Texture + "_Glow"))
			{
				Texture2D glow = ModContent.Request<Texture2D>(Texture + "_Glow").Value;
				spriteBatch.Draw(glow, NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY),
					NPC.frame, Color.White * (1f - NPC.alpha / 255f), NPC.rotation,
					NPC.frame.Size() * 0.5f, NPC.scale, SpriteEffects.None, 0f);
			}

			float beat = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.2f) * 0.5f + 0.5f;

			// the loom itself: two rings turning against each other, so the body reads as
			// machinery under load rather than as a sprite hanging in the air
			RiftDraw.Ring(NPC.Center, RiftDraw.Glow(90, 240, 220, 0.30f + beat * 0.12f),
				1.55f, Main.GlobalTimeWrappedHourly * 0.5f);
			RiftDraw.Ring(NPC.Center, RiftDraw.Glow(150, 255, 240, 0.22f),
				1.05f, Main.GlobalTimeWrappedHourly * -0.8f);
			RiftDraw.Bloom(NPC.Center, RiftDraw.Glow(120, 255, 235, 0.35f + beat * 0.2f), 1.3f);

			if (Staggered)
			{
				// the open window is loud on purpose: this is the moment worth spending on
				float flicker = 0.6f + Main.rand.NextFloat() * 0.4f;
				RiftDraw.Bloom(NPC.Center, RiftDraw.Glow(255, 220, 150, 0.5f * flicker), 2.1f);
				RiftDraw.Ring(NPC.Center, RiftDraw.Glow(255, 210, 140, 0.5f * flicker),
					2.0f, Main.GlobalTimeWrappedHourly * 1.6f);
			}

			if (aimStrength > 0f)
			{
				// the telegraph, brightening as it runs out
				float f = aimStrength;
				RiftDraw.Line(aimA, aimB, RiftDraw.Glow(255, 190, 130, 0.10f + f * 0.30f), 2f + f * 3f);
				RiftDraw.Line(aimA, aimB, RiftDraw.Glow(255, 245, 220, 0.10f + f * 0.35f), 1f + f * 1.2f);
				RiftDraw.Bloom(aimA, RiftDraw.Glow(255, 200, 140, 0.4f + f * 0.5f), 0.5f + f * 0.5f);
			}

			// the strands the boss is currently paying out, drawn from the hub to every corner
			// that is under tension — the web is visibly attached to it
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				if (!Mine(Main.npc[i]) || Main.npc[i].ModNPC is not WeaverAnchor anchor)
					continue;
				float pull = 0.10f + anchor.Tension * 0.5f;
				RiftDraw.Silk(NPC.Center, Main.npc[i].Center,
					RiftDraw.Glow(80, 210, 200, pull), default,
					1f + anchor.Tension * 2f, 18f * (1f - anchor.Tension), -1f, 10);
			}
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 40 : 6); i++)
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Vortex,
					hit.HitDirection, -1f, 100, default, 1.3f);
				d.noGravity = true;
			}
		}

		public override void OnKill()
		{
			CutAnchors();
			CutThreads();
			if (!DownedBossSystem.downedWeaver)
			{
				DownedBossSystem.downedWeaver = true;
				if (Main.netMode == NetmodeID.Server)
					NetMessage.SendData(MessageID.WorldData);
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<WeaverBag>()));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<WeaverTrophy>(), 10));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<WeaverMask>(), 7));

			LeadingConditionRule normal = new(new Conditions.NotExpert());
			// the leg stays behind as the tier's crafting station, per the design doc
			normal.OnSuccess(ItemDropRule.Common(ModContent.ItemType<AnchorLeg>()));
			// one heart smelts into 8 Echo Alloy and the tier wants ~162 of it, so a single heart
			// a kill priced the whole set at twenty-odd runs. Four to six puts a full class set
			// plus a weapon inside about four fights.
			normal.OnSuccess(ItemDropRule.Common(ModContent.ItemType<WeaverHeart>(), 1, 4, 6));
			normal.OnSuccess(ItemDropRule.OneFromOptions(1,
				ModContent.ItemType<Warp>(),
				ModContent.ItemType<Weft>(),
				ModContent.ItemType<LoomStaff>(),
				ModContent.ItemType<Sailcloth>(),
				ModContent.ItemType<Rend>()));
			normal.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Shuttle>(), 8));
			normal.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ThreadOfFate>(), 4));
			npcLoot.Add(normal);
		}

		/// <summary>The throes. Shared timing, own dust: the loom unravels - the web comes down before the weaver does, or the arena would keep killing through the part of the fight that is over.</summary>
		private readonly BossDeath death = new();

		public override bool CheckDead()
			=> death.CheckDead(NPC, 150, () =>
			{
				CutAnchors();
				CutThreads();
			});

		public override bool CheckActive() => false;
	}
}
