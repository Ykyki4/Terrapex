using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Particles;
using Terrapex.Content.Projectiles;
using Terrapex.Content.Projectiles.Trees.Accessories;

namespace Terrapex.Common.Players
{
	/// <summary>
	/// The two accessory trees of CRAFTING_TREES.md: the Wayfarer boots line and the Squall
	/// Aegis dash line.
	///
	/// Every node keeps every effect of what went into it, vanilla ingredients included, so
	/// each effect lives here once, in <see cref="TreeEffects"/>, behind a flag. A node calls
	/// the effects of its ingredients and the flag makes sure nothing applies twice when the
	/// player also wears one of the ingredients next to it — Wayfarer's Boots beside a pair of
	/// Tidewalker Sandals is still one +25% in water, not two.
	///
	/// The dashes follow ExampleMod's ExampleDashPlayer: the double tap is read in
	/// <see cref="ResetEffects"/>, the dash starts in <see cref="PreUpdateMovement"/>. Dash
	/// timing is local to the owner — player velocity syncs by itself — and anything the dash
	/// does to the world (the gust, the strike, the thread) is a projectile the owner spawns,
	/// which is what syncs it.
	/// </summary>
	public class TreeAccessoryPlayer : ModPlayer
	{
		// ---- effect flags (dedupe) -------------------------------------------------------
		public bool dusty, tide, sand, rime, hermes, wayfarer, rocket, landing, lavaWaders, anklet,
			threadstep, terraspark, obsidian, turtle, paladin, ninja;

		public bool sandVisual, threadVisual;

		/// <summary>0 none, 1 Gull Feather, 2 Squall (vanilla shield dash + gust), 3 Rift,
		/// 4 Weaver, 5 Regard. The highest equipped wins.</summary>
		public int dashTier;

		// ---- dash state ------------------------------------------------------------------
		public const int DashRight = 2, DashLeft = 3;
		private int dashDir = -1;
		private int dashDelay;
		private int dashTimer;   // ticks of the dash still held
		private int dashTierUsed;
		private Vector2 dashAim;
		private Vector2 dashStart;
		private int prevVanillaDashDelay;

		/// <summary>Regard: NPCs the last dash passed through, and how long they stay marked.</summary>
		public readonly HashSet<int> regardMarked = new();
		public int regardTimer;

		// ---- landing / thread walk ---------------------------------------------------------
		private float airPeakY = float.NaN;
		private float prevVelY;
		private int threadWalk;        // ticks spent on the thread this airborne stretch
		private bool onThread;
		private float threadY;
		private int threadProj = -1;
		private bool wasGrounded = true;
		private int sandTick;
		private int airTicks;

		public const int ThreadStandTicks = 60;

		public override void ResetEffects()
		{
			dusty = tide = sand = rime = hermes = wayfarer = rocket = landing = lavaWaders = anklet =
				threadstep = terraspark = obsidian = turtle = paladin = ninja = false;
			sandVisual = threadVisual = false;
			dashTier = 0;

			// ExampleDashPlayer's double tap, horizontal only: all five dashes of the line are
			// sideways except Regard's cursor dash, which has its own key
			if (Player.controlRight && Player.releaseRight && Player.doubleTapCardinalTimer[DashRight] < 15
				&& Player.doubleTapCardinalTimer[DashLeft] == 0)
				dashDir = DashRight;
			else if (Player.controlLeft && Player.releaseLeft && Player.doubleTapCardinalTimer[DashLeft] < 15
				&& Player.doubleTapCardinalTimer[DashRight] == 0)
				dashDir = DashLeft;
			else
				dashDir = -1;
		}

		private bool CustomDash => dashTier == 1 || dashTier >= 3;

		public override void PostUpdateEquips()
		{
			// the higher nodes replace whatever dash something else asked for — Master Ninja
			// Gear's Tabi dash is one of their own ingredients and theirs is the better dash
			if (dashTier >= 3)
				Player.dashType = 0;
		}

