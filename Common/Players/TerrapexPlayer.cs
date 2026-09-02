using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.GlobalProjectiles;
using Terrapex.Content.Buffs;
using Terrapex.Content.Items.Accessories;
using Terrapex.Common.GlobalNPCs;
using Terrapex.Content.Tiles;
using Terrapex.Content.Projectiles;

namespace Terrapex.Common.Players
{
	public class TerrapexPlayer : ModPlayer
	{
		/// <summary>Fissurite set bonus: every eighth hit cracks the target.</summary>
		public bool fissuriteSet;

		/// <summary>Fissure Sight potion: fissurite ore sparkles through stone.</summary>
		public bool fissureSight;

		/// <summary>Plate Shield: eats one hit, then needs twenty seconds.</summary>
		public bool plateShield;

		/// <summary>Glassblower set: magic crits throw splinters.</summary>
		public bool glassblowerSet;

		/// <summary>Mirror Charm: a hostile shot can be handed straight back.</summary>
		public bool mirrorCharm;

		/// <summary>Dust Cloak: double-tap left or right to dash.</summary>
		public bool dustCloak;

		/// <summary>Dustseeker set: ore and creatures both show through stone.</summary>
		public bool dustseekerSet;

		/// <summary>How many Guard Plates should be orbiting right now. Highest source wins.</summary>
		public int guardPlates;

		/// <summary>Companion Eye: keeps the nearest enemy marked.</summary>
		public bool companionEye;

		/// <summary>Shard Accelerator: your projectiles wind up the way the Keeper's do.</summary>
		public bool shardAccelerator;

		/// <summary>Darner set: the tier's whole payoff, damage against a stitched target.</summary>
		public bool darnerSet;

		/// <summary>Shard Resonator: every fifth shot is doubled.</summary>
		public bool shardResonator;
		public int resonatorCount;

		/// <summary>Cloth Belt: movement debuffs slide off.</summary>
		public bool clothBelt;

		/// <summary>
		/// What the Seam last bit into, and how long it stays remembered. A binding weapon
		/// needs two targets, and asking the player to hit both inside one swing would make
		/// the tier's mechanic unusable on anything that moves.
		/// </summary>
		public int seamTarget = -1;
		public int seamMemory;

		public const int SeamMemoryTime = 60 * 3;

		/// <summary>
		/// Rift Scythe reap stacks. The scythe is the tier's crowd weapon, so it is paid in
		/// bodies: every enemy its arc passes through makes the next swing bigger and harder,
		/// and the whole stack lapses if you stop swinging into a crowd.
		/// </summary>
		public int reaped;
		private int reapedTimer;

		public const int MaxReaped = 8;
		public const float ReapPerStack = 0.06f;
		public const int ReapMemory = 60 * 4;

		/// <summary>Darner heads. Only one is ever set, and each rewrites the thread differently.</summary>
		public bool darnerMelee, darnerRanged, darnerMagic, darnerSummon;

		/// <summary>Weaver set: the threads you leave behind outlast the ones you cut.</summary>
		public bool weaverSet;
		public bool weaverMelee, weaverRanged, weaverMagic, weaverSummon;

		/// <summary>Set by the Weaver Treads alone, so the legs carry an identity of their own.</summary>
		public bool weaverTreads;

		// ------------------------------------------------------------------------ the Loom
		//
		// The Weaver set's mechanic, and deliberately the opposite of the Darner set's above.
		// The Stitch joins two *enemies* and waits for you to hit one of them; the Loom plants
		// points in *space* and strings a thread of your own between them. One is about targets,
		// the other about the arena — which is the difference between the fourth tier and the
		// fifth, and the reason wearing both should not feel like wearing the same thing twice.

		/// <summary>Anchor points the loom has planted, in world space.</summary>
		public readonly List<Vector2> loom = new();
		private int loomHits;
		private int loomCooldown;
		private int loomDamage;

