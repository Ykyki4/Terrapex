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
using Terrapex.Content.Items.Mounts;
using Terrapex.Content.Items.Placeable;
using Terrapex.Content.Items.Weapons;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.NPCs.Bosses
{
	/// <summary>
	/// Boss 4, and the end of the ladder. Every tier of this mod has been a question about how
	/// many eyes are on you; the First Keeper is the answer, and it is one eye that has been
	/// open since before the crack.
	///
	/// **The Regard.** It does not aim at the player — it *looks*, and the looking is the whole
	/// fight. A cone of sight is drawn out of the pupil and turns toward you at a capped rate,
	/// so it can always be outrun. Standing inside it, you take its attacks aimed squarely at
	/// you and you deal <see cref="Seen"/>x damage to it. Standing outside, it fires blind —
	/// patterns laid on the arena rather than on you — and it only takes <see cref="Unseen"/>x.
	/// Neither is the correct answer. The fight is the negotiation.
	///
	/// **The lids are the dial.** Eight of them, and they are not armour: each one alive
	/// *narrows* the cone. Cutting a lid opens the eye wider, which grows the window in which
	/// you deal double damage and grows the slice of the arena it can aim into at the same
	/// time. So the shell is not a wall to chew through before the fight starts — it is a
	/// difficulty slider with the boss's own health bar on the other end of it, and the player
	/// may turn it either way at any point, in either direction, because lids grow back.
	///
	/// That is deliberately a different shape from the three bosses before it. The Dormant Eye
	/// and the Keeper made their plates a gate; the Weaver made its anchors a damage dial that
	/// only ever pointed one way. This is the first boss in the mod whose adds are worth
	/// keeping alive.
	/// </summary>
	[AutoloadBossHead]
	public class FirstKeeper : ModNPC
	{
		// FirstKeeper.png: 6 frames of 120 — 0-3 the slow idle, 4-5 wide open, during attacks
		private const int IdleFrames = 4;
		private const int OpenFrame = 4;

		private const float Phase2At = 0.68f;
		private const float Phase3At = 0.34f;

		private const int Lids = 8;
		private const float LidRing = 190f;
		private const int LidRegrow = 60 * 26;

		/// <summary>Damage multiplier while the player stands in the cone, and outside it.</summary>
		public const float Seen = 1.9f;
		public const float Unseen = 0.72f;

		/// <summary>Half-angle of the cone with every lid shut, and what each cut lid adds.</summary>
		private const float GazeBase = 0.36f;
		private const float GazePerLid = 0.14f;

		private enum Move
		{
			Idle = 0,
			GazeSweep,
			Watchers,
			LidVolley,
			Blink,
			LidThrow,
			Cascade,
			Recursion,
			Unblinking,
			Pursuit
		}

		// fixed rotations, never random — the same rule the Weaver runs on and for the same
		// reason: a boss with this much health that improvises is a boss nobody can practise
		private static readonly Move[] PatternP1 = {
			Move.GazeSweep, Move.LidVolley, Move.Watchers, Move.Blink
		};
		private static readonly Move[] PatternP2 = {
			Move.LidThrow, Move.Cascade, Move.GazeSweep,
			Move.Watchers, Move.Recursion, Move.LidVolley
		};
		private static readonly Move[] PatternP3 = {
			Move.Unblinking, Move.Pursuit, Move.Recursion,
			Move.Cascade, Move.Watchers, Move.Blink
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

		private Vector2 Home
		{
			get => new Vector2(NPC.localAI[0], NPC.localAI[1]);
			set { NPC.localAI[0] = value.X; NPC.localAI[1] = value.Y; }
		}

		/// <summary>Where the pupil points. Everything the fight is about hangs off this.</summary>
		private ref float Pupil => ref NPC.localAI[2];

		private int patternIndex;
		private int regrow;
		private int lidsCut;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 6;
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			NPCID.Sets.BossBestiaryPriority.Add(Type);
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
		}

		public override void SetDefaults()
		{
			NPC.width = 110;
			NPC.height = 110;
			NPC.damage = 180;
			NPC.defense = 70;
			// read against Moon Lord rather than against the plan's spreadsheet, the same way
			// the Weaver was read against Plantera. 240 000 through the 0.7 below lands near
			// 460 000 in master for one player — a clear step past the Lord without becoming a
			// different order of thing, and the plan's 320 000 was a normal-mode figure that
			// never accounted for the multiplier.
			NPC.lifeMax = 240000;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.knockBackResist = 0f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.boss = true;
			NPC.aiStyle = -1;
			NPC.value = Item.buyPrice(gold: 60);
			NPC.npcSlots = 16f;
			Music = MusicID.LunarBoss;
		}

		public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
		{
			NPC.lifeMax = (int)(NPC.lifeMax * 0.7f * balance);
			NPC.damage = (int)(NPC.damage * 0.8f);
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Sky,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.FirstKeeper.Bestiary")
			});
		}

		// --------------------------------------------------------------------- the Regard

		/// <summary>How wide the cone is right now, as a half-angle.</summary>
		public float GazeHalf => GazeBase + GazePerLid * lidsCut;

		/// <summary>True while that player is inside the cone.</summary>
		public bool Regards(Player player)
		{
			if (player == null || !player.active || player.dead)
				return false;
			float to = (player.Center - NPC.Center).ToRotation();
			return Math.Abs(MathHelper.WrapAngle(to - Pupil)) <= GazeHalf;
		}

		/// <summary>
		/// The one number the whole fight turns on. It is applied per attacker rather than
		/// globally so that in multiplayer the person standing in the light is the one being
		/// paid for it — which is the only version of this mechanic that survives a second
		/// player deciding to hide.
		/// </summary>
		private void ApplyRegard(Player attacker, ref NPC.HitModifiers modifiers)
			=> modifiers.FinalDamage *= Regards(attacker) ? Seen : Unseen;

		public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
			=> ApplyRegard(player, ref modifiers);

		public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
			=> ApplyRegard(Main.player[projectile.owner], ref modifiers);

		/// <summary>Called by a lid as it dies: the eye opens another notch.</summary>
		public void LidCut()
		{
			lidsCut = Math.Min(Lids, lidsCut + 1);
			if (Main.netMode != NetmodeID.Server)
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center,
					Main.rand.NextVector2Unit(), 6f, 6f, 16, 2400f, FullName));
			for (int i = 0; i < 30; i++)
			{
				Dust d = Dust.NewDustPerfect(NPC.Center, DustID.WhiteTorch,
					Main.rand.NextVector2Circular(8f, 8f), 90, default, 1.5f);
				d.noGravity = true;
			}
		}

		/// <summary>
		/// Where an attack lands. Inside the cone it is laid on the player; outside it, the boss
		/// is genuinely firing blind and the pattern goes somewhere in the arena instead. That
		/// is the readable half of the bargain — hiding is not free, but it does work.
		/// </summary>
		private Vector2 AimPoint(Player target)
			=> Regards(target)
				? target.Center
				: Home + (Pupil + Main.rand.NextFloat(-GazeHalf, GazeHalf)).ToRotationVector2()
					* Main.rand.NextFloat(160f, 420f);

		private float AimAngle(Player target)
			=> Regards(target)
				? (target.Center - NPC.Center).ToRotation()
				: Pupil + Main.rand.NextFloat(-GazeHalf, GazeHalf);

		// ---------------------------------------------------------------------- main loop

		public override void AI()
		{
			// nothing else runs while it is coming apart: no attacks, no targeting,
			// no phase checks. The NPC may be gone the moment Tick returns true.
			if (death.Dying)
			{
				death.Tick(NPC, DustID.WhiteTorch, new Color(235, 240, 255));
				return;
			}

			if (NPC.localAI[3] == 0f)
			{
				NPC.localAI[3] = 1f;
				Home = NPC.Center;
				Phase = 1f;
				Pupil = MathHelper.PiOver2;
				SpawnLids();
			}

			if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
				NPC.TargetClosest();
			Player target = Main.player[NPC.target];

			if (target.dead || !target.active)
			{
				CutLids();
				NPC.velocity.Y -= 0.4f;
				if (NPC.timeLeft > 60)
					NPC.timeLeft = 60;
				return;
			}

			Timer++;
			CheckPhase();
			TurnPupil(target);

			switch (State)
			{
				case Move.GazeSweep: DoGazeSweep(target); break;
				case Move.Watchers: DoWatchers(target); break;
				case Move.LidVolley: DoLidVolley(target); break;
				case Move.Blink: DoBlink(target); break;
				case Move.LidThrow: DoLidThrow(target); break;
				case Move.Cascade: DoCascade(target); break;
				case Move.Recursion: DoRecursion(target); break;
				case Move.Unblinking: DoUnblinking(target); break;
				case Move.Pursuit: DoPursuit(target); break;
				default: DoIdle(target); break;
			}

			if (Phase < 3f)
			{
				SeatLids();
				RegrowLids();
			}

			Lighting.AddLight(NPC.Center, 0.9f, 0.9f, 1.0f);
		}

		/// <summary>
		/// The pupil turns at a hard cap. This is the single number that decides whether the
		/// fight is fair: fast enough that hiding costs constant movement, slow enough that a
		/// player who commits to running can always leave the cone.
		/// </summary>
		private void TurnPupil(Player target)
		{
			float rate = Phase >= 3f ? 0.022f : (Phase >= 2f ? 0.016f : 0.011f);
			float want = (target.Center - NPC.Center).ToRotation();
			Pupil = MathHelper.WrapAngle(Pupil
				+ MathHelper.Clamp(MathHelper.WrapAngle(want - Pupil), -rate, rate));
		}

		private void CheckPhase()
		{
			float life = NPC.life / (float)NPC.lifeMax;
			if (Phase < 2f && life <= Phase2At)
			{
				Phase = 2f;
				Announce("FirstKeeperPhase2");
				State = Move.Idle;
				Timer = -50f;
				Shake(9f, 24);
			}
			else if (Phase < 3f && life <= Phase3At)
			{
				Phase = 3f;
				Announce("FirstKeeperPhase3");
				// it sheds the shell itself rather than waiting to be undressed: from here the
				// eye is open all the way and neither side is hiding any more
				ShedLids();
				lidsCut = Lids;
				State = Move.Idle;
				Timer = -60f;
				Shake(14f, 34);
			}
		}

		private void DoIdle(Player target)
		{
			if (Phase >= 3f)
				Chase(target, 7f, 0.03f);
			else
				Hover(Home, 0.035f, 5f);

			int wait = Phase >= 3f ? 26 : (Phase >= 2f ? 40 : 55);
			if (Timer >= wait)
				Pick();
		}

		private void Pick()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			Move[] pattern = Phase >= 3f ? PatternP3 : (Phase >= 2f ? PatternP2 : PatternP1);
			Move next = pattern[patternIndex % pattern.Length];

			// the two lid moves need lids; a player who has opened the eye all the way gets
			// more of the eye instead of watching the boss mime at an empty orbit
			if ((next == Move.LidVolley || next == Move.LidThrow) && CountLids() < 2)
			{
				patternIndex++;
				next = Move.Recursion;
			}

			patternIndex++;
			State = next;
		}

		private void EndMove() => State = Move.Idle;

		// ------------------------------------------------------------------------- moves

		/// <summary>
		/// The eye simply looks along one line, and the line turns. The first thing the fight
		/// teaches and the plainest use of the whole mechanic: this is what being seen costs.
		/// </summary>
		private void DoGazeSweep(Player target)
		{
			Hover(Home, 0.035f, 5f);
			int t = (int)Timer;

			if (t == 1 && Main.netMode != NetmodeID.MultiplayerClient)
			{
				float spin = Main.rand.NextBool() ? 0.0062f : -0.0062f;
				GazeRay.Spawn(NPC.GetSource_FromAI(), NPC.Center, AimAngle(target), spin,
					ProjDamage(RayDamage), GazeRay.Telegraph + 200, NPC.whoAmI);
			}
			if (t == 1)
				Flash(24);

			if (t >= GazeRay.Telegraph + 220)
				EndMove();
		}

		/// <summary>
		/// Six eyes set down around the player, each of which commits to one line before any of
		/// them can hurt. Six lines with gaps between them, all drawn at once: the answer is
		/// visible for well over a second before it is needed.
		/// </summary>
		private void DoWatchers(Player target)
		{
			Hover(Home, 0.03f, 4f);
			int t = (int)Timer;
			int count = Phase >= 3f ? 8 : 6;

			if (t == 1 && Main.netMode != NetmodeID.MultiplayerClient)
			{
				Vector2 focus = AimPoint(target);
				float off = Main.rand.NextFloat(MathHelper.TwoPi);
				for (int i = 0; i < count; i++)
				{
					float a = off + i * MathHelper.TwoPi / count;
					Vector2 at = focus + a.ToRotationVector2() * 400f;
					// each one faces the focus, so the lines converge and the safe ground is
					// the wedges between them rather than one lucky spot
					Watcher.Spawn(NPC, at, (focus - at).ToRotation(), ProjDamage(RayDamage));
				}
			}
			if (t == 1)
				Flash(20);

			if (t >= Watcher.Life + 40)
				EndMove();
		}

		/// <summary>
		/// Every lid still closed fires one flat fan. It is the reason keeping the shell on is
		/// not free: the narrow cone you bought comes with eight guns attached to it.
		/// </summary>
		private void DoLidVolley(Player target)
		{
			Hover(Home, 0.03f, 4f);
			int t = (int)Timer;

			if ((t == 30 || t == 90 || t == 150) && Main.netMode != NetmodeID.MultiplayerClient)
			{
				Vector2 at = AimPoint(target);
				for (int i = 0; i < Main.maxNPCs; i++)
				{
					if (!Mine(Main.npc[i]) || Main.npc[i].ModNPC is not KeeperLid lid || !lid.Orbiting)
						continue;
					Fan(Main.npc[i].Center, at, 3, 0.24f);
				}
			}
			if (t == 16 || t == 76 || t == 136)
				Flash(12);

			if (t >= 210)
				EndMove();
		}

		/// <summary>One ring out of the body, with a wedge missing.</summary>
		private void DoBlink(Player target)
		{
			Hover(Home, 0.03f, 4f);
			int t = (int)Timer;

			if (t == 1 && Main.netMode != NetmodeID.MultiplayerClient)
			{
				float gap = (target.Center - NPC.Center).ToRotation() + Main.rand.NextFloat(-0.9f, 0.9f);
				BlinkRing.Spawn(NPC.GetSource_FromAI(), NPC.Center, ProjDamage(RingDamage),
					gap, Phase >= 3f ? 0.34f : 0.46f);
			}
			if (t == 1)
				Flash(20);

			if (t >= BlinkRing.Life + 30)
				EndMove();
		}

		/// <summary>
		/// The lids come off their orbit one at a time and are thrown. Straight out of the
		/// Keeper's own book two tiers down, which is the point — this is the thing that
		/// taught it.
		/// </summary>
		private void DoLidThrow(Player target)
		{
			Hover(Home, 0.03f, 4f);
			int t = (int)Timer;

			if (t % 34 == 1 && t < 34 * 5 && Main.netMode != NetmodeID.MultiplayerClient)
			{
				int who = NearestLid(target.Center);
				if (who >= 0 && Main.npc[who].ModNPC is KeeperLid lid)
					lid.BeginSpear();
			}

			if (t >= 260)
				EndMove();
		}

		/// <summary>
		/// Three rings, staggered, each one's wedge a third of a turn from the last. The move
		/// has a route through it and the route is a circle: keep walking the same way round.
		/// </summary>
		private void DoCascade(Player target)
		{
			Hover(Home, 0.025f, 4f);
			int t = (int)Timer;

			if (t == 1)
			{
				Counter = (target.Center - NPC.Center).ToRotation();
				Flash(26);
				Shake(6f, 18);
			}

			if ((t == 1 || t == 46 || t == 92) && Main.netMode != NetmodeID.MultiplayerClient)
			{
				int k = t / 46;
				BlinkRing.Spawn(NPC.GetSource_FromAI(), NPC.Center, ProjDamage(RingDamage),
					Counter + k * MathHelper.TwoPi / 3f, 0.40f);
			}

			if (t >= BlinkRing.Life + 120)
				EndMove();
		}

		/// <summary>
		/// The signature, and the last rung of the ladder the whole mod has been climbing: the
		/// eye stops being one eye. Three lines out of the same pupil, turning against each
		/// other, so the safe ground is the wedge where none of the three currently is — and
		/// that wedge moves, splits and closes on its own schedule.
		/// </summary>
		private void DoRecursion(Player target)
		{
			Hover(Vector2.Lerp(Home, target.Center, 0.2f), 0.03f, 5f);
			int t = (int)Timer;

			if (t == 1 && Main.netMode != NetmodeID.MultiplayerClient)
			{
				float baseAngle = AimAngle(target);
				for (int i = 0; i < 3; i++)
				{
					// alternating spins: two of the three gaps widen while the third closes,
					// which is what stops it from being one rigid pinwheel to stand behind
					float spin = (i % 2 == 0 ? 1f : -1f) * 0.0052f;
					GazeRay.Spawn(NPC.GetSource_FromAI(), NPC.Center,
						baseAngle + i * MathHelper.TwoPi / 3f, spin,
						ProjDamage(RayDamage), GazeRay.Telegraph + 240, NPC.whoAmI);
				}
			}
			if (t == 1)
			{
				Flash(34);
				Shake(8f, 22);
			}

			if (t == 150 || t == 230)
				Fan(NPC.Center, AimPoint(target), 7, 0.5f);

			if (t >= GazeRay.Telegraph + 260)
				EndMove();
		}

		/// <summary>
		/// Phase three. One ray, and it does not sweep on a fixed rate — it turns toward you,
		/// slowly, for six seconds. The only attack in the fight that follows, and it is capped
		/// low enough that outrunning it is always possible and never free.
		/// </summary>
		private void DoUnblinking(Player target)
		{
			Chase(target, 4.5f, 0.02f);
			int t = (int)Timer;

			if (t == 1 && Main.netMode != NetmodeID.MultiplayerClient)
				GazeRay.Spawn(NPC.GetSource_FromAI(), NPC.Center,
					(target.Center - NPC.Center).ToRotation(), 0f,
					ProjDamage(RayDamage), GazeRay.Telegraph + 320, NPC.whoAmI);
			if (t == 1)
			{
				Flash(30);
				Shake(7f, 20);
			}

			if (t >= GazeRay.Telegraph + 340)
				EndMove();
		}

		/// <summary>Phase three. It closes the distance and pays out shards while it comes.</summary>
		private void DoPursuit(Player target)
		{
			Chase(target, 8.5f, 0.035f);
			int t = (int)Timer;

			if (t % 40 == 20)
				Flash(10);
			if (t % 40 == 0 && t > 0)
				Fan(NPC.Center, AimPoint(target), 5, 0.34f);

			if (t >= 240)
				EndMove();
		}

		// ----------------------------------------------------------------------- movement

		private void Hover(Vector2 at, float ease, float cap)
		{
			Vector2 gap = at - NPC.Center;
			NPC.velocity = Vector2.Lerp(NPC.velocity, gap * ease, 0.15f);
			if (NPC.velocity.Length() > cap)
				NPC.velocity = Vector2.Normalize(NPC.velocity) * cap;
			NPC.rotation = NPC.velocity.X * 0.012f;
		}

		private void Chase(Player target, float speed, float ease)
		{
			Vector2 want = target.Center - NPC.Center;
			float dist = want.Length();
			if (dist > 8f)
				NPC.velocity = Vector2.Lerp(NPC.velocity, want / dist * speed, ease);
			NPC.rotation = NPC.velocity.X * 0.016f;
		}

		// ------------------------------------------------------------------------ shooting

		private static int ProjDamage(int damage) => Main.expertMode ? damage / 2 : damage;

		private const int RayDamage = 170;
		private const int ShardDamage = 140;
		private const int RingDamage = 160;

		/// <summary>A flat fan from a point at a point. Nothing in it steers, ever.</summary>
		private void Fan(Vector2 from, Vector2 at, int count, float spread)
		{
			SoundEngine.PlaySound(SoundID.Item20, from);
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			float baseAngle = (at - from).ToRotation();
			for (int i = 0; i < count; i++)
			{
				float f = count == 1 ? 0.5f : i / (float)(count - 1);
				Vector2 aim = (baseAngle + MathHelper.Lerp(-spread, spread, f)).ToRotationVector2() * 5.2f;
				Projectile.NewProjectile(NPC.GetSource_FromAI(), from, aim,
					ModContent.ProjectileType<PrimalShard>(), ProjDamage(ShardDamage), 3f, Main.myPlayer);
			}
		}

		private void Shake(float strength, int frames)
		{
			if (Main.netMode == NetmodeID.Server)
				return;
			Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center,
				Main.rand.NextVector2Unit(), strength, 6f, frames, 2400f, FullName));
		}

		private void Flash(int count)
		{
			for (int i = 0; i < count; i++)
			{
				Dust d = Dust.NewDustPerfect(NPC.Center + Pupil.ToRotationVector2() * 34f,
					DustID.WhiteTorch, Main.rand.NextVector2Circular(6f, 6f), 90, default, 1.5f);
				d.noGravity = true;
			}
		}

		private static void Announce(string key)
		{
			if (Main.netMode == NetmodeID.Server)
				ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Mods.Terrapex.Chat." + key),
					new Color(232, 236, 250));
			else if (Main.netMode == NetmodeID.SinglePlayer)
				Main.NewText(Language.GetTextValue("Mods.Terrapex.Chat." + key), 232, 236, 250);
		}

		// ----------------------------------------------------------------------- the lids

		private static int LidType => ModContent.NPCType<KeeperLid>();

		private bool Mine(NPC n) => n.active && n.type == LidType && (int)n.ai[0] == NPC.whoAmI;

		private int CountLids()
		{
			int n = 0;
			for (int i = 0; i < Main.maxNPCs; i++)
				if (Mine(Main.npc[i]))
					n++;
			return n;
		}

		private int NearestLid(Vector2 to)
		{
			int best = -1;
			float dist = float.MaxValue;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				if (!Mine(Main.npc[i]) || Main.npc[i].ModNPC is not KeeperLid lid || !lid.Orbiting)
					continue;
				float d = Vector2.DistanceSquared(Main.npc[i].Center, to);
				if (d < dist) { dist = d; best = i; }
			}
			return best;
		}

		private void SeatLids()
		{
			for (int i = 0; i < Main.maxNPCs; i++)
				if (Mine(Main.npc[i]))
					Main.npc[i].ai[2] = LidRing;
		}

		private void SpawnLids()
		{
			for (int i = 0; i < Lids; i++)
				SpawnLid(i);
		}

		private void SpawnLid(int slot)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			float angle = slot * MathHelper.TwoPi / Lids;
			Vector2 at = NPC.Center + angle.ToRotationVector2() * LidRing;
			int who = NPC.NewNPC(NPC.GetSource_FromAI(), (int)at.X, (int)at.Y, LidType,
				0, NPC.whoAmI, angle, LidRing, KeeperLid.ModeOrbit);
			if (who < Main.maxNPCs && Main.npc[who].ModNPC is KeeperLid lid)
				lid.Slot = slot;
			if (who < Main.maxNPCs && Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.SyncNPC, number: who);
		}

		/// <summary>
		/// A lid grows back, slowly, one at a time — and growing back *narrows* the eye again.
		/// That is the part that makes the dial a dial: a player who opened the eye to burst the
		/// boss down and then wants the cone small again does not have to reload the fight, they
		/// just have to stop cutting for half a minute.
		/// </summary>
		private void RegrowLids()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient || CountLids() >= Lids)
			{
				regrow = 0;
				return;
			}
			if (++regrow < LidRegrow)
				return;

			regrow = 0;
			bool[] taken = new bool[Lids];
			for (int i = 0; i < Main.maxNPCs; i++)
				if (Mine(Main.npc[i]) && Main.npc[i].ModNPC is KeeperLid lid
					&& lid.Slot >= 0 && lid.Slot < Lids)
					taken[lid.Slot] = true;

			for (int slot = 0; slot < Lids; slot++)
			{
				if (taken[slot])
					continue;
				SpawnLid(slot);
				lidsCut = Math.Max(0, lidsCut - 1);
				SoundEngine.PlaySound(SoundID.Item37, NPC.Center);
				return;
			}
		}

		/// <summary>Phase three: it throws the whole shell away at once and never takes it back.</summary>
		private void ShedLids()
		{
			CutLids();
			SoundEngine.PlaySound(SoundID.Item14, NPC.Center);
		}

		private void CutLids()
		{
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				if (!Mine(Main.npc[i]))
					continue;
				// unparent first, or the lid's own death hook opens the eye another notch on
				// the way out and the count drifts
				Main.npc[i].ai[0] = -1f;
				Main.npc[i].life = 0;
				Main.npc[i].HitEffect();
				Main.npc[i].active = false;
			}
		}

		// ---------------------------------------------------------------------- appearance

		public override void FindFrame(int frameHeight)
		{
			bool open = State != Move.Idle;
			if (++NPC.frameCounter >= (open ? 5.0 : 8.0))
			{
				NPC.frameCounter = 0.0;
				int frame = NPC.frame.Y / frameHeight;
				frame = open ? OpenFrame + (frame + 1) % 2 : (frame + 1) % IdleFrames;
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

			// The eye closes. It is the only thing this boss has never done, so the cone goes out
			// on the first frame of the throes rather than fading with everything else - the
			// player should be able to see, immediately, that nothing is looking at them.
			if (death.Dying)
			{
				float open = 1f - death.Progress;
				RiftDraw.Ring(NPC.Center, RiftDraw.Glow(255, 226, 170, open * 0.55f),
					2.2f * open, Main.GlobalTimeWrappedHourly * 1.2f);
				RiftDraw.Bloom(NPC.Center, RiftDraw.Glow(255, 255, 255, open * 0.9f),
					0.3f + 1.6f * open);
				return;
			}

			float beat = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.0f) * 0.5f + 0.5f;
			Vector2 look = Pupil.ToRotationVector2();
			bool seen = Regards(Main.LocalPlayer);

			// The cone. This is the health bar of the fight as far as the player is concerned,
			// so it is drawn every frame whether or not anything is firing: two edges, a fill
			// of faint spokes, and a pupil that brightens when it has you.
			float half = GazeHalf;
			const float Reach = 1100f;
			Color edge = RiftDraw.Glow(235, 240, 255, seen ? 0.26f : 0.13f);
			for (int k = -1; k <= 1; k += 2)
				RiftDraw.Line(NPC.Center, NPC.Center + (Pupil + k * half).ToRotationVector2() * Reach,
					edge, 2.2f);

			const int Spokes = 9;
			for (int i = 1; i < Spokes; i++)
			{
				float a = Pupil - half + i * (half * 2f / Spokes);
				RiftDraw.Line(NPC.Center, NPC.Center + a.ToRotationVector2() * Reach,
					RiftDraw.Glow(200, 208, 235, (seen ? 0.075f : 0.035f) * (0.6f + beat * 0.4f)), 1.4f);
			}

			// the eye itself: a dark iris ring with a white pupil riding out along the look
			RiftDraw.Ring(NPC.Center, RiftDraw.Glow(210, 216, 240, 0.30f + beat * 0.12f),
				1.9f, Main.GlobalTimeWrappedHourly * 0.35f);
			RiftDraw.Ring(NPC.Center, RiftDraw.Glow(40, 44, 60, 0.55f),
				1.5f, Main.GlobalTimeWrappedHourly * -0.55f);
			RiftDraw.Bloom(NPC.Center, RiftDraw.Glow(255, 255, 255, 0.30f + beat * 0.18f), 1.5f);
			RiftDraw.Bloom(NPC.Center + look * 30f,
				RiftDraw.Glow(255, 255, 255, seen ? 0.95f : 0.45f), seen ? 1.15f : 0.75f);

			// and while it has you, a warm halo — the same signal the Weaver's stagger uses,
			// because it means the same thing: this is the moment worth spending on
			if (seen)
			{
				float flicker = 0.7f + Main.rand.NextFloat() * 0.3f;
				RiftDraw.Ring(NPC.Center, RiftDraw.Glow(255, 226, 170, 0.34f * flicker),
					2.3f, Main.GlobalTimeWrappedHourly * 1.4f);
			}
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 60 : 6); i++)
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.WhiteTorch,
					hit.HitDirection, -1f, 100, default, 1.4f);
				d.noGravity = true;
			}
		}

		public override void OnKill()
		{
			CutLids();
			if (!DownedBossSystem.downedFirstKeeper)
			{
				DownedBossSystem.downedFirstKeeper = true;
				if (Main.netMode == NetmodeID.Server)
					NetMessage.SendData(MessageID.WorldData);
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<FirstKeeperBag>()));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FirstKeeperTrophy>(), 10));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FirstKeeperMask>(), 7));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RidingPlate>(), 8));

			LeadingConditionRule normal = new(new Conditions.NotExpert());
			// the whole tier is priced at roughly 240 Primordium, and the Keeper Echo drops it
			// in ones and twos — so the boss is the bulk and the mob is the top-up, rather than
			// the other way round.
			normal.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Primordium>(), 1, 22, 30));
			normal.OnSuccess(ItemDropRule.OneFromOptions(1,
				ModContent.ItemType<FirstShard>(),
				ModContent.ItemType<Unblinking>(),
				ModContent.ItemType<Regard>(),
				ModContent.ItemType<KeeperStaff>(),
				ModContent.ItemType<Closure>()));
			normal.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Nothing>(), 20));
			normal.OnSuccess(ItemDropRule.Common(ModContent.ItemType<WholeEye>(), 4));
			normal.OnSuccess(ItemDropRule.Common(ModContent.ItemType<RealityAnchor>(), 4));
			npcLoot.Add(normal);
		}

		/// <summary>The throes. Shared timing, own dust: the eye closes, which it has not done once since before the crack.</summary>
		private readonly BossDeath death = new();

		public override bool CheckDead()
			=> death.CheckDead(NPC, 170, () =>
			{
				CutLids();
			});

		public override bool CheckActive() => false;
	}
}