		// ---- dash ----------------------------------------------------------------------------
		public override void PreUpdateMovement()
		{
			if (dashDelay > 0)
				dashDelay--;

			if (Player.whoAmI == Main.myPlayer && CustomDash && dashDelay == 0 && dashTimer == 0
				&& Player.dashType == 0 && !Player.mount.Active && !Player.CCed)
			{
				Vector2 aim = Vector2.Zero;
				if (dashTier == 5 && RegardDashKeybind.UsesKey)
				{
					if (RegardDashKeybind.Dash.JustPressed)
						aim = (Main.MouseWorld - Player.Center).SafeNormalize(new Vector2(Player.direction, 0f));
				}
				else if (dashDir == DashRight)
					aim = Vector2.UnitX;
				else if (dashDir == DashLeft)
					aim = -Vector2.UnitX;

				if (aim != Vector2.Zero)
					StartDash(aim);
			}

			if (dashTimer > 0)
				HoldDash();

			ThreadWalk();
		}

		private static float SpeedOf(int tier) => tier switch
		{
			1 => 9.5f,
			3 => 14f,
			4 => 15f,
			_ => 16f,
		};

		private static int HoldOf(int tier) => tier == 1 ? 1 : (tier == 5 ? 12 : 11);

		private void StartDash(Vector2 aim)
		{
			dashTierUsed = dashTier;
			dashAim = aim;
			dashStart = Player.Center;
			dashTimer = HoldOf(dashTier);
			dashDelay = dashTier == 1 ? 55 : 42;
			float speed = SpeedOf(dashTier);

			if (dashTier == 1)
			{
				// Tabi's dash, softer: one shove, no hold, the run speed bleeds it off
				if (aim.X > 0 && Player.velocity.X < speed || aim.X < 0 && Player.velocity.X > -speed)
					Player.velocity.X = aim.X * speed;
				if (!Main.dedServ)
					for (int i = 0; i < 8; i++)
						ParticleSystem.Spawn(new Smoke(Player.Center + Main.rand.NextVector2Circular(10f, 18f),
							new Vector2(-aim.X * Main.rand.NextFloat(1f, 2.5f), Main.rand.NextFloat(-0.6f, 0.6f)),
							new Color(232, 236, 240), Main.rand.NextFloat(0.18f, 0.3f), Main.rand.Next(18, 28)));
				SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.5f, Pitch = 0.4f }, Player.Center);
				dashTimer = 14; // only the afterimage
				return;
			}

			Player.velocity = aim * speed;
			regardMarked.Clear();
			regardTimer = 0;