		/// <summary>Ticks before the loom will take another anchor after it fires.</summary>
		public const int LoomCooldown = 40;
		/// <summary>What a woven thread carries, off the biggest hit that built it.</summary>
		public const float LoomShare = 0.55f;

		/// <summary>Two points make a line; the magic head's third makes a triangle.</summary>
		public int LoomCapacity => weaverMagic ? 3 : 2;

		/// <summary>Hits per anchor. The melee head runs the loom faster.</summary>
		private int LoomEvery => weaverMelee ? 2 : 3;

		/// <summary>
		/// One hit, offered to the loom. Every anchor is a place you chose to stand and swing,
		/// so the web that comes out of it is one you laid rather than one you were handed.
		/// </summary>
		public void Weave(Vector2 at, int damage, bool minion)
		{
			if (!weaverSet || Player.whoAmI != Main.myPlayer || damage <= 0 || loomCooldown > 0)
				return;

			// minions only drive the loom for the summoner head — that head's whole point is
			// that the weaving carries on while you are busy doing something else
			if (minion && !weaverSummon)
				return;

			loomDamage = Math.Max(loomDamage, damage);
			if (++loomHits < LoomEvery)
				return;
			loomHits = 0;

			loom.Add(at);
			SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.35f, Pitch = 0.7f }, at);
			for (int i = 0; i < 14; i++)
			{
				Dust d = Dust.NewDustPerfect(at, DustID.Vortex,
					Main.rand.NextVector2CircularEdge(3.5f, 3.5f), 110, default, 1.1f);
				d.noGravity = true;
			}

			if (loom.Count < LoomCapacity)
				return;

			// the melee head cuts deeper, the ranged head's web stands twice as long
			int dmg = (int)(loomDamage * LoomShare * (weaverMelee ? 1.5f : 1f));
			int life = weaverRanged ? 300 : 150;
			int sides = loom.Count == 2 ? 1 : loom.Count;
			for (int i = 0; i < sides; i++)
				FriendlyThread.Between(Player.GetSource_Misc("Loom"), loom[i],
					loom[(i + 1) % loom.Count], dmg, Player.whoAmI, life);

