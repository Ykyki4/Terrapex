using System;
using System.Collections.Generic;
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
using Terrapex.Content.Items.Weapons.Shellback;
using Terrapex.Content.Projectiles.Shellback;
using SFX = Terrapex.Common.Particles.ShellbackFX;

namespace Terrapex.Content.NPCs.Bosses
{
	/// <summary>
	/// The Old Shellback - an ancient hermit crab that took a sunken ship's hull for its shell.
	/// The mod's ocean boss, after the Eye of Cthulhu. See <c>BOSS_SHELLBACK.md</c>; the numbers
	/// there and here are kept in step.
	///
	/// It walks the beach. Four moves on a fixed rotation, each with a drawn tell: a sideways
	/// scuttle along a line laid on the sand, a straight fan of bubbles at where the player
	/// stood, cannonballs lobbed from the wreck along marked arcs, and the Hull - it pulls into
	/// the wreck, takes half damage, and either gets cracked open by a player who keeps hitting
	/// it or comes down in a telegraphed slam that leaves it stunned. The Hull is a dial, not a
	/// gate: the player picks whether to spend the hide hitting a shell or dodging the slam,
	/// and both end in a window.
	///
	/// OldShellback.png is twelve frames of 120x88, stacked: 0-3 walking, 4-7 walking with the
	/// mast snapped (phase two), 8/9 in the shell, 10/11 stunned. The sprite faces right.
	/// </summary>
	[AutoloadBossHead]
	public class OldShellback : ModNPC
	{
		public const float PhaseTwoAt = 0.5f;

		public const int BubbleDamage = 16;
		public const int CannonDamage = 22;
		public const int WaveDamage = 18;

		/// <summary>What a hit is worth while it hides in the hull, and while it lies stunned.</summary>
		public const float HiddenTaken = 0.5f, StunnedTaken = 1.3f;

		/// <summary>Damage dealt into the hull, as a share of max life, that cracks it open early.</summary>
		public const float CrackShare = 0.025f;

		public const float ChargeSpeed = 12f;
		public const int ChargeTicks = 45;
		public const float CannonGravity = 0.25f;
		public const int CannonFlight = 60;

		private const int Scuttle = 0, Bubbles = 1, Cannon = 2, Hull = 3;
		private static readonly int[] RotationOne = { Scuttle, Bubbles, Cannon, Scuttle, Bubbles, Hull };
		private static readonly int[] RotationTwo = { Scuttle, Cannon, Bubbles, Hull, Scuttle, Cannon, Bubbles };

		// the Hull's own steps
		private const int Hiding = 0, SlamTell = 1, Airborne = 2, Stunned = 3, Cracked = 4;
		public const int HideTicks = 150, HideTicksTwo = 120, SlamTellTicks = 40;
		public const int StunTicks = 60, CrackTicks = 100;

		private const int FrameW = 120, FrameH = 88;

		private ref float Timer => ref NPC.ai[0];
		private ref float Step => ref NPC.ai[1];
		private ref float Phase => ref NPC.ai[2];
		/// <summary>The scuttle's committed direction, -1 or 1.</summary>
		private ref float AimDir => ref NPC.ai[3];

		// synced through SendExtraAI
		private int sub;
		private int charges;
		private int hideLife;
		private float slamX;
		/// <summary>The floor under the player when the slam was marked - a platform counts.</summary>
		private float slamY;
		private int hopCooldown;
		private int holdTicks;
		private readonly Vector2[] cannonTargets = new Vector2[3];
		private int cannonCount;

		private int walkFrame;
		private readonly BossDeath death = new();

		// cosmetic and per client: nothing below is synced, and no gameplay reads it
		/// <summary>This charge already threw its wall-impact burst.</summary>
		private bool rammed;
		/// <summary>Cracks on the hull, as polylines in sprite-facing-right space from the sprite's bottom centre.</summary>
		private readonly List<Vector2[]> hullCracks = new();
		private int crackStage;
		private const int CrackStages = 7;

		private int[] Rotation => Phase == 0f ? RotationOne : RotationTwo;
		private int Move => Rotation[(int)Step % Rotation.Length];
		private int Pause => Phase == 0f ? 50 : 30;
		private bool InShell => Move == Hull && Timer > 0f && sub <= Airborne;
		private bool Down => Move == Hull && Timer > 0f && (sub == Stunned || sub == Cracked);
		private int CrackAt => Math.Max(1, (int)(NPC.lifeMax * CrackShare));

		private int ScuttleTell => Phase == 0f ? 40 : 30;
		private int BubbleTell => Phase == 0f ? 24 : 20;
		private const int CannonTell = 50;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 12;
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			NPCID.Sets.BossBestiaryPriority.Add(Type);
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
		}

		public override void SetDefaults()
		{
			NPC.width = 104;
			NPC.height = 62;
			NPC.damage = 26;
			NPC.defense = 10;
			NPC.lifeMax = 3800;
			NPC.HitSound = SoundID.NPCHit42;
			NPC.DeathSound = SoundID.NPCDeath5;
			NPC.knockBackResist = 0f;
			NPC.value = Item.buyPrice(gold: 3);
			NPC.aiStyle = -1;
			NPC.boss = true;
			NPC.npcSlots = 8f;
			NPC.noGravity = false;
			NPC.noTileCollide = false;
			Music = MusicID.Boss1;
		}

