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
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.NPCs.Bosses
{
	/// <summary>
	/// The Heave — what comes up the crack once the Breach has been held open long enough.
	///
	/// The mod already has four bosses and every one of them is a large body in the middle of an
	/// arena, so a fifth of those would read as the event having no finale of its own. This one
	/// never leaves the floor: it walks, it crouches, it drives plates of stone up out of the
	/// ground, and the fight is about where you are standing rather than about where it is.
	///
	/// Its size is the Breach's own dial. Every mouth left standing when the third wave closes
	/// adds <see cref="LifePerMouth"/> health and <see cref="DamagePerMouth"/> damage, which is
	/// what makes leaving mouths up an actual decision instead of free money: the pay-out and
	/// the bill are the same number.
	///
	/// Attacks run a fixed rotation, like every other boss here. Nothing it fires homes.
	/// Heave.png is six frames of 128x88: 0-3 the walk, 4 crouched, 5 reared.
	/// </summary>
	[AutoloadBossHead]
	public class Heave : ModNPC
	{
		/// <summary>What each mouth left standing is worth to it. See EVENT_BREACH.md.</summary>
		public const int LifePerMouth = 1200;
		public const int DamagePerMouth = 6;

		private const int FrameCrouch = 4;
		private const int FrameRear = 5;

		private const float PhaseTwoThreshold = 0.5f;

		// how long it walks between attacks, per phase
		private const int Gap = 110;
		private const int GapPhaseTwo = 74;

		// ---- attack ids, in rotation order
		private const int Walk = 0;
		private const int Upthrust = 1;
		private const int Scree = 2;
		private const int Shelf = 3;
		private const int Grind = 4;

		private static readonly int[] Rotation = { Upthrust, Scree, Shelf, Grind };

		private ref float Action => ref NPC.ai[0];
		private ref float Timer => ref NPC.ai[1];
		private ref float Phase => ref NPC.ai[2];
		private ref float Step => ref NPC.ai[3];

		private readonly BossDeath death = new();

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 6;
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			NPCID.Sets.BossBestiaryPriority.Add(Type);
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
		}

		public override void SetDefaults()
		{
			NPC.width = 112;
			NPC.height = 80;
			NPC.damage = 70;
			NPC.defense = 34;
			NPC.lifeMax = 9000;
			NPC.HitSound = SoundID.NPCHit41;
			NPC.knockBackResist = 0f;
			NPC.value = Item.buyPrice(gold: 8);
			NPC.aiStyle = -1;
			NPC.boss = true;
			NPC.npcSlots = 10f;
			NPC.noGravity = false;      // it is a plate of rock. It does not fly, ever
			NPC.noTileCollide = false;
			NPC.DeathSound = SoundID.NPCDeath43;
			Music = MusicID.Boss2;
		}

		/// <summary>
		/// Its own scaling, for the reason CLAUDE.md gives: an event finale on default scaling is
		/// cheaper than it is worth, and this one is meant to be fought again every run.
		/// </summary>
		public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
		{
			NPC.lifeMax = (int)(NPC.lifeMax * 0.62f * balance);
			NPC.damage = (int)(NPC.damage * 0.85f);
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
			=> bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Caverns,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.Heave.Bestiary")
			});

		// ------------------------------------------------------------------ the fight

		public override void AI()
		{
			// nothing else runs while it comes apart — no phase check, no attack, no walking
			if (death.Dying)
			{
				death.Tick(NPC, DustID.Stone, new Color(190, 130, 220));
				return;
			}

			NPC.TargetClosest();
			Player target = Main.player[NPC.target];
			if (!target.active || target.dead)
			{
				NPC.TargetClosest();
				target = Main.player[NPC.target];
				if (!target.active || target.dead)
				{
					NPC.velocity.X *= 0.9f;
					NPC.EncourageDespawn(60);
					return;
				}
			}

			if (Phase == 0f && NPC.life <= NPC.lifeMax * PhaseTwoThreshold)
				EnterPhaseTwo();

			Timer++;

			switch ((int)Action)
			{
				case Upthrust: DoUpthrust(target); break;
				case Scree: DoScree(target); break;
				case Shelf: DoShelf(target); break;
				case Grind: DoGrind(); break;
				default: DoWalk(target); break;
			}
		}

		private void EnterPhaseTwo()
		{
			Phase = 1f;
			NPC.netUpdate = true;
			SoundEngine.PlaySound(SoundID.NPCDeath43, NPC.Center);
			if (Main.netMode != NetmodeID.Server)
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(
					NPC.Center, Main.rand.NextVector2Unit(), 14f, 6f, 30, 1600f, "Heave"));
			for (int i = 0; i < 45; i++)
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Stone,
					Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-7f, 2f), 60, default, 1.5f);
				d.noGravity = true;
			}
			if (Main.netMode != NetmodeID.MultiplayerClient)
				ChatHelper.BroadcastChatMessage(
					NetworkText.FromKey("Mods.Terrapex.Chat.HeavePhase2"), new Color(210, 150, 240));
		}

		/// <summary>Walk at the player until the gap runs out, then take the next attack in the rotation.</summary>
		private void DoWalk(Player target)
		{
			Chase(target, Phase == 0f ? 2.4f : 3.1f);

			if (Timer < (Phase == 0f ? Gap : GapPhaseTwo))
				return;

			Action = Rotation[(int)Step % Rotation.Length];
			Step++;
			Timer = 0f;
			NPC.netUpdate = true;
		}

		/// <summary>
		/// Ground movement. It steps up ledges and it does not jump — a boss that can hop out of
		/// its own plates would undo the attack the whole fight is built around.
		/// </summary>
		private void Chase(Player target, float top)
		{
			float dir = Math.Sign(target.Center.X - NPC.Center.X);
			NPC.direction = NPC.spriteDirection = dir >= 0f ? 1 : -1;
			NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X + dir * 0.09f, -top, top);

			if (NPC.collideX && NPC.velocity.Y == 0f)
				NPC.velocity.Y = -6.4f;         // a step up, not a leap

			if (NPC.velocity.Y == 0f && Math.Abs(NPC.velocity.X) > 1f && Main.rand.NextBool(3))
			{
				Dust d = Dust.NewDustPerfect(
					new Vector2(NPC.Center.X + Main.rand.NextFloat(-NPC.width * 0.5f, NPC.width * 0.5f),
						NPC.Bottom.Y - 2f),
					DustID.Stone, new Vector2(-NPC.velocity.X * 0.2f, -Main.rand.NextFloat(0.4f, 1.6f)),
					120, default, 1f);
				d.velocity.X += Main.rand.NextFloat(-0.4f, 0.4f);
			}
		}

		private void Recover(int after)
		{
			if (Timer < after)
				return;
			Action = Walk;
			Timer = 0f;
			NPC.netUpdate = true;
		}

		/// <summary>
		/// Four plates walked toward the player along the floor. The slabs carry their own
		/// telegraph, so the boss only has to stand still and rear while they crack.
		/// </summary>
		private void DoUpthrust(Player target)
		{
			NPC.velocity.X *= 0.85f;

			if (Timer == 6f)
			{
				SoundEngine.PlaySound(SoundID.Item70 with { Pitch = -0.7f }, NPC.Center);
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					int dir = Math.Sign(target.Center.X - NPC.Center.X);
					if (dir == 0)
						dir = NPC.direction;
					for (int i = 0; i < 4; i++)
					{
						Vector2 at = new Vector2(NPC.Center.X + dir * (110f + i * 90f), NPC.Bottom.Y - 8f);
						Projectile.NewProjectile(NPC.GetSource_FromAI(), at, Vector2.Zero,
							ModContent.ProjectileType<HeaveSlab>(), NPC.damage / 4, 6f, Main.myPlayer,
							0f, Phase);
					}
				}
			}

			Recover(HeaveSlab.Crack + 24);
		}

		/// <summary>
		/// Nine stones out of the ceiling over the player. Every one of them lands its own ring
		/// marker before it falls, which is what keeps a fan of rock from being a coin flip.
		/// </summary>
		private void DoScree(Player target)
		{
			NPC.velocity.X *= 0.9f;

			if (Timer < 40f)
			{
				// dust shaken loose above the player: the attack announced before it is aimed
				if (Main.rand.NextBool(2))
				{
					Dust d = Dust.NewDustPerfect(
						target.Center + new Vector2(Main.rand.NextFloat(-220f, 220f), -Main.rand.NextFloat(180f, 300f)),
						DustID.Stone, new Vector2(0f, Main.rand.NextFloat(0.4f, 1.4f)), 130, default, 0.9f);
					d.noGravity = true;
				}
				return;
			}

			if (Timer == 40f)
			{
				SoundEngine.PlaySound(SoundID.Item14, NPC.Center);
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					int damage = NPC.damage / 3;
					for (int i = 0; i < 9; i++)
					{
						Vector2 at = new Vector2(
							target.Center.X + (i - 4) * 52f + Main.rand.NextFloat(-14f, 14f),
							target.Center.Y - 330f - Main.rand.NextFloat(0f, 50f));
						Projectile.NewProjectile(NPC.GetSource_FromAI(), at, Vector2.Zero,
							ModContent.ProjectileType<Rockfall>(), damage, 4f, Main.myPlayer);
					}
				}
			}

			Recover(76);
		}

		/// <summary>
		/// It crouches, throws itself up and comes down flat: a wave along the floor in each
		/// direction. The answer is a jump, and that is the only attack in the fight it is.
		/// </summary>
		private void DoShelf(Player target)
		{
			if (Timer < 36f)
			{
				NPC.velocity.X *= 0.8f;
				if (Main.rand.NextBool(3))
				{
					Dust d = Dust.NewDustPerfect(
						new Vector2(NPC.Center.X + Main.rand.NextFloat(-50f, 50f), NPC.Bottom.Y - 4f),
						DustID.Stone, new Vector2(0f, -Main.rand.NextFloat(0.5f, 2f)), 120, default, 1.1f);
					d.noGravity = true;
				}
				return;
			}

			if (Timer == 36f)
			{
				NPC.velocity.Y = -11.5f;
				NPC.velocity.X = Math.Sign(target.Center.X - NPC.Center.X) * 3.2f;
				SoundEngine.PlaySound(SoundID.Item62 with { Pitch = -0.3f }, NPC.Center);
				NPC.netUpdate = true;
				return;
			}

			// landed: the ground answers. The cap is there so a boss that lands somewhere odd
			// still finishes the move instead of hanging on a condition that never comes true
			if (Timer > 46f && (NPC.velocity.Y == 0f || Timer > 150f))
			{
				Land();
				Action = Walk;
				Timer = 0f;
				NPC.netUpdate = true;
			}
		}

		private void Land()
		{
			SoundEngine.PlaySound(SoundID.Item14, NPC.Center);
			if (Main.netMode != NetmodeID.Server)
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(
					NPC.Center, Vector2.UnitY, 11f, 6f, 22, 1600f, "Heave"));

			for (int i = 0; i < 30; i++)
			{
				Dust d = Dust.NewDustPerfect(
					new Vector2(NPC.Center.X + Main.rand.NextFloat(-56f, 56f), NPC.Bottom.Y - 2f),
					DustID.Stone, new Vector2(Main.rand.NextFloat(-5f, 5f), -Main.rand.NextFloat(0.5f, 4f)),
					100, default, 1.3f);
				d.velocity.X *= 1.2f;
			}

			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			for (int side = -1; side <= 1; side += 2)
				Projectile.NewProjectile(NPC.GetSource_FromAI(),
					new Vector2(NPC.Center.X + side * 40f, NPC.Bottom.Y - 12f),
					new Vector2(side * 8.5f, 0f), ModContent.ProjectileType<HeaveWave>(),
					NPC.damage / 3, 7f, Main.myPlayer);
		}

		/// <summary>
		/// Five seconds forward, chewing the stone in front of it the way the Colossus does. It
		/// commits to the direction it turned to: walking around it is the dodge, and a Grind
		/// that steered would be a homing attack wearing a pattern's clothes.
		/// </summary>
		private void DoGrind()
		{
			if (Timer < 30f)
			{
				NPC.velocity.X *= 0.8f;
				if (Timer == 1f)
					SoundEngine.PlaySound(SoundID.Item23 with { Pitch = -0.6f }, NPC.Center);
				return;
			}

			NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X + NPC.direction * 0.22f, -5.2f, 5.2f);

			if (Timer % 8f == 0f)
				ChewTiles();

			if (Main.rand.NextBool(2))
			{
				Dust d = Dust.NewDustPerfect(
					new Vector2(NPC.Center.X + NPC.direction * NPC.width * 0.5f,
						NPC.Center.Y + Main.rand.NextFloat(-30f, 34f)),
					DustID.Stone, new Vector2(-NPC.direction * Main.rand.NextFloat(1f, 4f),
						-Main.rand.NextFloat(0f, 2f)), 100, default, 1.2f);
				d.noGravity = true;
			}

			Recover(330);
		}

		private void ChewTiles()
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			int x = (int)((NPC.Center.X + NPC.direction * (NPC.width * 0.5f + 8f)) / 16f);
			int yTop = (int)((NPC.Center.Y - NPC.height * 0.42f) / 16f);

			for (int y = yTop; y <= yTop + 4; y++)
			{
				if (!WorldGen.InWorld(x, y, 10))
					continue;
				Tile tile = Main.tile[x, y];
				if (!tile.HasTile || Main.tileDungeon[tile.TileType] || TileID.Sets.BasicChest[tile.TileType])
					continue;

				WorldGen.KillTile(x, y);
				if (Main.netMode == NetmodeID.Server)
					NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, x, y);
			}
		}

		// ------------------------------------------------------------------ presentation

		public override void FindFrame(int frameHeight)
		{
			int f;
			if (Action == Shelf && Timer < 36f)
				f = FrameCrouch;
			else if (Action == Upthrust || (Action == Grind && Timer < 30f))
				f = FrameRear;
			else
			{
				NPC.frameCounter += Math.Min(Math.Abs(NPC.velocity.X) * 0.06f, 0.28) + 0.05;
				if (NPC.frameCounter >= 4.0)
					NPC.frameCounter = 0.0;
				f = (int)NPC.frameCounter;
			}
			NPC.frame.Y = f * frameHeight;
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 30 : 6); i++)
			{
				Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Stone,
					hit.HitDirection, -1f, 90, default, 1.2f);
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
			=> npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Ejecta>(), 1, 30, 50));

		public override void OnKill()
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				BreachSystem.downedHeave = true;
				BreachSystem.End(won: true);
				if (Main.netMode == NetmodeID.Server)
					NetMessage.SendData(MessageID.WorldData);
			}
		}

		/// <summary>
		/// The throes. The plates go with it: a permanent slab left standing after the fight is
		/// over would keep the arena walled in through the part that is supposed to be over.
		/// </summary>
		public override bool CheckDead()
			=> death.CheckDead(NPC, 170, ClearHazards);

		private static void ClearHazards()
		{
			int slab = ModContent.ProjectileType<HeaveSlab>();
			int wave = ModContent.ProjectileType<HeaveWave>();
			int rock = ModContent.ProjectileType<Rockfall>();
			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				Projectile p = Main.projectile[i];
				if (p.active && (p.type == slab || p.type == wave || p.type == rock))
					p.Kill();
			}
		}

		public override bool CheckActive() => false;
	}
}
