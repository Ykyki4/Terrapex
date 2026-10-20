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
using Terrapex.Content.Items.Armor;
using Terrapex.Content.Items.Consumables;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Items.Weapons.Rime;
using Terrapex.Content.Projectiles.Rime;

namespace Terrapex.Content.NPCs.Bosses
{
	/// <summary>
	/// The Rime - an old frost colossus out of the Snow biome, fought after Skeletron. See
	/// <c>BOSS_RIME.md</c>; the numbers there and here are kept in step.
	///
	/// It never leaves the floor. Everything it does is about the ground and the air over it: a
	/// slam that runs a line of ice along the floor (the answer is a jump), the block it carries
	/// thrown on a drawn arc to a drawn mark, icicles out of the ceiling in columns, and two walls
	/// of ice closing on the player with a door in one of them. Phase two adds a blizzard that
	/// leans on the player - gently, and never without warning.
	///
	/// RimeColossus.png is seven frames of 128x128: 0-3 walk, 4 arms overhead, 5 slam, 6 roar.
	/// The ice block is its own texture (RimeBoulder), drawn behind the body at the hand anchor of
	/// the frame, so it can leave the hands when it is thrown.
	/// </summary>
	[AutoloadBossHead]
	public class RimeColossus : ModNPC
	{
		public const float PhaseTwoAt = 0.5f;

		public const int ShockDamage = 26;
		public const int BoulderDamage = 30;
		public const int ShardDamage = 18;
		public const int IcicleDamage = 22;
		public const int WallDamage = 24;

		/// <summary>The crouch before a slam - the cracks run out along the floor for this long.</summary>
		public const int SlamTell = 44;
		/// <summary>Phase two's second slam comes this long after the first.</summary>
		public const int SlamSecond = 36;
		public const float ShockSpeed = 7.5f;
		/// <summary>The block held overhead while the arc and the landing mark are drawn.</summary>
		public const int ThrowTell = 46;
		/// <summary>The roar before the icicles are hung.</summary>
		public const int RoarTell = 20;
		public const int IcefallSecond = 40;
		public const int IcefallColumnsOne = 7, IcefallColumnsTwo = 8;
		public const float IcefallSpacing = 84f;
		/// <summary>The stamp before the walls come up.</summary>
		public const int WallTell = 24;
		/// <summary>How far either side of the player the walls rise.</summary>
		public const float WallStart = 290f;
		public const float WallSpeedOne = 1.7f, WallSpeedTwo = 2.2f;

		/// <summary>The blizzard: a direction held for WindPeriod ticks, the last WindWarn of them calm.</summary>
		public const int WindPeriod = 600;
		public const int WindWarn = 90;
		public const float WindPush = 0.035f;

		private const int FrameRaise = 4, FrameSlam = 5, FrameRoar = 6;

		private const int Slam = 0, Throw = 1, Icefall = 2, Walls = 3;
		private static readonly int[] RotationOne = { Slam, Throw, Icefall, Throw, Walls };
		private static readonly int[] RotationTwo = { Slam, Icefall, Throw, Walls, Slam, Throw, Icefall };

		private ref float Timer => ref NPC.ai[0];
		private ref float Step => ref NPC.ai[1];
		private ref float Phase => ref NPC.ai[2];
		private ref float Wind => ref NPC.ai[3];

		/// <summary>Where the thrown block will land, decided by the server and synced.</summary>
		private Vector2 throwAt;
		private int leapCooldown;
		private int holdTicks;

		/// <summary>Drops through platforms to a player below, stands on them otherwise.</summary>
		public override bool? CanFallThroughPlatforms()
		{
			Player t = Main.player[NPC.target];
			return t.active && !t.dead && t.Bottom.Y > NPC.Bottom.Y + 20f;
		}

		private readonly BossDeath death = new();

		private int[] Rotation => Phase == 0f ? RotationOne : RotationTwo;
		private int Move => Rotation[(int)Step % Rotation.Length];
		private int Gap => Phase == 0f ? 70 : 40;