		public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
		{
			NPC.lifeMax = (int)(NPC.lifeMax * 0.7f * balance);
			NPC.damage = (int)(NPC.damage * 0.85f);
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
			=> bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Ocean,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.OldShellback.Bestiary")
			});

		public override void SendExtraAI(BinaryWriter writer)
		{
			writer.Write(sub);
			writer.Write(charges);
			writer.Write(hideLife);
			writer.Write(slamX);
			writer.Write(slamY);
			writer.Write(cannonCount);
			for (int i = 0; i < cannonTargets.Length; i++)
				writer.WriteVector2(cannonTargets[i]);
		}

		public override void ReceiveExtraAI(BinaryReader reader)
		{
			sub = reader.ReadInt32();
			charges = reader.ReadInt32();
			hideLife = reader.ReadInt32();
			slamX = reader.ReadSingle();
			slamY = reader.ReadSingle();
			cannonCount = reader.ReadInt32();
			for (int i = 0; i < cannonTargets.Length; i++)
				cannonTargets[i] = reader.ReadVector2();
		}

		// ------------------------------------------------------------------ AI

		public override void AI()
		{
			if (death.Dying)
			{
				// the timing is BossDeath's; the wreck coming apart on top of it is ours
				Vector2 centre = NPC.Center, bottom = NPC.Bottom;
				DeathThroes(death.Progress);
				if (death.Tick(NPC, DustID.Water, new Color(120, 220, 210)))
					DeathBurst(centre, bottom);
				return;
			}

			NPC.TargetClosest();
			Player target = Main.player[NPC.target];
			if (!target.active || target.dead || NPC.Distance(target.Center) > 4000f)
			{
				// it goes back to sea: walks off and is gone
				NPC.velocity.X *= 0.95f;
				NPC.EncourageDespawn(30);
				return;
			}

			if (Phase == 0f && NPC.life <= NPC.lifeMax * PhaseTwoAt && !InShell)
				EnterPhaseTwo();

			Timer++;

			if (hopCooldown > 0)
				hopCooldown--;

			// the pause between moves: it walks at the player
			if (Timer <= 0f)
			{
				Face(target);
				Walk(target, Phase == 0f ? 2.2f : 3.0f);
				return;
			}

			// the scuttle runs along its own floor: from under a player on a platform it would
			// miss by design, so it climbs up first - up to a second and a half - then commits
			if (Timer == 1f && Move == Scuttle && holdTicks < 90 && NPC.Bottom.Y - target.Bottom.Y > 60f)
			{
				holdTicks++;
				Timer = 0f;
				Face(target);
				Walk(target, Phase == 0f ? 2.2f : 3.0f);
				return;
			}

			switch (Move)
			{
				case Scuttle: DoScuttle(target); break;
				case Bubbles: DoBubbles(target); break;
				case Cannon: DoCannon(target); break;
				case Hull: DoHull(target); break;
			}
		}

		private void Face(Player target)
			=> NPC.spriteDirection = NPC.direction = target.Center.X >= NPC.Center.X ? 1 : -1;

		/// <summary>Along the sand toward the player, hopping steps, leaping when left far behind.</summary>
		private void Walk(Player target, float speed)
		{
			float dx = target.Center.X - NPC.Center.X;
			if (Math.Abs(dx) > 90f)
				NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, Math.Sign(dx) * speed, 0.08f);
			else
				NPC.velocity.X *= 0.9f;

			bool onGround = NPC.velocity.Y == 0f;
			if (!onGround)
				return;
			float above = NPC.Bottom.Y - target.Bottom.Y;
			if (above > 40f && Math.Abs(dx) < 420f && hopCooldown <= 0)
			{
				// a platform is no shelf: it hops exactly high enough to land on the player's level,
				// up through platforms on the way
				float h = Math.Min(above + 36f, 380f);
				float vy = -MathF.Sqrt(2f * 0.3f * h);
				NPC.velocity.Y = vy;
				NPC.velocity.X = MathHelper.Clamp(dx / (-vy / 0.3f + 10f), -7f, 7f);
				hopCooldown = 45;
				NPC.netUpdate = true;
				SFX.SandKick(NPC.Bottom, new Vector2(-Math.Sign(dx) * 2f, -1f), 2, 2);
			}
			else if (Math.Abs(dx) > 900f)
			{
				// a crab that can be outrun is not a fight; it closes the gap in one leap
				NPC.velocity.Y = -9f;
				NPC.velocity.X = MathHelper.Clamp(dx * 0.02f, -11f, 11f);
				NPC.netUpdate = true;
			}
			else if (NPC.collideX)
				NPC.velocity.Y = -7.5f;
		}

		private void NextMove()
		{
			Timer = -Pause;
			Step++;
			sub = Hiding;
			charges = 0;
			holdTicks = 0;
			NPC.netUpdate = true;
			hullCracks.Clear();
			crackStage = 0;
		}

		private void EnterPhaseTwo()
		{
			Phase = 1f;
			Timer = -40f;
			Step = 0f;
			sub = Hiding;
			charges = 0;
			NPC.netUpdate = true;
			SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
			SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.5f }, NPC.Center);
			for (int i = 0; i < 40; i++)
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height,
					i % 2 == 0 ? DustID.Water : DustID.WoodFurniture,
					Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-7f, 2f), 60, default, 1.4f);
				d.noGravity = i % 2 == 0;
			}
			MastSnap();
			if (Main.netMode != NetmodeID.MultiplayerClient)
				ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Mods.Terrapex.Chat.ShellbackPhase2"),
					new Color(120, 220, 210));
		}

		/// <summary>A point on the sprite, given facing right from its bottom centre, in the world.</summary>
		private Vector2 OnSprite(Vector2 local) => NPC.Bottom + new Vector2(local.X * NPC.spriteDirection, local.Y);

		/// <summary>The mast goes: a flash, splinters of the spar, rope ends and rags of sail.</summary>
		private void MastSnap()
		{
			Vector2 mast = OnSprite(new Vector2(-24f, -66f));
			ParticleSystem.Spawn(new Bloom(mast, Color.White, 0.3f, 1.1f, 10));
			ParticleSystem.Spawn(new Bloom(mast, SFX.WoodL, 0.5f, 1.6f, 18));
			ParticleSystem.Spawn(new Shockwave(mast, SFX.CanvasL, 0.1f, 1.3f, 18));
			SFX.Splinters(mast, 22, 7f, 6f, 10f);
			for (int i = 0; i < 5; i++)
				ParticleSystem.Spawn(new SFX.Splinter(mast, Main.rand.NextVector2Circular(5f, 5f) + new Vector2(0f, -3f),
					SFX.CanvasD, Main.rand.NextFloat(12f, 20f), 1f, Main.rand.Next(60, 80)) { Gravity = 0.15f });
			for (int i = 0; i < 6; i++)
				ParticleSystem.Spawn(new Chip(mast, Main.rand.NextVector2Circular(4f, 4f) + new Vector2(0f, -2.5f),
					i % 2 == 0 ? SFX.CanvasL : SFX.CanvasD, 1.4f, Main.rand.Next(50, 70)));
			FX.Sparks(mast, SFX.WoodL, 8, 6f, 0.8f, 18, 0.1f);
			if (!Main.dedServ)
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(mast, Main.rand.NextVector2Unit(), 7f, 6f, 18, 1400f,
					"ShellbackMast"));
		}

		/// <summary>Through the throes: planks springing loose, spray, and light through new cracks.</summary>
		private void DeathThroes(float p)
		{
			Rectangle box = NPC.Hitbox;
			if (Main.rand.NextFloat() < 0.2f + p * 0.6f)
				SFX.Splinters(Main.rand.NextVector2FromRectangle(box), 1, 3f + p * 3f, 0f, 0f);
			if (Main.rand.NextBool(3))
				SFX.Splash(new Vector2(Main.rand.NextFloat(box.Left, box.Right), box.Top + 10f), 1, 4f);
			if (Main.rand.NextFloat() < 0.04f + p * 0.08f)
				FX.Crack(Main.rand.NextVector2FromRectangle(box), Main.rand.NextVector2Unit(), 30f + p * 40f,
					SFX.SeaL, 30, 2f, 2);
		}

		/// <summary>The final burst, at where it was on the frame it died.</summary>
		private static void DeathBurst(Vector2 centre, Vector2 bottom)
		{
			ParticleSystem.Spawn(new Bloom(centre, Color.White, 0.6f, 2.4f, 16));
			ParticleSystem.Spawn(new Bloom(centre, SFX.SeaL, 1f, 3f, 26));
			ParticleSystem.Spawn(new Shockwave(centre, SFX.SeaL, 0.2f, 3f, 28));
			FX.GroundSlam(bottom, SFX.SeaW, SFX.Lit(SFX.SandB, bottom), 1.8f);
			SFX.Splinters(centre, 36, 9f, 40f, 24f);
			SFX.Splash(centre, 24, 9f, -MathHelper.PiOver2, MathHelper.TwoPi, 8);
			for (int i = 0; i < 6; i++)
				ParticleSystem.Spawn(new SFX.Splinter(centre, Main.rand.NextVector2Circular(6f, 6f) + new Vector2(0f, -4f),
					SFX.CanvasD, Main.rand.NextFloat(12f, 22f), 1f, Main.rand.Next(70, 90)) { Gravity = 0.15f });
		}

		// ------------------------------------------------------------------ the scuttle

		private void DoScuttle(Player target)
		{
			int tell = ScuttleTell;
			int recover = Phase == 0f ? 30 : 18;
			if (Timer == 1f)
			{
				AimDir = target.Center.X >= NPC.Center.X ? 1f : -1f;
				NPC.netUpdate = true;
			}
			NPC.spriteDirection = NPC.direction = (int)AimDir;
			if (Timer <= tell)
			{
				// claws up and digging in: the line on the sand is the whole tell
				NPC.velocity.X *= 0.8f;
				if (Main.rand.NextBool(3))
					Dust.NewDustDirect(NPC.BottomLeft - new Vector2(0f, 6f), NPC.width, 6, DustID.Sand,
						-AimDir * 2f, -1.5f, 100, default, 1f);
				rammed = false;
				ScuttleTellFX();
				return;
			}
			if (Timer <= tell + ChargeTicks)
			{
				if (Timer == tell + 1)
					SoundEngine.PlaySound(SoundID.Item74 with { Pitch = -0.6f, Volume = 0.8f }, NPC.Center);
				bool hitWall = NPC.collideX;
				NPC.velocity.X = AimDir * ChargeSpeed;
				if (NPC.collideX && NPC.velocity.Y == 0f)
					NPC.velocity.Y = -6f;
				if (Main.rand.NextBool(2))
					Dust.NewDustDirect(NPC.BottomLeft - new Vector2(0f, 6f), NPC.width, 6, DustID.Sand,
						-AimDir * 3f, -2f, 80, default, 1.2f);
				ChargeFX(hitWall);
				return;
			}
			NPC.velocity.X *= 0.88f;
			if (Timer >= tell + ChargeTicks + recover)
			{
				// phase two scuttles twice: there and back
				if (Phase == 1f && charges == 0)
				{
					charges = 1;
					Timer = 0f;
					NPC.netUpdate = true;
					return;
				}
				NextMove();
			}
		}

		/// <summary>How much of the charge line the churned strip has reached, 0..1: fast at first, settling.</summary>
		private float ScuttleFill
		{
			get
			{
				float f = MathHelper.Clamp(Timer / ScuttleTell, 0f, 1f);
				return 1f - (1f - f) * (1f - f);
			}
		}

		private Vector2 ScuttleStart => new(NPC.Center.X + AimDir * NPC.width * 0.5f, NPC.Bottom.Y - 4f);

		/// <summary>The sand along the charge line churns up, front first; the claws dig in behind.</summary>
		private void ScuttleTellFX()
		{
			float len = ChargeSpeed * ChargeTicks;
			float fill = ScuttleFill;
			Vector2 a = ScuttleStart + new Vector2(0f, 3f);
			Vector2 p = a + new Vector2(AimDir * len * Main.rand.NextFloat(fill), 0f);
			if (Main.rand.NextBool(2))
				SFX.SandKick(p, new Vector2(AimDir * 0.8f, -0.3f), 1, 0);
			if (Main.rand.NextBool(3))
				SFX.SandKick(p, new Vector2(AimDir * 1.5f, 0f), 0, 1);
			// the running front of the strip throws a little more
			if ((int)Timer % 3 == 0)
				SFX.SandKick(a + new Vector2(AimDir * len * fill, 0f), new Vector2(AimDir * 1.6f, -0.4f), 1, 1);
			if (Main.rand.NextBool(2))
				SFX.SandKick(new Vector2(NPC.Center.X - AimDir * NPC.width * 0.3f, NPC.Bottom.Y - 2f),
					new Vector2(-AimDir * 2.5f, -1f), 0, 1);
		}

		/// <summary>Sand and spray behind, skid marks under the legs, and a burst if it meets a wall.</summary>
		private void ChargeFX(bool hitWall)
		{
			Vector2 back = new(NPC.Center.X - AimDir * NPC.width * 0.4f, NPC.Bottom.Y - 2f);
			if (NPC.velocity.Y == 0f)
			{
				SFX.SandKick(back, new Vector2(-AimDir * 2.5f, -1f), 1, Main.rand.NextBool(2) ? 1 : 0);
				if (Main.rand.NextBool(2))
					ParticleSystem.Spawn(new Spark(back - new Vector2(0f, 6f),
						new Vector2(-AimDir * Main.rand.NextFloat(2f, 5f), -Main.rand.NextFloat(2f, 5f)),
						SFX.SeaL, 0.8f, Main.rand.Next(18, 28), 0.2f) { Drag = 0.97f, Length = 1.6f });
				if ((int)Timer % 3 == 0)
				{
					// two legs' worth of scuff, long enough at this speed to read as a continuous skid
					for (int k = -1; k <= 1; k += 2)
						ParticleSystem.Spawn(new SFX.GroundMark(new Vector2(NPC.Center.X + k * NPC.width * 0.28f, NPC.Bottom.Y + 1f),
							30f, 2f, SFX.SandK * 0.85f, 80));
				}
			}
			if (hitWall && !rammed)
			{
				rammed = true;
				Vector2 wall = new(NPC.Center.X + AimDir * NPC.width * 0.5f, NPC.Bottom.Y);
				FX.GroundSlam(wall, SFX.SandL, SFX.Lit(SFX.SandB, wall), 1f);
				SFX.SandKick(wall - new Vector2(0f, 10f), new Vector2(-AimDir * 3f, -1f), 3, 8);
				SFX.Splinters(wall - new Vector2(0f, 24f), 6, 4f, 4f, 14f);
				FX.Sparks(wall - new Vector2(0f, 20f), SFX.SandL, 6, 5f, 0.8f, 16, 0.2f, 1.6f, AimDir > 0f ? MathHelper.Pi : 0f);
				SoundEngine.PlaySound(SoundID.Dig with { Pitch = -0.5f, Volume = 1.1f }, wall);
				if (!Main.dedServ)
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(wall, new Vector2(AimDir, 0f), 6f, 8f, 14, 1000f,
						"ShellbackRam"));
			}
		}

		// ------------------------------------------------------------------ the bubbles

		private Vector2 Mouth => NPC.Center + new Vector2(NPC.spriteDirection * 42f, 0f);

		private void DoBubbles(Player target)
		{
			Face(target);
			NPC.velocity.X *= 0.85f;
			int tell = BubbleTell;
			if (Timer < tell && (int)Timer % 2 == 0)
			{
				// it draws water in: pinhead bubbles gathering at the mouth under the eye flash
				Vector2 from = Mouth + Main.rand.NextVector2CircularEdge(34f, 26f);
				ParticleSystem.Spawn(new SFX.MicroBubble(from, (Mouth - from) * 0.09f, Main.rand.NextFloat(0.8f, 1.3f), 18));
			}
			if (Timer == tell)
				FireFan(target, Phase == 0f ? 5 : 7, 0f);
			if (Phase == 1f && Timer == tell + 30)
				FireFan(target, 6, 0.5f);
			if (Timer >= (Phase == 0f ? 70 : 96))
				NextMove();
		}

		/// <summary>A flat fan laid on where the player is now. <paramref name="offset"/> shifts it half a gap.</summary>
		private void FireFan(Player target, int count, float offset)
		{
			SoundEngine.PlaySound(SoundID.Item85, Mouth);
			Vector2 at = (target.Center - Mouth).SafeNormalize(Vector2.UnitX * NPC.spriteDirection);
			// the spit: a gulp of spray and a ring off the mouth, along the fan
			ParticleSystem.Spawn(new Bloom(Mouth, SFX.SeaL, 0.3f, 0.8f, 10));
			ParticleSystem.Spawn(new Shockwave(Mouth, SFX.SeaW, 0.05f, 0.6f, 12));
			SFX.Splash(Mouth, 10, 5f, at.ToRotation(), 1.2f, 3);
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			float spread = Phase == 0f ? 0.8f : 1.1f;
			int damage = NPC.GetAttackDamage_ForProjectiles(BubbleDamage, BubbleDamage);
			for (int i = 0; i < count; i++)
			{
				float t = count == 1 ? 0f : (i + offset) / (count - 1f) - 0.5f;
				Projectile.NewProjectile(NPC.GetSource_FromAI(), Mouth, at.RotatedBy(t * spread) * 4.2f,
					ModContent.ProjectileType<ShellbackBubble>(), damage, 0f, Main.myPlayer);
			}
		}

		// ------------------------------------------------------------------ the cannon

		/// <summary>The cannon on the wreck's deck, at the back of the hull.</summary>
		private Vector2 Muzzle => NPC.Center + new Vector2(-NPC.spriteDirection * 45f, -34f);

		/// <summary>
		/// The launch velocity that lands a ball on <paramref name="to"/> after
		/// <see cref="CannonFlight"/> ticks. The ball adds gravity before it moves, so after T
		/// ticks it has fallen g*T(T+1)/2 - the dotted arc is drawn from the same sum.
		/// </summary>
		public static Vector2 LaunchVelocity(Vector2 from, Vector2 to)
		{
			Vector2 d = to - from;
			float t = CannonFlight;
			return new Vector2(d.X / t, (d.Y - CannonGravity * t * (t + 1f) / 2f) / t);
		}

		private void DoCannon(Player target)
		{
			Face(target);
			NPC.velocity.X *= 0.8f;
			if (Timer == 1f && Main.netMode != NetmodeID.MultiplayerClient)
			{
				cannonCount = Phase == 0f ? 1 : 3;
				Vector2 at = target.Bottom - new Vector2(0f, 8f);
				cannonTargets[0] = at;
				cannonTargets[1] = at + new Vector2(-170f, 0f);
				cannonTargets[2] = at + new Vector2(170f, 0f);
				NPC.netUpdate = true;
			}
			for (int i = 0; i < cannonCount; i++)
			{
				int fired = CannonTell + i * 10;
				if (Timer > fired - 16 && Timer < fired && (int)Timer % 2 == 0)
				{
					// the fuse burning down
					FX.Sparks(Muzzle, SFX.Powder, 1, 3f, 0.6f, 12, 0.1f, 1.2f, -MathHelper.PiOver2);
				}
				if (Timer == fired)
				{
					SoundEngine.PlaySound(SoundID.Item62 with { Pitch = -0.2f }, Muzzle);
					for (int k = 0; k < 4; k++)
						Dust.NewDustPerfect(Muzzle, DustID.Smoke, Main.rand.NextVector2Circular(2f, 2f) + new Vector2(0f, -2f),
							100, default, 1.4f).noGravity = true;
					MuzzleFX(LaunchVelocity(Muzzle, cannonTargets[i]).SafeNormalize(-Vector2.UnitY));
					if (Main.netMode != NetmodeID.MultiplayerClient)
						Projectile.NewProjectile(NPC.GetSource_FromAI(), Muzzle, LaunchVelocity(Muzzle, cannonTargets[i]),
							ModContent.ProjectileType<ShellbackCannonball>(),
							NPC.GetAttackDamage_ForProjectiles(CannonDamage, CannonDamage), 0f, Main.myPlayer);
				}
			}
			if (Timer >= CannonTell + 20 * cannonCount + 30)
				NextMove();
		}

		/// <summary>The cannon goes off: a flash, fire and sparks along the shot, a ring, and powder smoke rolling off.</summary>
		private void MuzzleFX(Vector2 dir)
		{
			Vector2 m = Muzzle;
			ParticleSystem.Spawn(new Bloom(m, SFX.Powder, 0.4f, 1.0f, 10));
			ParticleSystem.Spawn(new Bloom(m + dir * 8f, new Color(255, 245, 210), 0.2f, 0.5f, 6));
			ParticleSystem.Spawn(new Shockwave(m, SFX.Powder, 0.05f, 0.6f, 12));
			FX.Sparks(m, SFX.Powder, 10, 8f, 1f, 20, 0.1f, 0.7f, dir.ToRotation());
			SFX.PowderSmoke(m, dir, 8, 3f);
			if (!Main.dedServ)
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(m, -dir, 2.5f, 8f, 8, 900f, "ShellbackMuzzle"));
		}

		/// <summary>Where a ball fired with <paramref name="v0"/> is after <paramref name="t"/> ticks - the same sum it flies.</summary>
		private static Vector2 ArcPoint(Vector2 from, Vector2 v0, float t)
			=> from + v0 * t + new Vector2(0f, CannonGravity * t * (t + 1f) / 2f);

		// ------------------------------------------------------------------ the hull

		private void DoHull(Player target)
		{
			if (Timer == 1f)
			{
				sub = Hiding;
				hideLife = NPC.life;
				NPC.netUpdate = true;
				SoundEngine.PlaySound(SoundID.NPCHit42 with { Pitch = -0.6f }, NPC.Center);
				hullCracks.Clear();
				crackStage = 0;
				SFX.SandKick(NPC.Bottom, new Vector2(-2f, 0f), 2, 3);
				SFX.SandKick(NPC.Bottom, new Vector2(2f, 0f), 2, 3);
			}

			switch (sub)
			{
				case Hiding:
					NPC.velocity.X *= 0.85f;
					HullCreaks();
					if (hideLife - NPC.life >= CrackAt)
					{
						// the player spent the hide hitting it, and the hull gave
						sub = Cracked;
						Timer = 1f;
						NPC.netUpdate = true;
						SoundEngine.PlaySound(SoundID.Item14 with { Pitch = 0.3f }, NPC.Center);
						for (int i = 0; i < 30; i++)
							Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.WoodFurniture,
								Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-6f, 0f), 40, default, 1.3f);
						HullGives();
						return;
					}
					if (Timer >= (Phase == 0f ? HideTicks : HideTicksTwo))
					{
						sub = SlamTell;
						Timer = 1f;
						slamX = target.Center.X;
						slamY = FloorUnder(target.Center);
						NPC.netUpdate = true;
					}
					break;

				case SlamTell:
					NPC.velocity.X *= 0.8f;
					SlamTellFX();
					if (Timer >= SlamTellTicks)
					{
						// the leap: up, and across to the marked spot
						sub = Airborne;
						Timer = 1f;
						// an arc solved for the marked floor, so a player on a platform is landed on, not under
						float dx = slamX - NPC.Center.X;
						float rise = NPC.Bottom.Y - slamY;
						float apex = MathHelper.Clamp(Math.Max(240f, rise + 90f), 120f, 460f);
						float vy = -MathF.Sqrt(2f * 0.3f * apex);
						float air = -vy / 0.3f + MathF.Sqrt(2f * Math.Max(8f, apex - rise) / 0.3f) + 6f;
						NPC.velocity.Y = vy;
						NPC.velocity.X = MathHelper.Clamp(dx / air, -10f, 10f);
						NPC.netUpdate = true;
						SoundEngine.PlaySound(SoundID.Item74 with { Pitch = -0.8f }, NPC.Center);
					}
					break;

				case Airborne:
					{
						float dx = slamX - NPC.Center.X;
						if (Math.Abs(dx) < 12f)
							NPC.velocity.X *= 0.8f;
						if ((Timer > 8f && NPC.velocity.Y == 0f) || Timer > 160f)
							Land();
					}
					break;

				case Stunned:
				case Cracked:
					NPC.velocity.X *= 0.85f;
					if (Main.rand.NextBool(4))
						Dust.NewDustDirect(NPC.position, NPC.width, 12, DustID.Water, 0f, -1f, 80, default, 1f).noGravity = true;
					// the sea leaking out of it: bubbles off the hull, now and then a loose plank
					if ((int)Timer % 6 == 0)
						ParticleSystem.Spawn(new SFX.MicroBubble(new Vector2(Main.rand.NextFloat(NPC.Left.X, NPC.Right.X), NPC.Top.Y + 8f),
							new Vector2(0f, -0.5f), Main.rand.NextFloat(0.9f, 1.4f), Main.rand.Next(30, 44)));
					if (sub == Cracked && Main.rand.NextBool(14))
						SFX.Splinters(OnSprite(new Vector2(Main.rand.NextFloat(-40f, 12f), -40f)), 1, 2f, 0f, 0f);
					if (Timer >= (sub == Cracked ? CrackTicks : StunTicks))
						NextMove();
					break;
			}
		}

		private void Land()
		{
			sub = Stunned;
			Timer = 1f;
			NPC.velocity.X = 0f;
			NPC.netUpdate = true;
			SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.6f, Volume = 1.2f }, NPC.Center);
			if (Main.netMode != NetmodeID.Server)
				Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(
				NPC.Center, Vector2.UnitY, 10f, 6f, 20, 1400f, "OldShellback"));
			for (int i = 0; i < 16; i++)
				Dust.NewDustDirect(NPC.BottomLeft - new Vector2(0f, 8f), NPC.width, 8, DustID.Sand,
					Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-5f, -1f), 60, default, 1.4f);
			LandFX();
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			// the slam sends a wave out along the sand each way
			int damage = NPC.GetAttackDamage_ForProjectiles(WaveDamage, WaveDamage);
			float speed = Phase == 0f ? 6f : 8f;
			for (int s = -1; s <= 1; s += 2)
				Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Bottom + new Vector2(s * 40f, -20f),
					new Vector2(s * speed, 0f), ModContent.ProjectileType<ShellbackWave>(), damage, 0f, Main.myPlayer);

			// phase two also spits a ring of bubbles up out of the hull
			if (Phase == 1f)
			{
				int bubble = NPC.GetAttackDamage_ForProjectiles(BubbleDamage, BubbleDamage);
				for (int i = 0; i < 6; i++)
				{
					float a = -MathHelper.PiOver2 + (i / 5f - 0.5f) * 2.0f;
					Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center - new Vector2(0f, 20f),
						a.ToRotationVector2() * 3.6f, ModContent.ProjectileType<ShellbackBubble>(), bubble, 0f, Main.myPlayer);
				}
			}
		}

		// ------------------------------------------------------------------ the hull's effects

		/// <summary>How far the hide is toward cracking, 0..1 - the same sum the gameplay checks.</summary>
		private float CrackProgress => MathHelper.Clamp((hideLife - NPC.life) / (float)CrackAt, 0f, 1f);

		/// <summary>One crack per seventh of the way to giving, each with a creak and a few splinters; idle creaks between.</summary>
		private void HullCreaks()
		{
			int want = (int)(CrackProgress * CrackStages);
			while (crackStage < want)
				AddHullCrack(true);
			if (Main.rand.NextBool(Math.Max(6, 40 - crackStage * 5)))
				SFX.Splinters(OnSprite(new Vector2(Main.rand.NextFloat(-40f, 12f), -44f)), 1, 1.5f, 0f, 0f);
		}

		/// <summary>A crack across the planks, kept on the sprite until the move ends.</summary>
		private void AddHullCrack(bool loud)
		{
			crackStage++;
			static Vector2 Snap(Vector2 v) => new(MathF.Round(v.X / 2f) * 2f, MathF.Round(v.Y / 2f) * 2f);
			static Vector2 Keep(Vector2 v) => new(MathHelper.Clamp(v.X, -46f, 18f), MathHelper.Clamp(v.Y, -46f, -10f));

			Vector2 p = Snap(new Vector2(Main.rand.NextFloat(-40f, 12f), Main.rand.NextFloat(-40f, -16f)));
			float ang = Main.rand.NextFloat(MathHelper.TwoPi);
			Vector2 start = p;
			var pts = new List<Vector2> { p };
			int n = Main.rand.Next(3, 6);
			for (int i = 0; i < n; i++)
			{
				ang += Main.rand.NextFloat(-0.7f, 0.7f);
				p = Snap(Keep(p + ang.ToRotationVector2() * Main.rand.NextFloat(4f, 7f)));
				pts.Add(p);
				if (i == 1 && Main.rand.NextBool(2))
				{
					float b = ang + (Main.rand.NextBool() ? 1f : -1f);
					Vector2 q = Snap(Keep(p + b.ToRotationVector2() * 6f));
					hullCracks.Add(new[] { p, q, Snap(Keep(q + (b + 0.3f).ToRotationVector2() * 5f)) });
				}
			}
			hullCracks.Add(pts.ToArray());
			if (!loud)
				return;
			Vector2 world = OnSprite(start);
			SFX.Splinters(world, 3, 2.5f, 4f, 4f);
			FX.Crack(world, new Vector2((float)Math.Cos(ang) * NPC.spriteDirection, (float)Math.Sin(ang)), 24f, SFX.Powder, 20, 1.5f, 1);
			SoundEngine.PlaySound(SoundID.Dig with { Pitch = -0.6f + crackStage * 0.05f, Volume = 0.7f }, world);
		}

		/// <summary>The hull gives: every crack at once, planks thrown off, light out of the seams.</summary>
		private void HullGives()
		{
			while (crackStage < CrackStages + 2)
				AddHullCrack(false);
			Vector2 hc = OnSprite(new Vector2(-14f, -28f));
			SFX.Splinters(hc, 26, 7f, 30f, 14f);
			ParticleSystem.Spawn(new Bloom(hc, SFX.Powder, 0.4f, 1.5f, 14));
			ParticleSystem.Spawn(new Shockwave(hc, SFX.WoodL, 0.1f, 1.4f, 20));
			for (int i = 0; i < 5; i++)
				FX.Crack(hc, Main.rand.NextVector2Unit(), 50f, SFX.Powder, 30, 2f, 2);
			if (!Main.dedServ)
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(hc, Main.rand.NextVector2Unit(), 6f, 6f, 16, 1200f,
					"ShellbackCrack"));
		}

		/// <summary>Anything the player could be standing on: solid ground or a platform top.</summary>
		public static bool Standable(int x, int y)
		{
			if (!WorldGen.InWorld(x, y, 2))
				return false;
			Tile t = Main.tile[x, y];
			return t.HasUnactuatedTile && (Main.tileSolid[t.TileType] || Main.tileSolidTop[t.TileType]);
		}

		private static float FloorUnder(Vector2 at)
		{
			int tx = (int)(at.X / 16f);
			int ty = (int)(at.Y / 16f);
			for (int i = 0; i < 40; i++)
				if (Standable(tx, ty + i))
					return (ty + i) * 16f;
			return at.Y + 24f;
		}

		/// <summary>
		/// Platforms hide nothing: it drops through them to a player below and stands on them
		/// otherwise. In the slam it falls through every platform above the marked floor and lands
		/// on that one.
		/// </summary>
		public override bool? CanFallThroughPlatforms()
		{
			if (Move == Hull && Timer > 0f && sub == Airborne)
				return NPC.Bottom.Y < slamY - 6f;
			Player t = Main.player[NPC.target];
			return t.active && !t.dead && t.Bottom.Y > NPC.Bottom.Y + 20f;
		}

		/// <summary>The floor under the slam spot (drawn, not simulated): where the player stood when it was marked.</summary>
		private float SlamGround()
		{
			if (slamY > 0f)
				return slamY;
			int tx = (int)(slamX / 16f);
			int ty0 = (int)((NPC.Bottom.Y - 160f) / 16f);
			for (int ty = ty0; ty < ty0 + 24; ty++)
			{
				if (WorldGen.InWorld(tx, ty, 2) && WorldGen.SolidTile(tx, ty) && !WorldGen.SolidTile(tx, ty - 1))
					return ty * 16f;
			}
			return NPC.Bottom.Y;
		}

		/// <summary>Motes rising up the column, sand lifting off the band, a ring on the sand every ten ticks.</summary>
		private void SlamTellFX()
		{
			float ground = SlamGround();
			float w = NPC.width * 0.6f;
			ParticleSystem.Spawn(new Spark(new Vector2(slamX + Main.rand.NextFloat(-10f, 10f), ground - Main.rand.NextFloat(0f, 80f)),
				new Vector2(0f, -Main.rand.NextFloat(2f, 4.5f)), SFX.SeaL, 0.7f, Main.rand.Next(20, 32)) { Drag = 0.99f });
			if ((int)Timer % 2 == 0)
				SFX.SandKick(new Vector2(slamX + Main.rand.NextFloat(-w, w), ground - 2f), new Vector2(0f, -0.6f), 1, 1);
			if ((int)Timer % 10 == 1)
				ParticleSystem.Spawn(new Shockwave(new Vector2(slamX, ground - 2f), SFX.SeaL, 0.2f, w * 2f / 64f, 20, 0.25f));
		}

		/// <summary>The landing: a floor shockwave, sand thrown both ways, water up out of the hull, loose planks.</summary>
		private void LandFX()
		{
			Vector2 floor = NPC.Bottom;
			FX.GroundSlam(floor, SFX.SeaL, SFX.Lit(SFX.SandB, floor), 1.6f);
			ParticleSystem.Spawn(new Shockwave(floor, Color.White, 0.2f, 2.6f, 22, 0.22f));
			SFX.SandKick(floor, new Vector2(-4f, 0f), 4, 8);
			SFX.SandKick(floor, new Vector2(4f, 0f), 4, 8);
			SFX.Splash(floor - new Vector2(0f, 10f), 18, 9f, -MathHelper.PiOver2, 2f, 6);
			SFX.Splinters(NPC.Center, 8, 4f, 30f, 10f);
		}

		// ------------------------------------------------------------------ taking hits

		public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
		{
			if (InShell)
				modifiers.FinalDamage *= HiddenTaken;
			else if (Down)
				modifiers.FinalDamage *= StunnedTaken;
		}

		public override bool CanHitPlayer(Player target, ref int cooldownSlot)
		{
			cooldownSlot = ImmunityCooldownID.Bosses;
			return !Down && !death.Dying;
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < 3; i++)
				Dust.NewDust(NPC.position, NPC.width, NPC.height, InShell ? DustID.WoodFurniture : DustID.Water,
					hit.HitDirection * 2f, -1f, 80, default, 1f);
			Vector2 at = Main.rand.NextVector2FromRectangle(NPC.Hitbox);
			if (InShell)
				SFX.Splinters(at, 2, 3f, 0f, 0f);
			else
				SFX.Splash(at, 3, 3f, hit.HitDirection > 0 ? -0.6f : -2.5f, 1.2f);
		}

		// ------------------------------------------------------------------ death and loot

		public override bool CheckDead()
			=> death.CheckDead(NPC, 130, () =>
			{
				int bubble = ModContent.ProjectileType<ShellbackBubble>();
				int ball = ModContent.ProjectileType<ShellbackCannonball>();
				int wave = ModContent.ProjectileType<ShellbackWave>();
				foreach (Projectile p in Main.ActiveProjectiles)
				{
					if (p.type == bubble || p.type == ball || p.type == wave)
						p.Kill();
				}
			});

		public override void OnKill()
		{
			if (!DownedBossSystem.downedShellback)
			{
				DownedBossSystem.downedShellback = true;
				if (Main.netMode != NetmodeID.MultiplayerClient)
					ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Mods.Terrapex.Chat.ShellbackDown"),
						new Color(120, 220, 210));
				if (Main.netMode == NetmodeID.Server)
					NetMessage.SendData(MessageID.WorldData);
			}
		}

		/// <summary>The four class weapons and the scythe, one of them; the bag rolls the same pool.</summary>
		public static int[] WeaponPool => new[] {
			ModContent.ItemType<WreckersCutlass>(),
			ModContent.ItemType<BarnacleBlunderbuss>(),
			ModContent.ItemType<BrineConch>(),
			ModContent.ItemType<RustedCrabPot>(),
			ModContent.ItemType<Undertow>(),
		};

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<ShellbackBag>()));

			LeadingConditionRule notExpert = new(new Conditions.NotExpert());
			notExpert.OnSuccess(ItemDropRule.OneFromOptions(1, WeaponPool));
			notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<BarnacleCluster>(), 1, 8, 14));
			notExpert.OnSuccess(ItemDropRule.Common(ItemID.Seashell, 1, 2, 4));
			notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<OldShellbackMask>(), 7));
			notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<BarnacledHone>(), 3));
			npcLoot.Add(notExpert);
		}

		public override void BossLoot(ref int potionType) => potionType = ItemID.LesserHealingPotion;

		// ------------------------------------------------------------------ drawing

		public override void FindFrame(int frameHeight)
		{
			int broken = Phase == 1f ? 1 : 0;
			int frame;
			if (InShell)
				frame = 8 + broken;
			else if (Down || death.Dying)
				frame = 10 + broken;
			else
			{
				float speed = Math.Abs(NPC.velocity.X);
				bool shaking = Move == Scuttle && Timer > 0f && Timer <= ScuttleTell;
				NPC.frameCounter += shaking ? 1.0 : speed * 0.25;
				if (NPC.frameCounter >= 6.0)
				{
					NPC.frameCounter = 0.0;
					walkFrame = (walkFrame + 1) % 4;
				}
				if (speed < 0.2f && !shaking)
					walkFrame = 0;
				frame = walkFrame + broken * 4;
			}
			NPC.frame.Y = frame * frameHeight;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			if (!death.Dying && Timer > 0f)
				DrawTells();

			Texture2D body = TextureAssets.Npc[Type].Value;
			SpriteEffects fx = NPC.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Rectangle frame = new(0, NPC.frame.Y, FrameW, FrameH);
			Vector2 origin = new(FrameW / 2f, FrameH);
			Vector2 shake = Vector2.Zero;
			if ((Move == Hull && sub == SlamTell) || (Move == Scuttle && Timer > 0f && Timer <= ScuttleTell))
				shake = new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), 0f);
			Vector2 at = NPC.Bottom - screenPos + new Vector2(0f, NPC.gfxOffY + 2f) + shake;
			spriteBatch.Draw(body, at, frame, drawColor, NPC.rotation, origin, 1f, fx, 0f);

			if (hullCracks.Count > 0 && Move == Hull && Timer > 0f && !death.Dying)
				DrawHullCracks(at + screenPos, drawColor);

			// the eye flash before a volley, and the water gathering in the mouth
			if (Move == Bubbles && Timer > 0f && Timer < BubbleTell)
			{
				float f = Timer / BubbleTell;
				Color flash = RiftDraw.Glow(140, 240, 255, 0.4f + 0.5f * f);
				RiftDraw.Bloom(NPC.Center + new Vector2(NPC.spriteDirection * 21f, -30f), flash, 0.3f + 0.2f * f);
				RiftDraw.Bloom(NPC.Center + new Vector2(NPC.spriteDirection * 29f, -32f), flash, 0.3f + 0.2f * f);
				RiftDraw.Bloom(Mouth, RiftDraw.Glow(120, 230, 230, 0.35f * f), 0.2f + 0.25f * f);
			}
			return false;
		}

		private static readonly Color Sea = new(120, 220, 210);
		private static readonly Color Powder = new(255, 170, 90);

		/// <summary>
		/// The cracks over the planks: a dark split with a lit lower lip, drawn on the 2-px grid.
		/// Past halfway the light of the inside starts to show through, so the dial reads as
		/// "nearly there" on the hull itself and not only on the bar.
		/// </summary>
		private void DrawHullCracks(Vector2 bottom, Color light)
		{
			float glow = sub == Hiding ? MathHelper.Clamp((CrackProgress - 0.5f) * 2f, 0f, 1f)
				: sub == Cracked ? 1f - Timer / CrackTicks : 0f;
			glow *= 0.75f + 0.25f * MathF.Sin(Timer * 0.4f);
			Color dark = SFX.Outline.MultiplyRGB(light);
			Color lip = SFX.WoodL.MultiplyRGB(light) * 0.8f;
			int dir = NPC.spriteDirection;
			foreach (Vector2[] pts in hullCracks)
			{
				for (int j = 0; j + 1 < pts.Length; j++)
				{
					Vector2 a = bottom + new Vector2(pts[j].X * dir, pts[j].Y);
					Vector2 b = bottom + new Vector2(pts[j + 1].X * dir, pts[j + 1].Y);
					RiftDraw.Line(a + new Vector2(0f, 2f), b + new Vector2(0f, 2f), lip, 2f);
					RiftDraw.Line(a, b, dark, 2f);
					if (glow > 0f)
						RiftDraw.Line(a, b, RiftDraw.Glow(255, 170, 90, glow), 1f);
				}
			}
		}

		private void DrawTells()
		{
			// the scuttle: the sand along the charge churns and wets from the boss outward, a faint
			// full-length strip from the first tick so the whole line is readable at once
			if (Move == Scuttle && Timer <= ScuttleTell)
			{
				float f = Timer / ScuttleTell;
				float len = ChargeSpeed * ChargeTicks;
				float fill = ScuttleFill;
				Vector2 a = ScuttleStart;
				float t = Timer;
				for (float x = 0f; x < len; x += 8f)
				{
					float u = x / len;
					bool reached = u <= fill;
					Vector2 p = a + new Vector2(AimDir * x, 0f);
					Vector2 q = p + new Vector2(AimDir * 8f, 0f);
					// ragged edge: each run its own thickness, churning
					float ragged = 2f + 1f * MathF.Sin(x * 0.21f + t * 0.3f) + 1f * MathF.Sin(x * 0.07f - t * 0.17f);
					if (reached)
					{
						// wet, scuffed sand under the foam
						RiftDraw.Line(p + new Vector2(0f, 3f), q + new Vector2(0f, 3f), SFX.SandK * 0.55f, 4f);
						RiftDraw.Line(p, q, Sea * (0.35f + 0.55f * f), 2f + ragged * 0.5f);
						RiftDraw.Line(p - new Vector2(0f, 1f), q - new Vector2(0f, 1f), RiftDraw.Glow(232, 255, 248, 0.35f + 0.4f * f), 1f);
					}
					else
						RiftDraw.Line(p, q, Sea * 0.22f, 2f);
				}
				// the running front of the strip
				Vector2 front = a + new Vector2(AimDir * len * fill, 0f);
				RiftDraw.Bloom(front, RiftDraw.Glow(150, 240, 225, 0.5f), 0.22f);
				// the end of the charge, staked
				Vector2 end = a + new Vector2(AimDir * len, 0f);
				RiftDraw.Line(end + new Vector2(0f, 3f), end - new Vector2(0f, 12f), Sea * (0.3f + 0.6f * f), 2f);

				// chevrons pointing the way it will go, a pulse of light running outward through them
				for (int i = 1; i <= 5; i++)
				{
					float pulse = 0.5f + 0.5f * MathF.Sin(t * 0.35f - i * 1.1f);
					float k = (0.2f + 0.5f * f) * (0.55f + 0.45f * pulse) * (i / 6f <= fill ? 1f : 0.45f);
					Vector2 c = a + new Vector2(AimDir * len * i / 6f, -10f);
					float lean = 2f * pulse;
					Vector2 tip = c + new Vector2(AimDir * (10f + lean), 9f);
					RiftDraw.Line(c, tip, SFX.Outline * (0.5f * k), 4f);
					RiftDraw.Line(c + new Vector2(0f, 18f), tip, SFX.Outline * (0.5f * k), 4f);
					RiftDraw.Line(c, tip, Sea * k, 2f);
					RiftDraw.Line(c + new Vector2(0f, 18f), tip, Sea * k, 2f);
					if (pulse > 0.85f)
						RiftDraw.Bloom(tip, RiftDraw.Glow(200, 255, 245, (pulse - 0.85f) * 3f * f), 0.15f);
				}
			}

			// the cannon: lights running along each arc from the deck to a reticle that tightens
			// onto where the ball will burst; the reticle stays up until the ball has landed
			if (Move == Cannon)
			{
				Texture2D px = TextureAssets.MagicPixel.Value;
				for (int i = 0; i < cannonCount; i++)
				{
					float fired = CannonTell + i * 10;
					if (Timer > fired + CannonFlight + 1)
						continue;
					float f = MathHelper.Clamp(Timer / fired, 0f, 1f);
					if (Timer <= fired)
					{
						Vector2 from = Muzzle;
						Vector2 v0 = LaunchVelocity(from, cannonTargets[i]);
						// a faint ghost of the whole path
						Vector2 prev = from;
						for (int s = 3; s <= CannonFlight; s += 3)
						{
							Vector2 q = ArcPoint(from, v0, s);
							RiftDraw.Line(prev, q, Powder * (0.08f + 0.14f * f), 1.5f);
							prev = q;
						}
						// glinting markers travelling the arc
						for (int k = 0; k < 6; k++)
						{
							float phase = (Timer * 0.02f + k / 6f) % 1f;
							Vector2 m = ArcPoint(from, v0, phase * CannonFlight);
							float a = MathF.Sin(phase * MathHelper.Pi) * (0.35f + 0.65f * f);
							Main.EntitySpriteDraw(px, m - Main.screenPosition, new Rectangle(0, 0, 1, 1), SFX.Outline * (0.6f * a),
								MathHelper.PiOver4, new Vector2(0.5f), 6f, SpriteEffects.None, 0);
							Main.EntitySpriteDraw(px, m - Main.screenPosition, new Rectangle(0, 0, 1, 1), Powder * a,
								MathHelper.PiOver4, new Vector2(0.5f), 4f, SpriteEffects.None, 0);
							RiftDraw.Bloom(m, RiftDraw.Glow(255, 200, 130, 0.45f * a), 0.12f);
						}
						// the fuse glows in the last sixteen ticks
						if (fired - Timer < 16f)
							RiftDraw.Bloom(from, RiftDraw.Glow(255, 190, 110, (1f - (fired - Timer) / 16f) * 0.8f), 0.25f);
					}
					DrawReticle(cannonTargets[i], f, Timer > fired);
				}
			}

			// the slam: a column of light standing on the spot it will land on, the sand there
			// brightening, and once it is airborne, its shadow
			if (Move == Hull && (sub == SlamTell || sub == Airborne))
			{
				float f = sub == SlamTell ? Timer / SlamTellTicks : 1f;
				float ground = SlamGround();
				float pulse = 0.85f + 0.15f * MathF.Sin(Timer * 0.5f);
				for (int i = 0; i < 16; i++)
				{
					float fall = 1f - i / 16f;
					fall *= fall;
					Vector2 p0 = new(slamX, ground - i * 22f), p1 = new(slamX, ground - (i + 1) * 22f);
					RiftDraw.Line(p0, p1, RiftDraw.Glow(120, 220, 210, 0.14f * fall * (0.4f + 0.6f * f)), 18f + 10f * f);
					RiftDraw.Line(p0, p1, RiftDraw.Glow(150, 240, 225, 0.35f * fall * (0.3f + 0.7f * f) * pulse), 4f + 3f * f);
					RiftDraw.Line(p0, p1, RiftDraw.Glow(255, 255, 255, 0.5f * fall * f), 1.5f + f);
				}
				// the old dashes, now rising up the column
				for (int i = 0; i < 10; i++)
				{
					float rise = (i * 30f + Timer * 2.5f) % 300f;
					float yy = ground - 40f - rise;
					float a = 1f - rise / 320f;
					RiftDraw.Line(new Vector2(slamX, yy), new Vector2(slamX, yy - 12f), Sea * ((0.2f + 0.5f * f) * a), 3f);
				}
				// the band on the sand, with its two ends staked so the width reads
				float w = NPC.width * 0.6f;
				Vector2 l = new(slamX - w, ground - 2f), r = new(slamX + w, ground - 2f);
				RiftDraw.Bloom(new Vector2(slamX, ground - 4f), RiftDraw.Glow(120, 220, 210, 0.3f + 0.4f * f), 0.5f + 0.6f * f);
				RiftDraw.Line(l, r, Sea * (0.3f + 0.6f * f), 3f + 2f * f);
				RiftDraw.Line(l - new Vector2(0f, 1f), r - new Vector2(0f, 1f), RiftDraw.Glow(220, 255, 248, 0.3f + 0.6f * f), 1.5f + f);
				RiftDraw.Line(l, l - new Vector2(0f, 12f), Sea * (0.4f + 0.5f * f), 2f);
				RiftDraw.Line(r, r - new Vector2(0f, 12f), Sea * (0.4f + 0.5f * f), 2f);
				if (sub == Airborne)
				{
					float near = MathHelper.Clamp(1f - (ground - NPC.Bottom.Y) / 320f, 0.25f, 1f);
					Texture2D flare = ModContent.Request<Texture2D>("Terrapex/Content/Projectiles/RiftFlare").Value;
					Main.EntitySpriteDraw(flare, new Vector2(slamX, ground) - Main.screenPosition, null, Color.Black * (0.5f * near), 0f,
						flare.Size() * 0.5f, new Vector2(0.6f + 1.2f * near, 0.18f + 0.12f * near), SpriteEffects.None, 0);
				}
			}

			// the hull: how close the shell is to giving, so the dial is on screen (the cracks on
			// the planks say the same thing on the boss itself)
			if (InShell && sub == Hiding)
			{
				float crack = CrackProgress;
				Vector2 a = NPC.Top + new Vector2(-40f, -26f);
				RiftDraw.Line(a - new Vector2(2f, 0f), a + new Vector2(82f, 0f), SFX.Outline * 0.85f, 8f);
				RiftDraw.Line(a, a + new Vector2(80f, 0f), new Color(30, 40, 50) * 0.7f, 6f);
				RiftDraw.Line(a + new Vector2(1f, 0f), a + new Vector2(1f + 78f * crack, 0f), Powder * 0.9f, 4f);
				RiftDraw.Line(a + new Vector2(1f, -1f), a + new Vector2(1f + 78f * crack, -1f), RiftDraw.Glow(255, 230, 180, 0.5f), 1f);
				// a notch per crack stage
				for (int i = 1; i < CrackStages; i++)
				{
					Vector2 n = a + new Vector2(80f * i / CrackStages, 0f);
					RiftDraw.Line(n - new Vector2(0f, 3f), n + new Vector2(0f, 3f), SFX.Outline * 0.8f, 1f);
				}
				if (crack > 0f)
					RiftDraw.Bloom(a + new Vector2(1f + 78f * crack, 0f), RiftDraw.Glow(255, 170, 90, 0.4f + 0.4f * crack), 0.14f);
			}
		}

		/// <summary>
		/// Where a ball lands: a dashed ring that rotates and closes in from wide onto the blast's
		/// own radius by the moment of firing, four brackets, a centre cross, and a faint fill.
		/// Once the ball is in the air it holds at the radius and throbs until the burst.
		/// </summary>
		private void DrawReticle(Vector2 center, float f, bool inFlight)
		{
			Texture2D px = TextureAssets.MagicPixel.Value;
			float tighten = (1f - f) * (1f - f);
			float radius = ShellbackCannonball.BlastRadius * (1f + 0.7f * tighten);
			float beat = inFlight ? 0.75f + 0.25f * MathF.Sin(Timer * 0.6f) : 1f;
			float alpha = (0.2f + 0.6f * f) * beat;
			Color col = Powder * alpha;
			float rot = Timer * 0.03f;

			RiftDraw.Bloom(center, RiftDraw.Glow(255, 150, 80, (0.1f + 0.18f * f) * beat),
				ShellbackCannonball.BlastRadius * 2f / 64f);
			int dashes = 24;
			for (int i = 0; i < dashes; i++)
			{
				if (i % 3 == 2)
					continue;
				Vector2 at = center + Vector2.UnitX.RotatedBy(rot + MathHelper.TwoPi * i / dashes) * radius - Main.screenPosition;
				Main.EntitySpriteDraw(px, at, new Rectangle(0, 0, 1, 1), col, 0f, new Vector2(0.5f), 3f, SpriteEffects.None, 0);
			}
			for (int k = 0; k < 4; k++)
			{
				Vector2 d = Vector2.UnitX.RotatedBy(-rot * 1.5f + MathHelper.PiOver4 + k * MathHelper.PiOver2);
				Vector2 p = center + d * (radius + 6f);
				RiftDraw.Line(p, p - d * 9f, col, 2f);
			}
			RiftDraw.Line(center - new Vector2(5f, 0f), center + new Vector2(5f, 0f), col, 2f);
			RiftDraw.Line(center - new Vector2(0f, 5f), center + new Vector2(0f, 5f), col, 2f);
		}
	}
}