			var src = Player.GetSource_FromThis();
			int dmg = dashTier switch { 3 => 60, 4 => 90, _ => 140 };
			dmg = (int)Player.GetTotalDamage(DamageClass.Melee).ApplyTo(dmg);
			Projectile.NewProjectile(src, Player.Center, Vector2.Zero, ModContent.ProjectileType<AegisStrike>(),
				dmg, 3f, Player.whoAmI, dashTier);
			Projectile.NewProjectile(src, Player.Center + aim * 30f, aim * speed * 1.15f,
				ModContent.ProjectileType<AegisGust>(), 0, 0f, Player.whoAmI, 0f, dashTier);
			SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.45f, Pitch = 0.5f }, Player.Center);
		}

		private void HoldDash()
		{
			Player.eocDash = dashTimer;
			Player.armorEffectDrawShadowEOCShield = true;
			dashTimer--;

			if (dashTierUsed == 1)
				return;

			float speed = SpeedOf(dashTierUsed);
			Player.velocity = dashAim * speed;
			// fall damage from a downward cursor dash would punish the move for working
			Player.fallStart = (int)(Player.position.Y / 16f);

			if (!Main.dedServ)
			{
				Color c = DashColour(dashTierUsed);
				ParticleSystem.Spawn(new Spark(Player.Center + Main.rand.NextVector2Circular(8f, 16f),
					-dashAim * Main.rand.NextFloat(2f, 5f), c, Main.rand.NextFloat(0.7f, 1.1f), 16));
			}

			if (dashTimer == 0)
				EndDash();
		}

		private void EndDash()
		{
			// out of the dash the run speed takes over again; keeping all of a cursor dash's
			// speed would fling the player twice as far as they aimed
			Player.velocity = dashAim * SpeedOf(dashTierUsed) * 0.45f;

			// Rift: a short invulnerability on the far side of the pack
			Player.SetImmuneTimeForAllTypes(24);

			if (Player.whoAmI != Main.myPlayer)
				return;

			if (dashTierUsed >= 4)
			{
				int dmg = (int)Player.GetTotalDamage(DamageClass.Melee).ApplyTo(dashTierUsed == 4 ? 55 : 80);
				FriendlyThread.Between(Player.GetSource_FromThis(), dashStart, Player.Center, dmg, Player.whoAmI, 120);
			}

			if (dashTierUsed == 5)
			{
				regardTimer = 60;
				if (!Main.dedServ)
					ParticleSystem.Spawn(new Shockwave(Player.Center, new Color(255, 215, 154), 0.1f, 0.9f, 20));
			}
		}

		public static Color DashColour(int tier) => tier switch
		{
			3 => new Color(255, 154, 217),
			4 => new Color(127, 242, 224),
			5 => new Color(255, 215, 154),
			_ => new Color(220, 232, 240),
		};

		public bool Dashing => dashTimer > 0 && dashTierUsed >= 3;

		// passing *through* the pack: no contact damage while the dash is held
		public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot) => !Dashing;

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (regardTimer > 0 && regardMarked.Contains(target.whoAmI))
				modifiers.FinalDamage *= 1.3f;
		}

		// ---- landing, the vanilla shield dash's gust, sand, regard ---------------------------
		public override void PostUpdate()
		{
			// the thread holds the player level: whatever gravity added this tick comes back off,
			// and a jump (velocity.Y < 0) is the player leaving it
			if (onThread && Player.whoAmI == Main.myPlayer)
			{
				if (Player.velocity.Y < 0f)
					StopThread();
				else
				{
					Player.position.Y = threadY - Player.height;
					Player.velocity.Y = 0f;
				}
			}

			bool grounded = Player.velocity.Y == 0f && !onThread;

			// Squall: the vanilla Shield of Cthulhu dash, with a gust pushed ahead of it.
			// dashDelay goes to -1 for the length of any vanilla dash.
			if (dashTier == 2 && Player.whoAmI == Main.myPlayer && Player.dashDelay == -1 && prevVanillaDashDelay != -1)
			{
				Vector2 aim = new Vector2(Player.velocity.X >= 0 ? 1 : -1, 0f);
				Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center + aim * 30f,
					aim * 14f, ModContent.ProjectileType<AegisGust>(), 0, 0f, Player.whoAmI, 0f, 2);
			}
			prevVanillaDashDelay = Player.dashDelay;

			// Riftstride: a landing from height knocks the ground out from under whoever is near
			if (!grounded)
			{
				if (float.IsNaN(airPeakY) || Player.position.Y < airPeakY)
					airPeakY = Player.position.Y;
			}
			else
			{
				if (landing && !wasGrounded && !float.IsNaN(airPeakY) && Player.whoAmI == Main.myPlayer
					&& Player.position.Y - airPeakY > 16f * 12f && prevVelY > 6f && Player.gravDir == 1f)
				{
					Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Bottom, Vector2.Zero,
						ModContent.ProjectileType<AegisGust>(), 0, 0f, Player.whoAmI, 1f, 3);
				}
				airPeakY = float.NaN;
			}
			wasGrounded = grounded;
			prevVelY = Player.velocity.Y;

			if (regardTimer > 0)
			{
				regardTimer--;
				if (!Main.dedServ && Main.rand.NextBool(3))
					foreach (int i in regardMarked)
					{
						NPC n = Main.npc[i];
						if (n.active)
							ParticleSystem.Spawn(new Spark(n.Center + Main.rand.NextVector2Circular(n.width * 0.5f, n.height * 0.5f),
								new Vector2(0f, -1.5f), new Color(255, 159, 74), 0.7f, 18));
					}
				if (regardTimer == 0)
					regardMarked.Clear();
			}

			if (sand && Player.whoAmI == Main.myPlayer)
				SandKick();
		}

		private void SandKick()
		{
			if (Player.velocity.Y != 0f || System.Math.Abs(Player.velocity.X) < 4f)
				return;
			if (++sandTick % 8 != 0)
				return;

			int back = -System.Math.Sign(Player.velocity.X);
			Rectangle behind = new Rectangle((int)(back > 0 ? Player.Right.X : Player.Left.X - 96),
				(int)Player.position.Y - 8, 96, Player.height + 8);

			if (sandVisual && !Main.dedServ)
				for (int i = 0; i < 3; i++)
					ParticleSystem.Spawn(new Smoke(Player.Bottom + new Vector2(back * 6f, -4f),
						new Vector2(back * Main.rand.NextFloat(1.5f, 3.5f), -Main.rand.NextFloat(0.5f, 1.8f)),
						new Color(226, 196, 130), Main.rand.NextFloat(0.2f, 0.32f), Main.rand.Next(26, 40)));

			foreach (NPC n in Main.ActiveNPCs)
			{
				if (n.friendly || n.boss || n.dontTakeDamage || !n.Hitbox.Intersects(behind))
					continue;
				// AddBuff from a client sends NPCAddBuff, the server applies it
				n.AddBuff(BuffID.Confused, 120);
			}
		}

		// ---- Threadstep: off a ledge at a run, the thread runs out under your feet -----------
		private void ThreadWalk()
		{
			if (Player.whoAmI != Main.myPlayer)
				return;

			bool grounded = Player.velocity.Y == 0f && !onThread;
			if (grounded)
			{
				threadWalk = 0;
				airTicks = 0;
				threadY = Player.Bottom.Y;
			}
			else if (!onThread)
				airTicks++;

			if (!threadstep || Player.gravDir != 1f || Player.mount.Active || Dashing)
			{
				StopThread();
				return;
			}

			bool running = (Player.controlLeft || Player.controlRight) && System.Math.Abs(Player.velocity.X) > 5f;

			if (!onThread)
			{
				// step off an edge mid-run, still moving level or down: the thread catches you
				// (the first airborne tick or two: gravity has already pulled a pixel off the ledge)
				if (!grounded && airTicks <= 2 && running && Player.velocity.Y >= 0f && threadWalk == 0)
				{
					onThread = true;
					Player.position.Y = threadY - Player.height;
					Player.velocity.Y = 0f;
					threadProj = Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Bottom, Vector2.Zero,
						ModContent.ProjectileType<StepThread>(), 0, 0f, Player.whoAmI, Player.Bottom.X, Player.Bottom.Y);
				}
				return;
			}

			threadWalk++;
			if (!running || threadWalk > ThreadStandTicks || Player.controlDown || Player.velocity.Y < 0f)
			{
				// a jump leaves velocity.Y negative, which is the vanilla jump working off the thread
				StopThread();
				return;
			}

			// standing still in Y means vanilla's own jump check sees the player as grounded
			Player.position.Y = threadY - Player.height;
			Player.velocity.Y = 0f;
			Player.fallStart = (int)(Player.position.Y / 16f);
		}

		public override void PostUpdateRunSpeeds()
		{
			// gravity is added after PreUpdateMovement; undo it while on the thread
			if (onThread && Player.whoAmI == Main.myPlayer)
				Player.gravity = 0f;
		}

		private void StopThread()
		{
			if (!onThread)
				return;
			onThread = false;
			threadWalk = ThreadStandTicks + 1; // one thread per airborne stretch
			if (threadProj >= 0 && Main.projectile[threadProj].active
				&& Main.projectile[threadProj].type == ModContent.ProjectileType<StepThread>())
			{
				Main.projectile[threadProj].ai[2] = 1f;
				Main.projectile[threadProj].netUpdate = true;
			}
			threadProj = -1;
		}

		public bool OnThread => onThread;
	}

	/// <summary>
	/// Every effect of both trees once. Each method sets its flag and returns early if it was
	/// already set this tick, so a node and its ingredients never stack the same line twice.
	/// </summary>
	public static class TreeEffects
	{
		private static TreeAccessoryPlayer M(Player p) => p.GetModPlayer<TreeAccessoryPlayer>();

		// ---------- boots ----------

		/// <summary>Dusty Boots, as DustyBoots.cs has them.</summary>
		public static void Dusty(Player p, bool hide)
		{
			var m = M(p);
			if (m.dusty) return;
			m.dusty = true;
			p.moveSpeed += 0.12f;
			p.runAcceleration += 0.02f;
			p.buffImmune[BuffID.Slow] = true;
		}

		/// <summary>Tidewalker Sandals: flippers, water walking, +25% speed in water.</summary>
		public static void Tidewalker(Player p, bool hide)
		{
			var m = M(p);
			if (m.tide) return;
			m.tide = true;
			p.accFlipper = true;
			p.waterWalk2 = true;
			if (p.wet && !p.lavaWet && !p.honeyWet)
				p.moveSpeed += 0.25f;
		}

		private static bool OnTile(Player p, params ushort[] types)
		{
			if (p.velocity.Y != 0f) return false;
			for (int dx = -1; dx <= 1; dx++)
			{
				Tile t = Framing.GetTileSafely((int)((p.Bottom.X + dx * 8f) / 16f), (int)((p.Bottom.Y + 4f) / 16f));
				if (!t.HasTile) continue;
				foreach (ushort ty in types)
					if (t.TileType == ty) return true;
			}
			return false;
		}

		private static readonly ushort[] SandTiles = { TileID.Sand, TileID.Ebonsand, TileID.Crimsand, TileID.Pearlsand,
			TileID.HardenedSand, TileID.CorruptHardenedSand, TileID.CrimsonHardenedSand, TileID.HallowHardenedSand,
			TileID.Sandstone, TileID.CorruptSandstone, TileID.CrimsonSandstone, TileID.HallowSandstone };

		private static readonly ushort[] SnowTiles = { TileID.SnowBlock, TileID.IceBlock, TileID.CorruptIce,
			TileID.FleshIce, TileID.HallowedIce, TileID.BreakableIce };

		/// <summary>Sandstrider Wraps: sand does not hold you, the desert is faster, the run kicks
		/// up blinding sand behind you.</summary>
		public static void Sandstrider(Player p, bool hide)
		{
			var m = M(p);
			if (m.sand) return;
			m.sand = true;
			m.sandVisual = !hide;
			p.buffImmune[BuffID.WindPushed] = true;
			if (p.ZoneDesert || p.ZoneUndergroundDesert)
				p.moveSpeed += 0.10f;
			if (OnTile(p, SandTiles))
				p.runAcceleration *= 1.35f;
		}

		/// <summary>Rimetread Boots: Ice Skates, and a quicker start on snow and ice.</summary>
		public static void Rimetread(Player p, bool hide)
		{
			var m = M(p);
			if (m.rime) return;
			m.rime = true;
			p.iceSkate = true;
			if (OnTile(p, SnowTiles))
				p.runAcceleration *= 1.5f;
		}

		/// <summary>Hermes / Flurry / Sailfish Boots.</summary>
		public static void Hermes(Player p, float run = 6f)
		{
			if (p.accRunSpeed < run)
				p.accRunSpeed = run;
			M(p).hermes = true;
		}

		public static void Wayfarer(Player p, bool hide)
		{
			Dusty(p, hide);
			Tidewalker(p, hide);
			Sandstrider(p, hide);
			Rimetread(p, hide);
			Hermes(p, 6f);
			var m = M(p);
			if (m.wayfarer) return;
			m.wayfarer = true;
			p.moveSpeed += 0.08f;
		}

		/// <summary>Rocket Boots — or better, when a later node passes a higher id.</summary>
		public static void Rocket(Player p, int id)
		{
			M(p).rocket = true;
			if (p.rocketBoots < id)
				p.rocketBoots = id;
			if (p.vanityRocketBoots < id)
				p.vanityRocketBoots = id;
		}

		public static void Riftstride(Player p, bool hide)
		{
			Wayfarer(p, hide);
			Rocket(p, 2); // Spectre's flight and visuals
			M(p).landing = true;
		}

		/// <summary>Lava Waders: fire blocks, water/lava walking, lava.</summary>
		public static void LavaWaders(Player p)
		{
			var m = M(p);
			if (m.lavaWaders) return;
			m.lavaWaders = true;
			p.waterWalk = true;
			p.fireWalk = true;
			p.lavaImmune = true;
			p.lavaRose = true;
		}

		/// <summary>Anklet of the Wind.</summary>
		public static void Anklet(Player p)
		{
			var m = M(p);
			if (m.anklet) return;
			m.anklet = true;
			p.moveSpeed += 0.10f;
		}

		public static void Threadstep(Player p, bool hide)
		{
			Riftstride(p, hide);
			LavaWaders(p);
			Anklet(p);
			Hermes(p, 7.5f);
			var m = M(p);
			m.threadstep = true;
			m.threadVisual |= !hide;
		}

		/// <summary>Terraspark Boots: Lightning's +8% and Frostspark's skates on top of the
		/// Lava Waders, with Terraspark's rocket.</summary>
		public static void Terraspark(Player p)
		{
			var m = M(p);
			if (m.terraspark) return;
			m.terraspark = true;
			LavaWaders(p);
			p.iceSkate = true;
			p.moveSpeed += 0.08f;
			Rocket(p, 4);
			Hermes(p, 6.75f);
		}

		public static void Worldwalker(Player p, bool hide)
		{
			Threadstep(p, hide);
			Terraspark(p);
			Hermes(p, 8.5f);
		}

		// ---------- aegis ----------

		private static void Dash(Player p, int tier)
		{
			var m = M(p);
			if (m.dashTier < tier)
				m.dashTier = tier;
		}

		public static void Gull(Player p) => Dash(p, 1);

		/// <summary>Obsidian Shield: knockback immunity, fire blocks.</summary>
		public static void Obsidian(Player p)
		{
			var m = M(p);
			if (m.obsidian) return;
			m.obsidian = true;
			p.noKnockback = true;
			p.fireWalk = true;
		}

		public static void Squall(Player p)
		{
			Gull(p);
			Obsidian(p);
			Dash(p, 2);
			// the Shield of Cthulhu's own dash; a later node in the line replaces it
			if (M(p).dashTier == 2)
				p.dashType = 2;
		}

		/// <summary>Frozen Turtle Shell: the Ice Barrier below half health.</summary>
		public static void Turtle(Player p)
		{
			var m = M(p);
			if (m.turtle) return;
			m.turtle = true;
			if (p.statLife <= p.statLifeMax2 * 0.5f)
				p.AddBuff(BuffID.IceBarrier, 5);
		}

		public static void Rift(Player p)
		{
			Squall(p);
			Turtle(p);
			Dash(p, 3);
		}

		/// <summary>Paladin's Shield: knockback immunity, and above a quarter of your life
		/// you take part of your teammates' damage.</summary>
		public static void Paladin(Player p)
		{
			var m = M(p);
			if (m.paladin) return;
			m.paladin = true;
			p.noKnockback = true;
			if (p.statLife > p.statLifeMax2 * 0.25f)
			{
				p.hasPaladinShield = true;
				if (p.whoAmI == Main.myPlayer && p.team != 0)
					foreach (Player o in Main.ActivePlayers)
						if (o.whoAmI != p.whoAmI && o.team == p.team && !o.dead
							&& o.Distance(p.Center) < 800f)
							o.AddBuff(BuffID.PaladinsShield, 20, quiet: false);
			}
		}

		/// <summary>Master Ninja Gear minus its Tabi dash, which the aegis dash replaces:
		/// the Black Belt's dodge and the Tiger Climbing Gear's wall climb.</summary>
		public static void Ninja(Player p)
		{
			var m = M(p);
			if (m.ninja) return;
			m.ninja = true;
			p.blackBelt = true;
			if (p.spikedBoots < 2)
				p.spikedBoots = 2;
		}

		public static void Weaver(Player p)
		{
			Rift(p);
			Paladin(p);
			Ninja(p);
			Dash(p, 4);
		}

		public static void Regard(Player p)
		{
			Weaver(p);
			Dash(p, 5);
		}
	}

	/// <summary>The Regard Aegis' cursor dash key. Unbound, the aegis falls back to the double tap.</summary>
	public class RegardDashKeybind : ModSystem
	{
		public static ModKeybind Dash { get; private set; }

		public static bool UsesKey => Dash != null && Dash.GetAssignedKeys().Count > 0;

		public override void Load() => Dash = KeybindLoader.RegisterKeybind(Mod, "RegardDash", "Mouse4");

		public override void Unload() => Dash = null;
	}
}