			loom.Clear();
			loomDamage = 0;
			loomCooldown = LoomCooldown;
		}

		// ---------------------------------------------------------------------- the Regard
		//
		// The First Keeper set's mechanic, and the boss's own turned around: the eye that spent
		// the whole fight deciding whether it could see you is now yours, and what it looks at
		// is what you hurt.
		//
		// It is deliberately a third kind of thing next to the two tiers below it. The Stitch
		// (T4) joins two enemies and waits; the Loom (T5) plants points in the arena and strings
		// them; the Regard is about **facing** - a cone out of you, following your aim, that
		// pays for pointing yourself at the danger instead of kiting it with your back turned.
		// One is about targets, one about ground, and this one about where you are looking.

		/// <summary>First Keeper set, and its four heads. Only ever one head at a time.</summary>
		public bool firstSet;
		public bool firstMelee, firstRanged, firstMagic, firstSummon;

		/// <summary>Set by the greaves alone, so the legs carry a line of their own.</summary>
		public bool firstGreaves;

		/// <summary>What the regard adds, and what the melee head's narrower cone adds instead.</summary>
		public const float RegardBonus = 0.18f;
		public const float RegardFocused = 0.36f;

		/// <summary>How far it reaches, and how wide the magic head's cursor disc is.</summary>
		public const float RegardReach = 620f;
		public const float RegardDisc = 260f;

		/// <summary>Where the cone is pointing, and where the cursor is. Owner's client only.</summary>
		private float regardAim;
		private Vector2 regardCursor;

		/// <summary>The melee head trades width for weight: a narrow stare that hits twice as hard.</summary>
		public float RegardHalf => firstMelee ? 0.30f : 0.62f;

		/// <summary>The ranged head sees twice as far; the greaves add a quarter on top.</summary>
		public float RegardRange
			=> RegardReach * (firstRanged ? 2f : 1f) * (firstGreaves ? 1.25f : 1f);

		public float RegardShare => firstMelee ? RegardFocused : RegardBonus;

		public float RegardAim => regardAim;
		public Vector2 RegardCursor => regardCursor;

		/// <summary>
		/// Is this the thing you are looking at? The magic head reads a disc around the cursor
		/// instead of a cone out of the player, which is the one head that can regard something
		/// it is not facing - the price is that the disc does not reach across the screen.
		/// </summary>
		public bool Regarded(NPC npc)
		{
			if (!firstSet || npc == null || !npc.active || npc.friendly || npc.dontTakeDamage)
				return false;

			if (firstMagic)
				return Vector2.DistanceSquared(npc.Center, regardCursor) <= RegardDisc * RegardDisc;

			Vector2 to = npc.Center - Player.MountedCenter;
			float range = RegardRange;
			if (to.LengthSquared() > range * range)
				return false;
			return Math.Abs(MathHelper.WrapAngle(to.ToRotation() - regardAim)) <= RegardHalf;
		}

		/// <summary>Nearest thing inside the regard, for the summoner head to point minions at.</summary>
		private void PointMinions()
		{
			if (!firstSummon || Player.whoAmI != Main.myPlayer)
				return;

			int best = -1;
			float dist = float.MaxValue;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!Regarded(n) || n.lifeMax <= 5)
					continue;
				float d = Vector2.DistanceSquared(n.Center, Player.MountedCenter);
				if (d < dist) { dist = d; best = i; }
			}
			if (best >= 0)
				Player.MinionAttackTargetNPC = best;
		}

		/// <summary>
		/// Ticks the counter the Nothing sword is priced off. It is not a stack the player
		/// builds by doing something - it is one they keep by not being hit, which is the only
		/// version of that idea that asks anything of them.
		/// </summary>
		public int untouched;

		public const int UntouchedFull = 60 * 12;

		/// <summary>0 at the moment you are hit, 1 after twelve clean seconds.</summary>
		public float UntouchedShare => MathHelper.Clamp(untouched / (float)UntouchedFull, 0f, 1f);

		/// <summary>Reality Anchor: nothing gets to move you or take your senses.</summary>
		public bool realityAnchor;

		/// <summary>Echo Spindle: a minion hit sometimes lands twice.</summary>
		public bool echoSpindle;

		/// <summary>Weave Charm: one killing blow a fight is simply refused.</summary>
		public bool weaveCharm;
		private int weaveCooldown;

		public const int WeaveCharmCooldown = 60 * 180;
		public const float WeaverThreadLife = 1.5f;

		public void Reap(int count = 1)
		{
			reaped = System.Math.Min(MaxReaped, reaped + count);
			reapedTimer = ReapMemory;
		}

		public const int PlateShieldCooldown = 60 * 60;
		private int shieldCooldown;

		public const int HitsPerCrack = 8;
		private int hitCounter;
		private int sightTimer;

		public override void ResetEffects()
		{
			fissuriteSet = false;
			fissureSight = false;
			plateShield = false;
			glassblowerSet = false;
			mirrorCharm = false;
			dustCloak = false;
			dustseekerSet = false;
			darnerSet = false;
			darnerMelee = false;
			darnerRanged = false;
			darnerMagic = false;
			darnerSummon = false;
			weaverSet = false;
			weaverMelee = false;
			weaverRanged = false;
			weaverMagic = false;
			weaverSummon = false;
			weaverTreads = false;
			firstSet = false;
			firstMelee = false;
			firstRanged = false;
			firstMagic = false;
			firstSummon = false;
			firstGreaves = false;
			realityAnchor = false;
			echoSpindle = false;
			weaveCharm = false;
			shardResonator = false;
			clothBelt = false;
			guardPlates = 0;
			companionEye = false;
			shardAccelerator = false;
		}

		/// <summary>Sources do not stack — the better shell wins, so wearing both is not a trap.</summary>
		public void GrantGuardPlates(int count)
		{
			if (count > guardPlates)
				guardPlates = count;
		}

		private void CountHit(NPC target)
		{
			if (!fissuriteSet)
				return;

			hitCounter++;
			if (hitCounter < HitsPerCrack)
				return;

			hitCounter = 0;
			target.AddBuff(ModContent.BuffType<Cracked>(), 180);

			for (int i = 0; i < 10; i++)
			{
				Dust d = Dust.NewDustPerfect(target.Center, DustID.PurpleTorch,
					Main.rand.NextVector2Circular(3.5f, 3.5f), 100, default, 1.15f);
				d.noGravity = true;
			}
		}

		public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (Regarded(target))
				modifiers.FinalDamage *= 1f + RegardShare;

			if (!darnerSet || !target.GetGlobalNPC<BoundGlobalNPC>().IsBound)
				return;

			modifiers.FinalDamage *= 1f + DarnerBonus;

			// the melee head's own twist: a stitched target is a guaranteed crit, which is
			// worth far more on one big swing than a flat percentage would be
			if (darnerMelee && item.DamageType.CountsAsClass(DamageClass.Melee))
				modifiers.SetCrit();
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (Regarded(target))
			{
				modifiers.FinalDamage *= 1f + RegardShare;
				// the summoner head's twist: the eye is what the minions work off, so they are
				// paid again for hitting the thing you are actually looking at
				if (firstSummon && (proj.minion || proj.sentry
						|| proj.DamageType.CountsAsClass(DamageClass.Summon)))
					modifiers.FinalDamage *= 1f + RegardBonus;
			}

			// Closure's tag: a minion striking a marked target always crits. Kept here rather
			// than on the whip because the whip is long gone by the time the minion lands
			if (target.HasBuff<Closing>() && (proj.minion || proj.sentry
					|| proj.DamageType.CountsAsClass(DamageClass.Summon)))
				modifiers.SetCrit();

			if (!darnerSet || !target.GetGlobalNPC<BoundGlobalNPC>().IsBound)
				return;

			modifiers.FinalDamage *= 1f + DarnerBonus;

			if (darnerMelee && proj.DamageType.CountsAsClass(DamageClass.Melee))
				modifiers.SetCrit();

			// the summoner head's twist: minions are the thing that keeps hitting, so they
			// are what gets paid for the thread
			if (darnerSummon && (proj.minion || proj.sentry
					|| proj.DamageType.CountsAsClass(DamageClass.Summon)))
				modifiers.FinalDamage *= 1f + DarnerMinionBonus;
		}

		/// <summary>The Darner set bonus, straight from the plan. Shared by all four heads.</summary>
		public const float DarnerBonus = 0.20f;

		/// <summary>What the summoner head adds on top, for minions only.</summary>
		public const float DarnerMinionBonus = 0.25f;

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
		{
			CountHit(target);
			Weave(target.Center, damageDone, false);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (proj.owner != Player.whoAmI)
				return;
			CountHit(target);
			Weave(proj.Center, damageDone, proj.minion || proj.sentry
				|| proj.DamageType.CountsAsClass(DamageClass.Summon));

			// Glassblower bonus: a magic crit shatters, and the shards go where the spell
			// already was. It rewards the crit chance the set is built around.
			if (glassblowerSet && hit.Crit && proj.DamageType == DamageClass.Magic && Player.whoAmI == Main.myPlayer)
			{
				for (int i = -1; i <= 1; i += 2)
				{
					Vector2 v = Main.rand.NextVector2CircularEdge(5f, 5f).RotatedBy(i * 0.3f);
					Projectile.NewProjectile(Player.GetSource_OnHit(target), target.Center, v,
						ProjectileID.CrystalShard, (int)(damageDone * 0.25f), 0f, Player.whoAmI);
				}
			}
		}

		public override void PostUpdateEquips()
		{
			if (shieldCooldown > 0)
				shieldCooldown--;
			if (dashCooldown > 0)
				dashCooldown--;

			ReflectNearbyShots();
			MaintainGuardPlates();
			MarkNearestEnemy();
			AimRegard();
			PointMinions();
		}

		/// <summary>
		/// The cone follows the cursor, read once a tick on the owning client. Other players see
		/// a stale angle, which is fine - the bonus is applied by whoever is swinging, and they
		/// are the one holding the mouse.
		/// </summary>
		private void AimRegard()
		{
			if (!firstSet || Player.whoAmI != Main.myPlayer)
				return;
			regardCursor = Main.MouseWorld;
			Vector2 to = regardCursor - Player.MountedCenter;
			if (to.LengthSquared() > 1f)
				regardAim = to.ToRotation();
		}

		// ---------------------------------------------------------------- Guard Plates

		/// <summary>Eight seconds, straight out of the plan's set-bonus line.</summary>
		public const int GuardPlateCooldown = 8 * 60;

		private int plateCooldown;

		/// <summary>Called when a plate eats a shot: the whole shell goes, and comes back together.</summary>
		public void BreakGuardPlates()
		{
			plateCooldown = GuardPlateCooldown;
			int type = ModContent.ProjectileType<Content.Projectiles.GuardPlate>();
			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				Projectile p = Main.projectile[i];
				if (p.active && p.owner == Player.whoAmI && p.type == type)
					p.Kill();
			}
		}

		private void MaintainGuardPlates()
		{
			if (Player.whoAmI != Main.myPlayer)
				return;
			if (plateCooldown > 0)
			{
				plateCooldown--;
				return;
			}
			if (guardPlates <= 0)
				return;

			int type = ModContent.ProjectileType<Content.Projectiles.GuardPlate>();
			int have = Player.ownedProjectileCounts[type];
			if (have >= guardPlates)
				return;

			// the shell is rebuilt whole, so the seats are handed out in one pass
			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				Projectile p = Main.projectile[i];
				if (p.active && p.owner == Player.whoAmI && p.type == type)
					p.Kill();
			}
			for (int slot = 0; slot < guardPlates; slot++)
			{
				Projectile.NewProjectile(Player.GetSource_Accessory(Player.HeldItem),
					Player.MountedCenter, Vector2.Zero, type, 0, 0f, Player.whoAmI, slot, guardPlates);
			}
		}

		// ---------------------------------------------------------------- Companion Eye

		private int markTimer;

		private void MarkNearestEnemy()
		{
			if (!companionEye || Player.whoAmI != Main.myPlayer)
				return;
			if (++markTimer < 30)
				return;
			markTimer = 0;

			NPC best = null;
			float bestDist = 700f;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.dontTakeDamage || n.lifeMax <= 5)
					continue;
				float d = Vector2.Distance(Player.Center, n.Center);
				if (d < bestDist) { bestDist = d; best = n; }
			}
			if (best == null)
				return;

			best.AddBuff(ModContent.BuffType<RiftMark>(), 90);
			Player.MinionAttackTargetNPC = best.whoAmI;
		}

		// ---------------------------------------------------------------- Mirror Charm

		/// <summary>A reflected shot is worth more than a blocked one, so it hits harder.</summary>
		private const float ReflectDamage = 1.5f;

		private void ReflectNearbyShots()
		{
			if (!mirrorCharm || Player.whoAmI != Main.myPlayer || Player.immune)
				return;

			Rectangle box = Player.Hitbox;
			box.Inflate(6, 6);

			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				Projectile proj = Main.projectile[i];
				if (!proj.active || !proj.hostile || proj.damage <= 0 || !proj.Hitbox.Intersects(box))
					continue;

				var mirror = proj.GetGlobalProjectile<MirrorGlobalProjectile>();
				if (mirror.mirrorRolled)
					continue;
				mirror.mirrorRolled = true;

				if (!Main.rand.NextBool(MirrorCharm.ReflectChance))
					continue;

				proj.hostile = false;
				proj.friendly = true;
				proj.owner = Player.whoAmI;
				proj.velocity = -proj.velocity;
				proj.damage = (int)(proj.damage * ReflectDamage);
				proj.netUpdate = true;

				Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Pitch = 0.5f }, Player.Center);
				for (int k = 0; k < 12; k++)
				{
					Dust d = Dust.NewDustPerfect(proj.Center, DustID.Glass,
						Main.rand.NextVector2Circular(4f, 4f), 100, default, 1.15f);
					d.noGravity = true;
				}
			}
		}

		// ---------------------------------------------------------------- Dust Cloak

		public const int DashDuration = 22;
		public const int DashCooldown = 55;
		private const float DashSpeed = 11.5f;

		/// <summary>How long the second tap has to arrive, in ticks.</summary>
		private const int TapWindow = 16;

		private int dashCooldown;
		private int dashTimer;
		private int dashDirection;
		private int tapDir;
		private int tapWindow;
		private bool heldLeft, heldRight;

		public override void PreUpdateMovement()
		{
			if (dashTimer > 0)
			{
				dashTimer--;
				// eased so the dash ends by drifting rather than by stopping dead
				float t = dashTimer / (float)DashDuration;
				Player.velocity.X = dashDirection * DashSpeed * (0.45f + 0.55f * t);
				Player.dashDelay = 10;

				for (int i = 0; i < 2; i++)
				{
					Dust d = Dust.NewDustDirect(Player.position, Player.width, Player.height,
						DustID.PurpleTorch, -Player.velocity.X * 0.15f, 0f, 130, default, 1.1f);
					d.noGravity = true;
				}
				return;
			}

			// The double tap is counted here rather than read out of Player.doubleTapCardinalTimer.
			// That was the first attempt and the dash never fired once: vanilla rewrites the timer
			// on the same tick as the second press, so by the time this hook runs the window it
			// describes is always already closed.
			bool left = Player.controlLeft, right = Player.controlRight;
			bool tappedLeft = left && !heldLeft;
			bool tappedRight = right && !heldRight;
			heldLeft = left;
			heldRight = right;

			if (tapWindow > 0)
				tapWindow--;
			else
				tapDir = 0;

			if (!dustCloak || Player.mount.Active || Player.dead)
				return;

			int dir = 0;
			if (tappedRight)
			{
				if (tapDir == 1 && tapWindow > 0)
					dir = 1;
				else { tapDir = 1; tapWindow = TapWindow; }
			}
			else if (tappedLeft)
			{
				if (tapDir == -1 && tapWindow > 0)
					dir = -1;
				else { tapDir = -1; tapWindow = TapWindow; }
			}

			if (dir == 0 || dashCooldown > 0)
				return;

			tapDir = 0;
			tapWindow = 0;
			dashDirection = dir;
			dashTimer = DashDuration;
			dashCooldown = DashCooldown;
			Player.direction = dir;
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.5f, Pitch = 0.3f }, Player.Center);
		}

		/// <summary>The shield takes the hit instead of the player, once per cooldown.</summary>
		public override bool ConsumableDodge(Player.HurtInfo info)
		{
			if (!plateShield || shieldCooldown > 0)
				return false;

			shieldCooldown = PlateShieldCooldown;
			Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit42, Player.Center);
			for (int i = 0; i < 18; i++)
			{
				Dust d = Dust.NewDustDirect(Player.position, Player.width, Player.height,
					DustID.Stone, Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 4f), 80, default, 1.3f);
				d.noGravity = true;
			}
			Player.SetImmuneTimeForAllTypes(Player.longInvince ? 60 : 40);
			return true;
		}

		public bool PlateShieldReady => plateShield && shieldCooldown <= 0;

		/// <summary>
		/// Hands the Seam its second target. Returns the remembered one and takes the new one
		/// in its place, or -1 when there is nothing to stitch to yet.
		/// </summary>
		public NPC TakeSeamTarget(NPC hit)
		{
			NPC held = null;
			if (seamMemory > 0 && seamTarget >= 0 && seamTarget < Main.maxNPCs)
			{
				NPC candidate = Main.npc[seamTarget];
				if (candidate.active && candidate.whoAmI != hit.whoAmI)
					held = candidate;
			}

			seamTarget = hit.whoAmI;
			seamMemory = SeamMemoryTime;
			return held;
		}

		/// <summary>True while the charm still has its one refusal in hand.</summary>
		public bool WeaveCharmReady => weaveCharm && weaveCooldown <= 0;

		public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound,
			ref bool genDust, ref Terraria.DataStructures.PlayerDeathReason damageSource)
		{
			if (!WeaveCharmReady)
				return true;

			// the charm spends itself and then sits out three minutes, so it is one save per
			// fight rather than a permanent extra life
			weaveCooldown = WeaveCharmCooldown;
			Player.statLife = Player.statLifeMax2 / 4;
			Player.HealEffect(Player.statLife, true);
			Player.immune = true;
			Player.immuneTime = 120;
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29, Player.Center);
			for (int i = 0; i < 40; i++)
			{
				Dust d = Dust.NewDustPerfect(Player.Center, DustID.Vortex,
					Main.rand.NextVector2Circular(6f, 6f), 90, default, 1.4f);
				d.noGravity = true;
			}
			return false;
		}

		/// <summary>The Nothing sword's whole price, paid the instant anything touches you.</summary>
		public override void OnHurt(Player.HurtInfo info) => untouched = 0;

		public override void PostUpdate()
		{
			if (seamMemory > 0)
				seamMemory--;

			if (reapedTimer > 0 && --reapedTimer == 0)
				reaped = 0;
			if (weaveCooldown > 0)
				weaveCooldown--;
			if (loomCooldown > 0)
				loomCooldown--;
			if (untouched < UntouchedFull)
				untouched++;

			// a planted anchor has to be visible or the set is bookkeeping; and taking the set
			// off drops the frame rather than leaving points hanging in the world
			if (!weaverSet)
				loom.Clear();
			foreach (Vector2 at in loom)
			{
				Lighting.AddLight(at, 0.25f, 0.6f, 0.55f);
				if (Main.rand.NextBool(4))
				{
					Dust d = Dust.NewDustPerfect(at + Main.rand.NextVector2Circular(10f, 10f),
						DustID.Vortex, Vector2.Zero, 120, default, 0.9f);
					d.noGravity = true;
					d.velocity = (at - d.position) * 0.08f;
				}
			}

			if ((!fissureSight && !dustseekerSet) || Player.whoAmI != Main.myPlayer)
				return;

			// A cheap spelunker: sweep a window of tiles every third of a second and
			// mark the fissurite in it. Scanning the whole screen every tick is what
			// makes homebrew ore-sight potions tank the framerate.
			if (++sightTimer < 20)
				return;
			sightTimer = 0;

			int ore = ModContent.TileType<FissuriteOreTile>();
			Point origin = Player.Center.ToTileCoordinates();
			const int radius = 34;
			int marked = 0;

			for (int x = origin.X - radius; x <= origin.X + radius && marked < 40; x++)
			{
				for (int y = origin.Y - radius; y <= origin.Y + radius && marked < 40; y++)
				{
					if (!WorldGen.InWorld(x, y, 2))
						continue;
					Tile tile = Main.tile[x, y];
					if (!tile.HasTile || tile.TileType != ore)
						continue;

					marked++;
					Dust d = Dust.NewDustPerfect(new Vector2(x * 16 + 8, y * 16 + 8),
						DustID.PurpleTorch, Vector2.Zero, 90, default, 1.1f);
					d.noGravity = true;
					d.velocity = Vector2.Zero;
					d.fadeIn = 1.2f;
				}
			}
		}
	}
}