		private int SlamEnd => Phase == 0f ? SlamTell + 30 : SlamTell + SlamSecond + 30;
		private int ThrowEnd => Phase == 0f ? 125 : 112;
		private int IcefallEnd => Phase == 0f
			? RoarTell + RimeIcicle.Tell + 40
			: RoarTell + IcefallSecond + RimeIcicle.Tell + 40;
		private float WallSpeed => Phase == 0f ? WallSpeedOne : WallSpeedTwo;
		private int WallEnd => WallTell + RimeWall.Tell + RimeWall.LifeFor(WallSpeed) + 10;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 7;
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			NPCID.Sets.BossBestiaryPriority.Add(Type);
			// its tells (the landing mark, the wind's chevrons, the end of a crack) sit far from the body
			NPCID.Sets.MustAlwaysDraw[Type] = true;
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn] = true;
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn2] = true;
		}

		public override void SetDefaults()
		{
			NPC.width = 96;
			NPC.height = 100;
			NPC.damage = 34;
			NPC.defense = 16;
			NPC.lifeMax = 6500;
			NPC.HitSound = SoundID.NPCHit5;
			NPC.DeathSound = SoundID.NPCDeath7;
			NPC.knockBackResist = 0f;
			NPC.value = Item.buyPrice(gold: 5);
			NPC.aiStyle = -1;
			NPC.boss = true;
			NPC.npcSlots = 10f;
			NPC.noGravity = false;      // a mountain does not fly
			NPC.noTileCollide = false;
			Music = MusicID.Boss3;
		}

		public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
		{
			NPC.lifeMax = (int)(NPC.lifeMax * 0.7f * balance);
			NPC.damage = (int)(NPC.damage * 0.85f);
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
			=> bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Snow,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.RimeColossus.Bestiary")
			});

		public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(throwAt);

		public override void ReceiveExtraAI(BinaryReader reader) => throwAt = reader.ReadVector2();

		// ------------------------------------------------------------------ helpers

		/// <summary>The top of the first solid floor at or below <paramref name="at"/>.</summary>
		public static float FloorBelow(Vector2 at, int maxTiles = 40)
		{
			int tx = (int)(at.X / 16f);
			int ty = (int)(at.Y / 16f);
			for (int i = 0; i < maxTiles; i++)
			{
				int y = ty + i;
				if (!WorldGen.InWorld(tx, y, 2))
					break;
				Tile tile = Main.tile[tx, y];
				if (tile.HasUnactuatedTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]))
					return y * 16f;
			}
			return at.Y + maxTiles * 16f;
		}

		/// <summary>Just under the first solid ceiling above <paramref name="at"/>, or open sky.</summary>
		public static float CeilingAbove(Vector2 at, int maxTiles = 36)
		{
			int tx = (int)(at.X / 16f);
			int ty = (int)(at.Y / 16f);
			for (int i = 3; i < maxTiles; i++)
			{
				int y = ty - i;
				if (!WorldGen.InWorld(tx, y, 2))
					break;
				Tile tile = Main.tile[tx, y];
				if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
					return (y + 1) * 16f + 20f;
			}
			return at.Y - maxTiles * 16f;
		}

		/// <summary>The centre of the body sheet's frame, in the world.</summary>
		private Vector2 DrawCenter => NPC.Bottom + new Vector2(0f, -57f + NPC.gfxOffY);

		/// <summary>Where the block sits for a frame, as an offset from the draw centre facing right.</summary>
		private static Vector2 BlockAnchor(int frame) => frame switch
		{
			FrameRaise => new Vector2(8f, -76f),
			FrameSlam => new Vector2(40f, 42f),
			_ => new Vector2(-30f, -34f),
		};

		private Vector2 BlockWorld(int frame)
		{
			Vector2 a = BlockAnchor(frame);
			a.X *= NPC.spriteDirection;
			return DrawCenter + a;
		}

		private Vector2 Eye => DrawCenter + new Vector2(38f * NPC.spriteDirection, -10f);
		private Vector2 Mouth => DrawCenter + new Vector2(44f * NPC.spriteDirection, 6f);

		/// <summary>How far the slam's crack has run, 0..1, or -1 when no crack is being told.</summary>
		private float SlamTellF()
		{
			if (Timer <= 0f || Move != Slam)
				return -1f;
			if (Timer <= SlamTell)
				return Timer / SlamTell;
			if (Phase == 1f && Timer > SlamTell + 16 && Timer <= SlamTell + SlamSecond)
				return (Timer - SlamTell - 16) / (SlamSecond - 16f);
			return -1f;
		}

		private float SlamReach(float f) => 60f + 620f * f;

		private int CrackSeed(int side) => NPC.whoAmI * 7919 + (int)Step * 31 + side * 3 + (Timer > SlamTell ? 1000 : 0);

		/// <summary>Cosmetic: how far the crack had run last tick, so crystals grow where it has just reached.</summary>
		private float crackReached;

		// ------------------------------------------------------------------ AI

		public override void AI()
		{
			if (death.Dying)
			{
				Vector2 at = NPC.Center;
				if (!Main.dedServ)
					DeathFX(death.Progress);
				if (death.Tick(NPC, DustID.Snow, new Color(170, 225, 255)) && !Main.dedServ)
					DeathBurst(at);
				return;
			}

			NPC.TargetClosest(false);
			Player target = Main.player[NPC.target];
			if (!target.active || target.dead || NPC.Distance(target.Center) > 4200f)
			{
				NPC.velocity.X *= 0.9f;
				NPC.EncourageDespawn(60);
				return;
			}

			if (Phase == 0f && NPC.life <= NPC.lifeMax * PhaseTwoAt)
				EnterPhaseTwo();
			if (Phase == 1f)
				Blizzard();

			if (leapCooldown > 0)
				leapCooldown--;

			Timer++;
			if (Timer <= 0f)
			{
				Walk(target, Phase == 0f ? 2.2f : 2.8f);
				return;
			}

			// the slam and the walls run along its own floor: started from under a player on a
			// platform they would miss by design. So it climbs up first - holding the move for up to
			// a second and a half while it walks and jumps - and only then commits to it.
			if (Timer == 1f && (Move == Slam || Move == Walls) && holdTicks < 90
				&& NPC.Bottom.Y - target.Bottom.Y > 60f)
			{
				holdTicks++;
				Timer = 0f;
				Walk(target, Phase == 0f ? 2.2f : 2.8f);
				return;
			}

			switch (Move)
			{
				case Slam: DoSlam(target); break;
				case Throw: DoThrow(target); break;
				case Icefall: DoIcefall(target); break;
				case Walls: DoWalls(target); break;
			}
		}

		private void Face(Player target)
			=> NPC.direction = NPC.spriteDirection = target.Center.X >= NPC.Center.X ? 1 : -1;

		/// <summary>
		/// Ground movement between attacks. It steps up ledges, and leaps when the player is well
		/// above it or far off - a ground boss that cannot follow is not a fight.
		/// </summary>
		private void Walk(Player target, float top)
		{
			Face(target);
			float dx = target.Center.X - NPC.Center.X;
			if (Math.Abs(dx) > 700f)
				top *= 1.6f;
			NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X + Math.Sign(dx) * 0.1f, -top, top);

			bool onGround = NPC.velocity.Y == 0f;
			if (!onGround)
				return;
			float above = NPC.Bottom.Y - target.Bottom.Y;
			if (leapCooldown <= 0 && above > 40f && Math.Abs(dx) < 420f)
			{
				// a platform is no shelf: it jumps exactly high enough to stand on the player's level,
				// passing up through platforms on the way
				float h = Math.Min(above + 40f, 380f);
				float vy = -MathF.Sqrt(2f * 0.3f * h);
				NPC.velocity.Y = vy;
				NPC.velocity.X = MathHelper.Clamp(dx / (-vy / 0.3f + 10f), -6f, 6f);
				leapCooldown = 50;
				NPC.netUpdate = true;
				SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.9f, Volume = 0.5f }, NPC.Bottom);
			}
			else if (NPC.collideX)
				NPC.velocity.Y = -6.4f;
			else if (leapCooldown <= 0 && Math.Abs(dx) > 900f)
			{
				NPC.velocity.Y = -12.5f;
				NPC.velocity.X = Math.Sign(dx) * 5.5f;
				leapCooldown = 90;
				NPC.netUpdate = true;
			}

			if (Math.Abs(NPC.velocity.X) > 1f && Main.rand.NextBool(3))
				Dust.NewDustPerfect(new Vector2(NPC.Center.X + Main.rand.NextFloat(-40f, 40f), NPC.Bottom.Y - 2f),
					DustID.Snow, new Vector2(-NPC.velocity.X * 0.2f, -Main.rand.NextFloat(0.4f, 1.6f)), 100, default, 1f);
		}

		private void NextMove()
		{
			Timer = -Gap;
			Step++;
			holdTicks = 0;
			NPC.netUpdate = true;
		}

		private void EnterPhaseTwo()
		{
			Phase = 1f;
			Timer = -30f;
			Step = 0f;
			Wind = 0f;
			NPC.netUpdate = true;
			SoundEngine.PlaySound(SoundID.Roar with { Pitch = -0.5f }, NPC.Center);
			if (Main.netMode != NetmodeID.Server)
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(
					NPC.Center, Main.rand.NextVector2Unit(), 12f, 6f, 30, 1600f, "Rime"));
			for (int i = 0; i < 25; i++)
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Snow,
					Main.rand.NextFloat(-7f, 7f), Main.rand.NextFloat(-7f, 3f), 60, default, 1.6f);
				d.noGravity = true;
			}
			if (!Main.dedServ)
			{
				// the roar that calls the storm: rings off the head, the ground slammed, snow torn off it
				RimeFX.Roar(Eye, new Vector2(NPC.spriteDirection, 0.15f), 1.6f);
				ParticleSystem.Spawn(new RimeRing(NPC.Center, RimeFX.Pale, 0.5f, 7f, 50) { Delay = 4 });
				ParticleSystem.Spawn(new RimeBloom(NPC.Center, RimeFX.Frost, 2f, 5f, 30));
				RimeFX.FloorSlam(new Vector2(NPC.Center.X, NPC.Bottom.Y), 1.4f);
				for (int i = 0; i < 40; i++)
				{
					Vector2 v = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 11f);
					ParticleSystem.Spawn(new RimeStreak(NPC.Center + v * 4f, v, RimeFX.Snow * 0.8f, Main.rand.NextFloat(0.6f, 1.1f),
						Main.rand.Next(20, 36)) { Wobble = 0f });
				}
			}
			if (Main.netMode != NetmodeID.MultiplayerClient)
				ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Mods.Terrapex.Chat.RimePhase2"),
					new Color(170, 225, 255));
		}

		/// <summary>The wind's direction now: +1 blows right.</summary>
		private int WindDir => ((int)Wind / WindPeriod) % 2 == 0 ? 1 : -1;

		private bool WindCalm => (int)Wind % WindPeriod >= WindPeriod - WindWarn;

		/// <summary>
		/// Phase two's blizzard. It runs on every machine, but it only ever pushes the local
		/// player - movement is the client's, and a push decided on the server would rubber-band.
		/// The push is small enough that standing on the ground holds you in place (friction beats
		/// it); it is the air and the run where you feel it. The last <see cref="WindWarn"/> ticks
		/// of each direction are calm, the snow slows and swirls, and chevrons point the new way.
		/// </summary>
		private void Blizzard()
		{
			Wind++;
			if (Main.netMode == NetmodeID.Server)
				return;

			int dir = WindDir;
			Player me = Main.LocalPlayer;
			if (!WindCalm && me.active && !me.dead && me.Distance(NPC.Center) < 2600f && me.velocity.X * dir < 5f)
				me.velocity.X += dir * WindPush;

			if (me.Distance(NPC.Center) < 2600f)
				StormFX(dir, WindCalm);
		}

		/// <summary>
		/// The storm you see: a cold veil and fog banks (<see cref="RimeBlizzardFog"/>), snow driven
		/// across the screen as streaks, bigger flakes tumbling with them, and now and then a gust
		/// of brighter air. In the calm it all slows to flakes swirling down, and wind streaks start
		/// running round the player the way the next wind will blow. Spawns are capped per tick and
		/// only fill the visible part of the screen.
		/// </summary>
		private void StormFX(int dir, bool calm)
		{
			RimeBlizzardFog.Feed(dir, calm);
			float zoom = Math.Max(1f, Main.GameViewMatrix.Zoom.X);
			Vector2 full = new(Main.screenWidth, Main.screenHeight);
			Vector2 size = full / zoom;
			Vector2 view = Main.screenPosition + (full - size) / 2f;
			Vector2 Anywhere(float margin) => view + new Vector2(Main.rand.NextFloat(-margin, size.X + margin), Main.rand.NextFloat(-40f, size.Y));

			if (!calm)
			{
				for (int i = 0; i < 4; i++)
				{
					var s = ParticleSystem.Spawn(new RimeStreak(Anywhere(80f), new Vector2(dir * Main.rand.NextFloat(9f, 14f), Main.rand.NextFloat(1f, 3f)),
						RimeFX.Snow * Main.rand.NextFloat(0.45f, 0.8f), Main.rand.NextFloat(0.6f, 1.2f), Main.rand.Next(30, 55)));
					s.Length = 1.6f;
				}
				ParticleSystem.Spawn(new RimeFlake(Anywhere(60f), new Vector2(dir * Main.rand.NextFloat(5f, 8f), Main.rand.NextFloat(0.5f, 1.5f)),
					RimeFX.Snow, Main.rand.NextFloat(0.5f, 0.85f), Main.rand.Next(50, 80)) { Sway = 0.03f, Spin = dir * 0.12f });
				if (Main.rand.NextBool(22))
					ParticleSystem.Spawn(new RimeGust(view + new Vector2(dir > 0 ? -150f : size.X + 150f, Main.rand.NextFloat(0.15f, 0.85f) * size.Y),
						new Vector2(dir * Main.rand.NextFloat(18f, 24f), Main.rand.NextFloat(-0.5f, 0.5f)), Main.rand.NextFloat(1f, 1.6f), 90));
			}
			else
			{
				for (int i = 0; i < 2; i++)
					ParticleSystem.Spawn(new RimeFlake(Anywhere(40f), new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(0.3f, 0.9f)),
						RimeFX.Snow, Main.rand.NextFloat(0.4f, 0.8f), Main.rand.Next(90, 130)) { Sway = 0.05f });
				// wind already gathering round the player, the way it is about to blow
				if (Main.rand.NextBool(3))
				{
					Vector2 at = Main.LocalPlayer.Center + new Vector2(Main.rand.NextFloat(-90f, 90f), Main.rand.NextFloat(-110f, 30f));
					var s = ParticleSystem.Spawn(new RimeStreak(at, new Vector2(-dir * Main.rand.NextFloat(3f, 5f), 0f), RimeFX.Pale * 0.55f,
						Main.rand.NextFloat(0.7f, 1f), Main.rand.Next(20, 30)));
					s.Length = 4f;
					s.Wobble = 0.02f;
				}
			}
		}

		// ------------------------------------------------------------------ the moves

		/// <summary>
		/// Crouch, slam, and a line of ice erupts along the floor both ways. The cracks it will
		/// run along are drawn out from its feet for the whole crouch. Phase two slams twice.
		/// </summary>
		private void DoSlam(Player target)
		{
			NPC.velocity.X *= 0.8f;
			if (Timer == 1f)
				Face(target);
			if (!Main.dedServ)
				SlamTellFX();
			if (Timer == SlamTell + 1 || (Phase == 1f && Timer == SlamTell + SlamSecond + 1))
				SlamImpact();
			if (Timer >= SlamEnd)
				NextMove();
		}

		/// <summary>
		/// The crouch, seen: rime crystals grow out of the floor where the crack has just reached and
		/// stand until the wave arrives at them, then break - so the row of crystals is the wave's
		/// schedule. Cold breath leaks out of it, and the crack's head spits frost.
		/// </summary>
		private void SlamTellFX()
		{
			float f = SlamTellF();
			if (f < 0f)
			{
				crackReached = 0f;
				return;
			}
			float reach = SlamReach(f);
			if (reach < crackReached)
				crackReached = 0f;
			bool second = Timer > SlamTell;
			int slamAt = second ? SlamTell + SlamSecond + 1 : SlamTell + 1;
			float y = NPC.Bottom.Y;
			for (int side = -1; side <= 1; side += 2)
			{
				for (float d = 40f; d <= reach; d += 56f)
				{
					if (d <= crackReached)
						continue;
					Vector2 foot = new(NPC.Center.X + side * (48f + d), y + 1f);
					if (!RimeFX.Floor(foot + new Vector2(0f, 6f)))
						continue;
					float arrive = Math.Max(0f, (48f + d - 56f) / ShockSpeed);
					int life = Math.Max(8, (int)(slamAt - Timer + arrive) + 1);
					ParticleSystem.Spawn(new RimeSpike(foot, Main.rand.NextFloat(0.6f, 0.9f), Main.rand.NextFloat(0.6f, 1.1f), life,
						side * Main.rand.NextFloat(0f, 0.22f)) { GrowTicks = 10f });
				}
				if (Main.rand.NextBool(2))
				{
					Vector2 tip = new(NPC.Center.X + side * (48f + reach), y);
					ParticleSystem.Spawn(new Spark(tip, new Vector2(side * Main.rand.NextFloat(0.5f, 2f), -Main.rand.NextFloat(1f, 3f)),
						RimeFX.Frost, 0.7f, 16, 0.08f));
				}
			}
			crackReached = reach;
			if ((int)Timer % 9 == 0)
				RimeFX.Breath(Mouth, new Vector2(NPC.spriteDirection, 0.35f), 2, 0.6f);
		}

		private void SlamImpact()
		{
			SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.4f }, NPC.Center);
			if (Main.netMode != NetmodeID.Server)
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(
					NPC.Center, Vector2.UnitY, 10f, 6f, 20, 1600f, "Rime"));
			for (int i = 0; i < 14; i++)
				Dust.NewDustPerfect(new Vector2(NPC.Center.X + Main.rand.NextFloat(-60f, 60f), NPC.Bottom.Y - 2f),
					DustID.Snow, new Vector2(Main.rand.NextFloat(-5f, 5f), -Main.rand.NextFloat(0.5f, 4f)), 100, default, 1.3f);
			if (!Main.dedServ)
			{
				Vector2 floor = new(NPC.Center.X + NPC.spriteDirection * 30f, NPC.Bottom.Y);
				RimeFX.FloorSlam(floor, 1.3f);
				RimeFX.Breath(Mouth, new Vector2(NPC.spriteDirection, 0.5f), 8, 1.2f);
				ParticleSystem.Spawn(new RimeBloom(floor, RimeFX.Pale, 0.8f, 2.4f, 12, 0.5f));
			}

			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			int damage = NPC.GetAttackDamage_ForProjectiles(ShockDamage, ShockDamage);
			for (int side = -1; side <= 1; side += 2)
				Projectile.NewProjectile(NPC.GetSource_FromAI(), new Vector2(NPC.Center.X + side * 56f, NPC.Bottom.Y - 24f),
					new Vector2(side * ShockSpeed, 0f), ModContent.ProjectileType<RimeShockwave>(), damage, 6f, Main.myPlayer);
		}

		/// <summary>
		/// The block goes up, an arc and a landing mark are drawn to where the player stood, and it
		/// is thrown along exactly that arc. It shatters on the mark into a straight fan of shards.
		/// Then it grows back in the giant's hands.
		/// </summary>
		private void DoThrow(Player target)
		{
			NPC.velocity.X *= 0.8f;
			if (Timer == 1f)
			{
				Face(target);
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					throwAt = target.Center;
					NPC.netUpdate = true;
				}
			}
			if (!Main.dedServ && Timer <= ThrowTell)
				ThrowTellFX(Timer / ThrowTell);
			if (Timer == ThrowTell + 1)
			{
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.8f, Volume = 1.2f }, NPC.Center);
				if (!Main.dedServ)
				{
					Vector2 hands = BlockWorld(FrameRaise);
					ParticleSystem.Spawn(new RimeRing(hands, RimeFX.Pale, 0.3f, 1.8f, 18));
					RimeFX.Breath(Mouth, new Vector2(NPC.spriteDirection, -0.2f), 6, 1f);
					for (int i = 0; i < 8; i++)
						ParticleSystem.Spawn(new Chip(hands + Main.rand.NextVector2Circular(18f, 18f), Main.rand.NextVector2Circular(3f, 3f),
							RimeFX.Snow, Main.rand.NextFloat(0.8f, 1.3f), Main.rand.Next(30, 50)));
				}
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					Vector2 from = BlockWorld(FrameRaise);
					Projectile.NewProjectile(NPC.GetSource_FromAI(), from, RimeBoulder.Launch(from, throwAt),
						ModContent.ProjectileType<RimeBoulder>(),
						NPC.GetAttackDamage_ForProjectiles(BoulderDamage, BoulderDamage), 6f, Main.myPlayer,
						Phase == 0f ? 7f : 9f, 0f, NPC.GetAttackDamage_ForProjectiles(ShardDamage, ShardDamage));
				}
			}
			if (Timer >= ThrowEnd)
				NextMove();
		}

		/// <summary>
		/// The block held overhead frosts over: vapour boiling off it, snow falling off its sides,
		/// glints as it hardens. Flakes drift off the drawn arc, and the mark's floor rimes over.
		/// </summary>
		private void ThrowTellFX(float f)
		{
			Vector2 block = BlockWorld(FrameRaise);
			if (Main.rand.NextFloat() < 0.4f + 0.4f * f)
				ParticleSystem.Spawn(new Smoke(block + Main.rand.NextVector2Circular(20f, 16f), new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(0.4f, 1.2f)),
					RimeFX.Pale * 0.3f, Main.rand.NextFloat(0.3f, 0.5f), Main.rand.Next(30, 46), glow: true));
			if (Main.rand.NextBool(3))
				ParticleSystem.Spawn(new RimeFlake(block + new Vector2(Main.rand.NextFloat(-22f, 22f), 14f), new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), 0.6f),
					RimeFX.Snow, Main.rand.NextFloat(0.4f, 0.65f), Main.rand.Next(40, 60)) { Gravity = 0.03f, Sway = 0.03f });
			if (f > 0.4f && Main.rand.NextBool(8))
				ParticleSystem.Spawn(new RimeGlint(block + Main.rand.NextVector2Circular(18f, 16f), RimeFX.Pale, 0.7f, 14));

			if (throwAt == Vector2.Zero)
				return;
			if (Main.rand.NextBool(3))
			{
				Vector2 v = RimeBoulder.Launch(block, throwAt);
				int n = Main.rand.Next(3, RimeBoulder.Flight);
				Vector2 p = block + v * n + new Vector2(0f, RimeBoulder.Gravity * n * (n + 1) / 2f);
				ParticleSystem.Spawn(new RimeFlake(p, new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), 0.35f), RimeFX.Frost,
					Main.rand.NextFloat(0.35f, 0.5f), 40, glow: true) { Sway = 0.02f });
			}
			if (Main.rand.NextBool(4))
				ParticleSystem.Spawn(new RimeGlint(throwAt + Main.rand.NextVector2CircularEdge(1f, 1f) * MathHelper.Lerp(70f, 34f, f),
					RimeFX.Pale, 0.5f, 12));
		}

		/// <summary>How much of the block is in its hands: gone after the throw, growing back at the end.</summary>
		private float BlockScale
		{
			get
			{
				if (Timer <= 0f || Move != Throw || Timer <= ThrowTell)
					return 1f;
				return MathHelper.Clamp((Timer - (ThrowEnd - 24)) / 24f, 0f, 1f);
			}
		}

		/// <summary>
		/// A roar, and icicles hang off the ceiling (or the top of the sky) in evenly spaced
		/// columns over the player, each glinting and throwing its shadow on the floor before it
		/// drops. Phase two hangs a second set between the first.
		/// </summary>
		private void DoIcefall(Player target)
		{
			NPC.velocity.X *= 0.85f;
			if (Timer == 1f)
			{
				Face(target);
				SoundEngine.PlaySound(SoundID.Roar with { Pitch = -0.7f, Volume = 0.8f }, NPC.Center);
				if (!Main.dedServ)
					RimeFX.Roar(Eye, new Vector2(NPC.spriteDirection, -0.4f), 1f);
			}
			// the roar shakes the ceiling: snow comes loose over the player before anything hangs
			if (!Main.dedServ && Timer > 1f && Timer < RoarTell && Main.rand.NextBool(2))
			{
				float x = target.Center.X + Main.rand.NextFloat(-1f, 1f) * IcefallSpacing * 4f;
				RimeFX.Sift(new Vector2(x, CeilingAbove(new Vector2(x, target.Center.Y))), 10f);
			}
			if (!Main.dedServ && Phase == 1f && Timer == RoarTell + IcefallSecond - 16)
				RimeFX.Roar(Eye, new Vector2(NPC.spriteDirection, -0.4f), 0.8f);
			if (Timer == RoarTell)
				Columns(target, 0f, Phase == 0f ? IcefallColumnsOne : IcefallColumnsTwo);
			if (Phase == 1f && Timer == RoarTell + IcefallSecond)
				Columns(target, 0.5f, IcefallColumnsTwo - 1);
			if (Timer >= IcefallEnd)
				NextMove();
		}

		private void Columns(Player target, float shift, int count)
		{
			SoundEngine.PlaySound(SoundID.Item30 with { Pitch = -0.3f }, target.Center);
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			int damage = NPC.GetAttackDamage_ForProjectiles(IcicleDamage, IcicleDamage);
			for (int i = 0; i < count; i++)
			{
				float x = target.Center.X + (i - (count - 1) / 2f + shift) * IcefallSpacing;
				Vector2 top = new(x, CeilingAbove(new Vector2(x, target.Center.Y)));
				Projectile.NewProjectile(NPC.GetSource_FromAI(), top, Vector2.Zero,
					ModContent.ProjectileType<RimeIcicle>(), damage, 3f, Main.myPlayer);
			}
		}

		/// <summary>
		/// A stamp, and two walls of ice come up out of the floor either side of the player and
		/// close in. One of them has a door, drawn from its first frame; go through it, or over
		/// the walls, or take the hit. It plods after you while they close.
		/// </summary>
		private void DoWalls(Player target)
		{
			if (Timer == 1f)
				Face(target);
			if (Timer <= WallTell)
				NPC.velocity.X *= 0.8f;
			else
				Walk(target, 1.2f);

			if (Timer == WallTell)
			{
				SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.6f }, NPC.Center);
				if (!Main.dedServ)
				{
					// the stamp: the floor rimes under its foot and a ring runs out toward the player
					Vector2 foot = new(NPC.Center.X + NPC.spriteDirection * 30f, NPC.Bottom.Y);
					RimeFX.FloorSlam(foot, 0.9f);
					ParticleSystem.Spawn(new RimeRing(foot, RimeFX.Frost, 0.5f, 9f, 40, 0.15f));
				}
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					float floor = FloorBelow(target.Center);
					int doorSide = Main.rand.NextBool() ? -1 : 1;
					float lift = Main.rand.NextBool() ? 0f : 40f;
					int damage = NPC.GetAttackDamage_ForProjectiles(WallDamage, WallDamage);
					for (int side = -1; side <= 1; side += 2)
					{
						Vector2 at = new(target.Center.X + side * WallStart, floor - RimeWall.Height / 2f);
						Projectile.NewProjectile(NPC.GetSource_FromAI(), at, Vector2.Zero,
							ModContent.ProjectileType<RimeWall>(), damage, 5f, Main.myPlayer,
							-side * WallSpeed, side == doorSide ? 1f : 0f, lift);
					}
				}
			}
			if (Timer >= WallEnd)
				NextMove();
		}

		// ------------------------------------------------------------------ death and loot

		public override bool CheckDead() => death.CheckDead(NPC, 150, ClearHazards);

		/// <summary>The throes: it frosts over, crystals push out through it and cracks of light run across it.</summary>
		private void DeathFX(float f)
		{
			if (Main.rand.NextFloat() < 0.15f + 0.45f * f)
			{
				Vector2 at = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
				ParticleSystem.Spawn(new RimeSpike(at, Main.rand.NextFloat(0.6f, 1f), Main.rand.NextFloat(0.6f, 1.2f), Main.rand.Next(18, 34),
					Main.rand.NextFloat(-1.2f, 1.2f)) { GrowTicks = 8f, Chips = 2 });
			}
			if ((int)(f * 150f) % 14 == 0)
				FX.Crack(NPC.Center + Main.rand.NextVector2Circular(30f, 30f), Main.rand.NextVector2Unit(),
					Main.rand.NextFloat(30f, 60f) * (0.6f + f), RimeFX.Glow(RimeFX.Pale, 0.9f), 40, 1.6f, 2);
			if (Main.rand.NextBool(3))
				ParticleSystem.Spawn(new Smoke(NPC.Center + Main.rand.NextVector2Circular(50f, 50f), new Vector2(0f, -Main.rand.NextFloat(0.5f, 1.5f)),
					RimeFX.Pale * 0.3f, Main.rand.NextFloat(0.4f, 0.7f), Main.rand.Next(30, 50), glow: true));
			if (Main.rand.NextBool(6))
				ParticleSystem.Spawn(new RimeGlint(NPC.Center + Main.rand.NextVector2Circular(50f, 50f), RimeFX.Pale, 0.8f, 16));
		}

		/// <summary>The end: the whole mountain shatters at once.</summary>
		private static void DeathBurst(Vector2 at)
		{
			RimeFX.Shatter(at, 2.6f, 40);
			ParticleSystem.Spawn(new RimeRing(at, RimeFX.Pale, 0.5f, 8f, 50) { Delay = 4 });
			ParticleSystem.Spawn(new RimeRing(at, RimeFX.Frost, 0.3f, 5f, 40, 0.3f));
			for (int i = 0; i < 16; i++)
				ParticleSystem.Spawn(new RimeIceBit(at + Main.rand.NextVector2Circular(40f, 40f), Main.rand.NextVector2Circular(9f, 9f) + new Vector2(0f, -4f),
					Main.rand.NextFloat(0.8f, 1.4f), Main.rand.Next(60, 90)));
			for (int i = 0; i < 30; i++)
				ParticleSystem.Spawn(new RimeFlake(at, Main.rand.NextVector2Circular(8f, 8f), RimeFX.Pale, Main.rand.NextFloat(0.6f, 1f),
					Main.rand.Next(60, 110), glow: true) { Drag = 0.95f, Gravity = 0.02f, Sway = 0.03f });
		}

		private static void ClearHazards()
		{
			foreach (Projectile p in Main.ActiveProjectiles)
			{
				if (p.type == ModContent.ProjectileType<RimeShockwave>() || p.type == ModContent.ProjectileType<RimeBoulder>()
					|| p.type == ModContent.ProjectileType<RimeShard>() || p.type == ModContent.ProjectileType<RimeIcicle>()
					|| p.type == ModContent.ProjectileType<RimeWall>())
					p.Kill();
			}
		}

		public override bool CheckActive() => false;

		public override void OnKill()
		{
			if (!DownedBossSystem.downedRime)
			{
				DownedBossSystem.downedRime = true;
				if (Main.netMode != NetmodeID.MultiplayerClient)
					ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Mods.Terrapex.Chat.RimeDown"),
						new Color(170, 225, 255));
				if (Main.netMode == NetmodeID.Server)
					NetMessage.SendData(MessageID.WorldData);
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<RimeBag>()));

			LeadingConditionRule notExpert = new(new Conditions.NotExpert());
			notExpert.OnSuccess(ItemDropRule.OneFromOptions(1,
				ModContent.ItemType<Glacierbreaker>(),
				ModContent.ItemType<HoarfrostBow>(),
				ModContent.ItemType<SnowcapStaff>(),
				ModContent.ItemType<GlacierBell>(),
				ModContent.ItemType<FrostfellScythe>()));
			notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<FrostPelt>(), 1, 4, 8));
			notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<RimeMask>(), 7));
			notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<RimeboundWrap>(), 3));
			npcLoot.Add(notExpert);
		}

		public override void BossLoot(ref int potionType) => potionType = ItemID.HealingPotion;

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 30 : 5); i++)
				Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, i % 3 == 0 ? DustID.Ice : DustID.Snow,
					hit.HitDirection * 2f, -1.5f, 90, default, 1.2f);
		}

		// ------------------------------------------------------------------ drawing

		private int PoseFrame()
		{
			if (Timer <= 0f || death.Dying)
				return -1;
			switch (Move)
			{
				case Slam:
					if (Timer <= SlamTell)
						return FrameRaise;
					if (Timer <= SlamTell + 16)
						return FrameSlam;
					if (Phase == 1f && Timer <= SlamTell + SlamSecond)
						return FrameRaise;
					if (Phase == 1f && Timer <= SlamTell + SlamSecond + 16)
						return FrameSlam;
					return 0;
				case Throw:
					if (Timer <= ThrowTell)
						return FrameRaise;
					return Timer <= ThrowTell + 14 ? FrameRoar : 0;
				case Icefall:
					if (Timer <= RoarTell + 24)
						return FrameRoar;
					if (Phase == 1f && Timer > RoarTell + IcefallSecond - 16 && Timer <= RoarTell + IcefallSecond + 8)
						return FrameRoar;
					return 0;
				case Walls:
					if (Timer <= 12)
						return FrameRaise;
					return Timer <= WallTell + 10 ? FrameSlam : -1;
			}
			return -1;
		}

		public override void FindFrame(int frameHeight)
		{
			int f = PoseFrame();
			if (f < 0)
			{
				if (NPC.velocity.Y != 0f)
					f = 1;
				else
				{
					NPC.frameCounter += Math.Abs(NPC.velocity.X) * 0.05 + 0.03;
					if (NPC.frameCounter >= 4.0)
						NPC.frameCounter = 0.0;
					f = (int)NPC.frameCounter;
				}
			}
			NPC.frame.Y = f * frameHeight;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			DrawTells();

			int frame = NPC.frame.Y / Math.Max(1, NPC.frame.Height);
			Vector2 center = DrawCenter;
			SpriteEffects fx = NPC.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

			// the block, behind the body so the fists close over it
			float block = BlockScale;
			if (block > 0f)
			{
				Texture2D bt = TextureAssets.Projectile[ModContent.ProjectileType<RimeBoulder>()].Value;
				Vector2 bw = BlockWorld(frame);
				// frost gathering on it while it is held up to be thrown, and while it grows back
				float frost = !death.Dying && Timer > 0f && Move == Throw
					? (Timer <= ThrowTell ? Timer / ThrowTell : (block < 1f ? 1f - block : 0f))
					: 0f;
				if (frost > 0f)
					RimeFX.Soft(bw, RimeFX.Glow(RimeFX.Frost, 0.3f * frost), 1.3f * block);
				spriteBatch.Draw(bt, bw - screenPos, null, drawColor, 0f, bt.Size() / 2f,
					block, fx, 0f);
				if (frost > 0f)
					spriteBatch.Draw(bt, bw - screenPos, null, RimeFX.Glow(RimeFX.Pale, 0.35f * frost), 0f, bt.Size() / 2f,
						block, fx, 0f);
			}

			Texture2D body = TextureAssets.Npc[Type].Value;
			spriteBatch.Draw(body, center - screenPos, NPC.frame, drawColor, NPC.rotation, NPC.frame.Size() / 2f, 1f, fx, 0f);

			// the eye: it burns brighter for every tell
			bool telling = PoseFrame() == FrameRaise || PoseFrame() == FrameRoar;
			Vector2 eye = center + new Vector2(38f * NPC.spriteDirection, -10f);
			RimeFX.Soft(eye, RimeFX.Glow(new Color(255, 200, 90), telling ? 0.7f : 0.25f), telling ? 0.4f : 0.22f);
			if (telling)
				RimeFX.Star(eye, RimeFX.Glow(new Color(255, 236, 170), 0.8f), 0.7f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 10f));
			if (death.Dying)
				RimeFX.Soft(center, RimeFX.Glow(RimeFX.Frost, 0.2f + 0.5f * death.Progress), 2.2f);
			return false;
		}

		/// <summary>Everything the player is told before it hurts: the cracks, the arc and its mark, the wind's turn.</summary>
		private void DrawTells()
		{
			if (death.Dying)
				return;

			// the slam: frost cracks run out along the floor from its feet, as far as the wave will go
			float sf = SlamTellF();
			if (sf >= 0f)
			{
				float reach = SlamReach(sf);
				for (int side = -1; side <= 1; side += 2)
					RimeFX.FloorCrack(new Vector2(NPC.Center.X + side * 48f, NPC.Bottom.Y - 1f), side, reach, sf, CrackSeed(side));
			}

			// the throw: the arc it will fly, as snowflakes drifting on it, and the mark it lands on
			if (Timer > 0f && Move == Throw && Timer <= ThrowTell && throwAt != Vector2.Zero)
			{
				float f = Timer / ThrowTell;
				float t = Main.GlobalTimeWrappedHourly;
				Vector2 from = BlockWorld(FrameRaise);
				Vector2 v = RimeBoulder.Launch(from, throwAt);
				Vector2 prev = from;
				for (int n = 3; n < RimeBoulder.Flight; n += 3)
				{
					float along = n / (float)RimeBoulder.Flight;
					// the markers come in from the hands to the mark over the first sixth of the hold
					float shown = MathHelper.Clamp((f - along * 0.12f) / 0.05f, 0f, 1f);
					Vector2 p = from + v * n + new Vector2(0f, RimeBoulder.Gravity * n * (n + 1) / 2f);
					Vector2 bob = new((float)Math.Sin(t * 1.7f + n) * 1.5f, (float)Math.Sin(t * 2.3f + n * 0.7f) * 2f);
					RimeFX.Line(prev, p, RimeFX.Glow(RimeFX.Frost, (0.05f + 0.07f * f) * shown), 2f);
					RimeFX.Soft(p + bob, RimeFX.Glow(RimeFX.Frost, (0.12f + 0.2f * f) * shown), 0.2f);
					RimeFX.Flake(p + bob, RimeFX.Glow(RimeFX.Pale, (0.35f + 0.5f * f) * shown), 0.7f + 0.35f * along,
						t * 1.2f + n * 0.5f);
					prev = p;
				}
				DrawMark(throwAt, f);
			}

			// the wind's turn: chevrons over the local player, swept along the way it will blow
			if (Phase == 1f && WindCalm && Main.netMode != NetmodeID.Server)
			{
				float f = ((int)Wind % WindPeriod - (WindPeriod - WindWarn)) / (float)WindWarn;
				int next = -WindDir;
				Vector2 over = Main.LocalPlayer.Center + new Vector2(0f, -78f);
				float t = Main.GlobalTimeWrappedHourly;
				for (int i = -1; i <= 1; i++)
				{
					// each chevron slides a little the way it points, and fades as it goes, then again
					float run = (t * 1.6f + (i + 1) / 3f) % 1f;
					Vector2 tip = over + new Vector2(i * 16f + next * (6f + run * 6f), 0f);
					float a = (0.3f + 0.5f * f) * (0.65f + 0.35f * (float)Math.Sin(run * MathHelper.Pi));
					Color haze = RimeFX.Glow(RimeFX.Frost, a * 0.45f);
					Color core = RiftDraw.Glow(220, 240, 255, a);
					Vector2 up = tip + new Vector2(-next * 8f, -8f), down = tip + new Vector2(-next * 8f, 8f);
					RimeFX.Line(tip, up, haze, 5f);
					RimeFX.Line(tip, down, haze, 5f);
					RimeFX.Taper(tip, up, core, 2.4f);
					RimeFX.Taper(tip, down, core, 2.4f);
					// the wind trailing off behind it
					RimeFX.Taper(tip, tip - new Vector2(next * (14f + 10f * f), 0f), RimeFX.Glow(RimeFX.Pale, a * 0.5f), 1.5f);
					RimeFX.Star(tip, RimeFX.Glow(RimeFX.Pale, a * 0.6f), 0.35f);
				}
			}
		}

		/// <summary>
		/// A landing mark: rime closing in on a point. A ring of crystals pointing inward shrinks onto
		/// it, frost fills the circle, and a snowflake over a thin X marks the centre. The floor
		/// under it, when there is one near, frosts over in step.
		/// </summary>
		public static void DrawMark(Vector2 at, float f)
		{
			float r = MathHelper.Lerp(70f, 34f, f);
			float t = Main.GlobalTimeWrappedHourly;
			RimeFX.Soft(at, RimeFX.Glow(RimeFX.Frost, 0.08f + 0.16f * f), r / 32f);
			RimeFX.Ring(at, RimeFX.Glow(RimeFX.Frost, 0.25f + 0.45f * f), r / 28f);
			RimeFX.Ring(at, RimeFX.Glow(RimeFX.Pale, 0.15f + 0.3f * f), r / 28f * 0.92f, 1f, t);
			for (int i = 0; i < 12; i++)
			{
				float ang = MathHelper.TwoPi * i / 12f + f * 0.8f;
				Vector2 p = at + ang.ToRotationVector2() * (r + 4f);
				RimeFX.Crystal(p, 0.55f, 0.45f + 0.35f * f, ang - MathHelper.PiOver2, 0.35f + 0.4f * f);
			}
			Color c = RiftDraw.Glow(150, 225, 255, 0.3f + 0.6f * f);
			float s = 10f + 6f * f;
			RiftDraw.Line(at + new Vector2(-s, -s), at + new Vector2(s, s), c, 2f);
			RiftDraw.Line(at + new Vector2(-s, s), at + new Vector2(s, -s), c, 2f);
			RimeFX.Flake(at, RimeFX.Glow(RimeFX.Pale, 0.4f + 0.5f * f), 1.1f + 0.7f * f, f * 2f);

			float floor = FloorBelow(at, 15);
			if (floor - at.Y < 240f)
			{
				Vector2 g = new(at.X, floor);
				RimeFX.Soft(g, RimeFX.Glow(RimeFX.Frost, 0.12f + 0.3f * f), r * 1.3f / 32f, 0.18f);
				RimeFX.Line(g - new Vector2(r * f, 1f), g + new Vector2(r * f, -1f), RimeFX.Glow(RimeFX.Pale, 0.25f + 0.4f * f), 2f);
			}
		}
	}
}
