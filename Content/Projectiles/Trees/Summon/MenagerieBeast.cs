using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Particles;
using Terrapex.Content.Buffs.Trees;

namespace Terrapex.Content.Projectiles.Trees.Summon
{
	/// <summary>
	/// The Menagerie's beast - one minion type, four animals. Which animal a summon is lives in
	/// <c>ai[2]</c>, set by the item when it spawns the projectile and synced with it, so every
	/// client draws and runs the same form. The four fight in four different ways, because they
	/// are the four summons that went into the Horn:
	///
	/// <list type="bullet">
	/// <item><b>crab</b> (Rusted Crab Pot) walks the floor and, on a hit, grips: it rides the
	/// target for <see cref="GripTime"/> ticks, pinching, and drags a non-boss to a crawl;</item>
	/// <item><b>beetling</b> (Beetling Staff) lines up off to the side, holds, then dives through;</item>
	/// <item><b>rime sprite</b> (Glacier Bell) rams, chills what it rams and bounces off;</item>
	/// <item><b>hatchling</b> (Hatchling Rod) perches over the player's shoulder and spits. It has
	/// no contact damage at all: <see cref="CanDamage"/> is false for it and the spit is a separate
	/// projectile, which is the CLAUDE.md minion rule - <see cref="MinionContactDamage"/> stays true
	/// for the type, because the other three need their hitbox.</item>
	/// </list>
	///
	/// Each node is a subclass that only says its tier, its buff and its slot cost; the tier then
	/// switches on what the node added. Court and up: all four forms on the field is "the court
	/// in session" and the buff gives +15% summon damage. Conclave and up: every 6 s the beasts
	/// trade places and each strikes its own target in the same instant. The Ark: one summon is
	/// all four at a quarter slot each, and every 3 s each of them fires a volley.
	/// </summary>
	public abstract class MenagerieBeast : ModProjectile
	{
		public const int Crab = 0, Beetle = 1, Sprite = 2, Hatchling = 3;
		public const int SwapEvery = 360;
		public const int VolleyEvery = 180;
		public const int GripTime = 60;
		public const float CourtBonus = 0.15f;
		public const float StrikeMult = 1.5f;
		public const float VolleyMult = 0.6f;

		private const float Range = 760f;
		private const float Leash = 1500f;

		/// <summary>0 Horn, 1 Court, 2 Conclave, 3 Ark.</summary>
		public abstract int Tier { get; }
		protected abstract int BuffType { get; }

		public override string Texture => "Terrapex/Content/Projectiles/Trees/Summon/MenagerieBeast";

		public int Form => (int)Projectile.ai[2] & 3;
		private ref float State => ref Projectile.ai[0];
		private ref float Timer => ref Projectile.ai[1];

		// synced through SendExtraAI: the NPC the crab is riding, and the cooldown before the next
		// grip / dart / spit
		private int grip = -1;
		private int cooldown;

		// client-side only
		private bool spawned;
		private bool wasAssembled;
		private int spitFlash;

		public static readonly Color[] FormColor =
		{
			new(242, 96, 63), new(245, 192, 74), new(163, 233, 248), new(217, 78, 203),
		};

		public override void SetStaticDefaults()
		{
			Main.projFrames[Type] = 16;
			Main.projPet[Type] = true;
			ProjectileID.Sets.MinionSacrificable[Type] = true;
			ProjectileID.Sets.MinionTargettingFeature[Type] = true;
			ProjectileID.Sets.CultistIsResistantTo[Type] = true;
			ProjectileID.Sets.TrailCacheLength[Type] = 8;
			ProjectileID.Sets.TrailingMode[Type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.width = 22;
			Projectile.height = 22;
			Projectile.friendly = true;
			Projectile.minion = true;
			Projectile.minionSlots = Tier == 3 ? 0.25f : 1f;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 18000;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.netImportant = true;
			Projectile.aiStyle = -1;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
		}

		// true for the type: three of the four forms deal damage with their own body. The
		// hatchling is switched off in CanDamage instead and fights through MenagerieSpit.
		public override bool MinionContactDamage() => true;

		public override bool? CanDamage()
		{
			switch (Form)
			{
				case Crab: return null;
				case Beetle: return State == 2f ? null : false;
				case Sprite: return State == 1f ? null : false;
				default: return false;
			}
		}

		public override void SendExtraAI(BinaryWriter writer)
		{
			writer.Write((short)grip);
			writer.Write((short)cooldown);
		}

		public override void ReceiveExtraAI(BinaryReader reader)
		{
			grip = reader.ReadInt16();
			cooldown = reader.ReadInt16();
		}

		// ---------------------------------------------------------------------------------------
		// helpers the items and buffs share

		/// <summary>The form the next summon of this type should be: whichever is fewest on the field.</summary>
		public static int NextForm(Player player, int type)
		{
			Span<int> count = stackalloc int[4];
			foreach (Projectile p in Main.ActiveProjectiles)
				if (p.owner == player.whoAmI && p.type == type)
					count[(int)p.ai[2] & 3]++;
			int best = 0;
			for (int f = 1; f < 4; f++)
				if (count[f] < count[best])
					best = f;
			return best;
		}

		/// <summary>All four forms of this type on the field for this player: the court is in session.</summary>
		public static bool Assembled(Player player, int type)
		{
			int mask = 0;
			foreach (Projectile p in Main.ActiveProjectiles)
				if (p.owner == player.whoAmI && p.type == type)
					mask |= 1 << ((int)p.ai[2] & 3);
			return mask == 15;
		}

		private List<Projectile> Pack()
		{
			var list = new List<Projectile>();
			foreach (Projectile p in Main.ActiveProjectiles)
				if (p.owner == Projectile.owner && p.type == Type)
					list.Add(p);
			list.Sort((a, b) => a.whoAmI.CompareTo(b.whoAmI));
			return list;
		}

		private bool IsLeader()
		{
			foreach (Projectile p in Main.ActiveProjectiles)
				if (p.owner == Projectile.owner && p.type == Type && p.whoAmI < Projectile.whoAmI)
					return false;
			return true;
		}

		private int SlotIndex()
		{
			int i = 0;
			foreach (Projectile p in Main.ActiveProjectiles)
				if (p.owner == Projectile.owner && p.type == Type && p.whoAmI < Projectile.whoAmI)
					i++;
			return i;
		}

		private NPC FindTarget(Player owner)
		{
			if (owner.HasMinionAttackTargetNPC)
			{
				NPC forced = Main.npc[owner.MinionAttackTargetNPC];
				if (forced.CanBeChasedBy(Projectile) && owner.Distance(forced.Center) < Range * 1.5f)
					return forced;
			}
			NPC best = null;
			float bestDist = Range;
			foreach (NPC n in Main.ActiveNPCs)
			{
				if (!n.CanBeChasedBy(Projectile) || owner.Distance(n.Center) > Range * 1.2f)
					continue;
				float d = Projectile.Distance(n.Center);
				if (d < bestDist)
				{
					bestDist = d;
					best = n;
				}
			}
			return best;
		}

		public static void Poof(Vector2 at, Color color, float size = 1f)
		{
			ParticleSystem.Spawn(new Bloom(at, new Color(color.R, color.G, color.B, 0), 0.4f * size, 1.1f * size, 16));
			ParticleSystem.Spawn(new Shockwave(at, new Color(color.R, color.G, color.B, 0) * 0.8f, 0.1f * size, 0.8f * size, 20));
			for (int i = 0; i < 7 * size; i++)
				ParticleSystem.Spawn(new Smoke(at + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextVector2Circular(1.6f, 1.6f) + new Vector2(0f, -0.6f),
					new Color(214, 208, 226), Main.rand.NextFloat(0.22f, 0.36f) * size, Main.rand.Next(26, 40)));
			FX.Sparks(at, new Color(color.R, color.G, color.B, 0), (int)(8 * size), 4f * size, 0.8f, 20);
		}

		// ---------------------------------------------------------------------------------------

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (owner.dead || !owner.active)
			{
				owner.ClearBuff(BuffType);
				Projectile.Kill();
				return;
			}
			if (owner.HasBuff(BuffType))
				Projectile.timeLeft = 2;

			if (!spawned)
			{
				spawned = true;
				Poof(Projectile.Center, FormColor[Form]);
				SoundEngine.PlaySound(SoundID.Item44 with { Volume = 0.5f, Pitch = 0.15f * Form }, Projectile.Center);
			}

			if (Projectile.Distance(owner.Center) > Leash)
			{
				Poof(Projectile.Center, FormColor[Form], 0.6f);
				Projectile.Center = owner.Center + new Vector2(0f, -30f);
				Projectile.velocity = Vector2.Zero;
				State = 0f;
				grip = -1;
				Poof(Projectile.Center, FormColor[Form], 0.6f);
			}

			if (cooldown > 0)
				cooldown--;
			if (spitFlash > 0)
				spitFlash--;

			// the court in session: a ring of poofs the moment the fourth beast arrives
			if (Tier >= 1)
			{
				bool assembled = Assembled(owner, Type);
				if (assembled && !wasAssembled && IsLeader())
				{
					foreach (Projectile p in Pack())
						Poof(p.Center, Color.Gold, 0.7f);
					SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.6f, Pitch = 0.3f }, owner.Center);
				}
				wasAssembled = assembled;
			}

			NPC target = FindTarget(owner);

			if (Projectile.owner == Main.myPlayer)
			{
				uint clock = Main.GameUpdateCount;
				if (Tier >= 2 && clock % SwapEvery == 0 && IsLeader())
					Conclave(owner);
				if (Tier >= 3 && clock % VolleyEvery == VolleyEvery / 2 && target != null)
					Volley(target);
			}

			switch (Form)
			{
				case Crab: CrabAI(owner, target); break;
				case Beetle: BeetleAI(owner, target); break;
				case Sprite: SpriteAI(owner, target); break;
				default: HatchlingAI(owner, target); break;
			}

			Color glow = FormColor[Form];
			Lighting.AddLight(Projectile.Center, glow.ToVector3() * (Tier == 0 ? 0.15f : 0.3f));
		}

		// --- crab -------------------------------------------------------------------------------
		private void CrabAI(Player owner, NPC target)
		{
			if (grip >= 0)
			{
				NPC n = Main.npc[grip];
				Timer++;
				if (!n.active || n.life <= 0 || n.friendly || Timer > GripTime)
				{
					grip = -1;
					cooldown = 40;
					Projectile.velocity = new Vector2(-Projectile.spriteDirection * 3f, -5f);
					Projectile.netUpdate = true;
				}
				else
				{
					Projectile.tileCollide = false;
					Projectile.Center = n.Top + new Vector2(-Projectile.spriteDirection * 4f, 6f);
					Projectile.velocity = n.velocity;
					// the grip: a non-boss is dragged to a crawl. Server-side only, it owns NPC motion
					if (Main.netMode != NetmodeID.MultiplayerClient && !n.boss && n.knockBackResist > 0f)
						n.velocity *= 0.86f;
					if (Main.rand.NextBool(6))
						FX.Sparks(Projectile.Center, new Color(255, 170, 120, 0), 2, 2.5f, 0.6f, 12);
					Projectile.frame = Crab * 4 + 3;
					return;
				}
			}

			Projectile.tileCollide = true;
			Vector2 goal = target?.Center ?? owner.Center + new Vector2(-owner.direction * (40f + SlotIndex() * 22f), 0f);
			float dx = goal.X - Projectile.Center.X;
			float speed = 5f + Tier * 0.6f;
			if (Math.Abs(dx) < (target != null ? 10f : 30f))
				Projectile.velocity.X *= 0.8f;
			else
				Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, Math.Sign(dx) * speed, 0.15f);
			Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.35f, 12f);

			bool grounded = Projectile.velocity.Y == 0f;
			if (grounded && Timer <= 0f)
			{
				bool blocked = Collision.SolidCollision(
					Projectile.position + new Vector2(Math.Sign(dx) * 10f, 0f), Projectile.width, Projectile.height - 8);
				bool above = target != null && target.Center.Y < Projectile.Center.Y - 30f;
				bool lunge = target != null && Math.Abs(dx) < 80f;
				bool behind = target == null && owner.Center.Y < Projectile.Center.Y - 120f;
				if (blocked || above || lunge || behind)
				{
					Projectile.velocity.Y = lunge && !blocked ? -5.5f : -8f;
					Timer = 16f;
				}
			}
			if (Timer > 0f)
				Timer--;

			// a crab left far below the player (it cannot climb a shaft) is fetched back up
			if (target == null && Projectile.Distance(owner.Center) > 700f)
			{
				Poof(Projectile.Center, FormColor[Crab], 0.5f);
				Projectile.Center = owner.Center;
				Projectile.velocity = Vector2.Zero;
				Poof(Projectile.Center, FormColor[Crab], 0.5f);
			}

			if (Math.Abs(Projectile.velocity.X) > 0.2f)
				Projectile.spriteDirection = Projectile.direction = Projectile.velocity.X > 0f ? 1 : -1;
			Projectile.rotation = 0f;

			int f;
			if (!grounded)
				f = 3;
			else if (Math.Abs(Projectile.velocity.X) > 0.4f)
			{
				if (++Projectile.frameCounter >= 5)
				{
					Projectile.frameCounter = 0;
					Projectile.localAI[0] = (Projectile.localAI[0] + 1) % 3;
				}
				f = (int)Projectile.localAI[0];
			}
			else
				f = 0;
			Projectile.frame = Crab * 4 + f;
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			if (Form == Crab && oldVelocity.Y > 0f)
				Projectile.velocity.Y = 0f;
			if (Form == Crab && Projectile.velocity.X != oldVelocity.X)
				Projectile.velocity.X = 0f;
			return false;
		}

		// --- beetling ---------------------------------------------------------------------------
		private const int Approach = 0, Lining = 1, Diving = 2;

		private void BeetleAI(Player owner, NPC target)
		{
			Projectile.tileCollide = false;
			Timer++;
			int dash = 16;
			float dashSpeed = 15f + Tier * 1.5f;

			if (State == Diving)
			{
				if (Timer >= dash)
				{
					State = Approach;
					Timer = 0f;
				}
				// the dive streak
				if (Main.rand.NextBool(2))
					ParticleSystem.Spawn(new Spark(Projectile.Center, -Projectile.velocity * 0.15f + Main.rand.NextVector2Circular(0.6f, 0.6f),
						new Color(255, 214, 120, 0), Main.rand.NextFloat(0.7f, 1f), 14));
			}
			else if (target == null)
			{
				State = Approach;
				Idle(owner, -50f);
			}
			else if (State == Approach)
			{
				int side = Projectile.Center.X < target.Center.X ? -1 : 1;
				Vector2 spot = target.Center + new Vector2(side * 130f, -50f);
				MoveTo(spot, 11f + Tier, 0.12f);
				if (Projectile.Distance(spot) < 40f)
				{
					State = Lining;
					Timer = 0f;
				}
			}
			else
			{
				Projectile.velocity *= 0.8f;
				if (Timer >= 10)
					Dive(target, dashSpeed);
			}

			Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;
			float a = Projectile.velocity.ToRotation();
			Projectile.rotation = State == Diving
				? (Projectile.spriteDirection > 0 ? a : a - MathHelper.Pi)
				: Projectile.velocity.X * 0.04f;

			if (++Projectile.frameCounter >= 3)
			{
				Projectile.frameCounter = 0;
				Projectile.localAI[0] = 1 - Projectile.localAI[0];
			}
			Projectile.frame = Beetle * 4 + (State == Diving ? 2 : 0) + (int)Projectile.localAI[0];
		}

		private void Dive(NPC target, float speed)
		{
			State = Diving;
			Timer = 0f;
			Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * speed;
			Projectile.netUpdate = true;
			FX.Sparks(Projectile.Center, new Color(255, 214, 120, 0), 6, 3f, 0.8f, 14, 0f, 1.2f, (-Projectile.velocity).ToRotation());
			SoundEngine.PlaySound(SoundID.Item32 with { Volume = 0.4f, Pitch = 0.4f }, Projectile.Center);
		}

		// --- rime sprite ------------------------------------------------------------------------
		private void SpriteAI(Player owner, NPC target)
		{
			Projectile.tileCollide = false;
			if (State == 1f)
			{
				Timer++;
				if (target != null)
					Projectile.velocity = Vector2.Lerp(Projectile.velocity,
						(target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * (12f + Tier * 1.2f), 0.15f);
				if (Timer > 34 || target == null)
				{
					State = 0f;
					Timer = 0f;
				}
				if (Main.rand.NextBool(3))
					ParticleSystem.Spawn(new Spark(Projectile.Center, -Projectile.velocity * 0.1f, new Color(180, 240, 255, 0), 0.7f, 12));
				Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;
			}
			else
			{
				Idle(owner, -60f);
				Projectile.spriteDirection = owner.direction;
				if (target != null && cooldown <= 0)
					StartDart();
			}
			Projectile.rotation = Projectile.velocity.X * 0.03f;

			if (++Projectile.frameCounter >= 7)
			{
				Projectile.frameCounter = 0;
				Projectile.localAI[0] = (Projectile.localAI[0] + 1) % 4;
			}
			Projectile.frame = Sprite * 4 + (int)Projectile.localAI[0];
			if (Main.rand.NextBool(10))
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), DustID.Snow,
					new Vector2(0f, 0.5f), 140, default, 0.8f).noGravity = true;
		}

		private void StartDart()
		{
			State = 1f;
			Timer = 0f;
			Projectile.netUpdate = true;
			SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.35f, Pitch = 0.5f }, Projectile.Center);
		}

		// --- hatchling --------------------------------------------------------------------------
		private void HatchlingAI(Player owner, NPC target)
		{
			Projectile.tileCollide = false;
			// a perch over the player's shoulder; several hatchlings stack outward
			int slot = SlotIndex();
			Vector2 perch = owner.Center + new Vector2(-owner.direction * (26f + slot * 14f),
				-46f - slot * 6f + MathF.Sin(Main.GlobalTimeWrappedHourly * 2.5f + slot) * 4f);
			MoveTo(perch, 14f, 0.2f);
			Projectile.spriteDirection = target != null ? (target.Center.X > Projectile.Center.X ? 1 : -1) : owner.direction;
			Projectile.rotation = 0f;

			int every = 40 - Tier * 4;
			if (target != null && cooldown <= 0
				&& Collision.CanHitLine(Projectile.Center, 1, 1, target.position, target.width, target.height))
			{
				cooldown = every;
				spitFlash = 10;
				if (Projectile.owner == Main.myPlayer)
					Spit(target, 1, 0f);
				SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.5f, Pitch = 0.3f }, Projectile.Center);
			}

			if (++Projectile.frameCounter >= 12)
			{
				Projectile.frameCounter = 0;
				Projectile.localAI[0] = 1 - Projectile.localAI[0];
			}
			Projectile.frame = Hatchling * 4 + (spitFlash > 0 ? 2 + (spitFlash > 5 ? 0 : 1) : (int)Projectile.localAI[0]);
		}

		private Vector2 Mouth => Projectile.Center + new Vector2(Projectile.spriteDirection * 8f, 2f);

		/// <summary>Fires <paramref name="count"/> spits in a fan at the target. Owner only.</summary>
		private void Spit(NPC target, int count, float fan, float mult = 1f, int kind = 0)
		{
			Vector2 from = Form == Hatchling ? Mouth : Projectile.Center;
			// lead the target a little, like a thrown thing would
			Vector2 aim = target.Center + target.velocity * 10f - from;
			float speed = kind == 1 ? 14f : 11f;
			for (int i = 0; i < count; i++)
			{
				float off = count == 1 ? 0f : MathHelper.Lerp(-fan, fan, i / (float)(count - 1));
				Vector2 v = aim.SafeNormalize(Vector2.UnitX).RotatedBy(off) * speed;
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), from, v, ModContent.ProjectileType<MenagerieSpit>(),
					Math.Max(1, (int)(Projectile.damage * mult)), Projectile.knockBack * 0.5f, Projectile.owner, kind, Form);
			}
		}

		// --- shared movement --------------------------------------------------------------------
		private void Idle(Player owner, float height)
		{
			int slot = SlotIndex();
			Vector2 spot = owner.Center + new Vector2(-owner.direction * (40f + slot * 22f),
				height + MathF.Sin(Main.GlobalTimeWrappedHourly * 3f + slot) * 7f);
			MoveTo(spot, 10f, 0.1f);
		}

		private void MoveTo(Vector2 spot, float speed, float pull)
		{
			Vector2 to = spot - Projectile.Center;
			float d = to.Length();
			Vector2 want = d > 1f ? to / d * Math.Min(speed, d * 0.15f + 1f) : Vector2.Zero;
			Projectile.velocity = Vector2.Lerp(Projectile.velocity, want, pull);
		}

		// --- the Conclave: trade places, then every beast strikes its own target at once ---------
		private void Conclave(Player owner)
		{
			List<Projectile> pack = Pack();
			if (pack.Count < 2)
				return;

			// distinct targets, nearest first; the pack shares them out in order
			var targets = new List<NPC>();
			foreach (NPC n in Main.ActiveNPCs)
				if (n.CanBeChasedBy(Projectile) && owner.Distance(n.Center) < Range * 1.2f)
					targets.Add(n);
			targets.Sort((a, b) => owner.DistanceSQ(a.Center).CompareTo(owner.DistanceSQ(b.Center)));

			var old = new Vector2[pack.Count];
			for (int i = 0; i < pack.Count; i++)
				old[i] = pack[i].Center;
			for (int i = 0; i < pack.Count; i++)
			{
				Projectile p = pack[i];
				var beast = (MenagerieBeast)p.ModProjectile;
				Poof(old[i], FormColor[beast.Form], 0.6f);
				p.Center = old[(i + 1) % pack.Count];
				p.velocity *= 0.2f;
				Poof(p.Center, FormColor[beast.Form], 0.6f);
				if (targets.Count > 0)
					beast.Strike(targets[i % targets.Count]);
				p.netUpdate = true;
			}
			SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.7f, Pitch = -0.2f }, owner.Center);
		}

		/// <summary>The Conclave's synchronised hit: a strike on the target, and the form's own move at it.</summary>
		private void Strike(NPC target)
		{
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
				ModContent.ProjectileType<ConclaveStrike>(), Math.Max(1, (int)(Projectile.damage * StrikeMult)),
				Projectile.knockBack, Projectile.owner, Form);
			switch (Form)
			{
				case Crab:
					grip = target.whoAmI;
					Timer = 0f;
					break;
				case Beetle:
					Dive(target, 16f + Tier * 1.5f);
					break;
				case Sprite:
					StartDart();
					Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 12f;
					break;
				default:
					Spit(target, 3, 0.18f);
					spitFlash = 10;
					break;
			}
		}

		// --- the Ark: every beast fires a pair of bolts in the same tick ------------------------
		private void Volley(NPC target)
		{
			Spit(target, 2, 0.12f, VolleyMult, 1);
			ParticleSystem.Spawn(new Bloom(Projectile.Center, new Color(255, 215, 154, 0), 0.3f, 0.9f, 12));
		}

		// --- hits ------------------------------------------------------------------------------
		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			// a crab that is riding something pinches; it does not also take knockback off it
			if (Form == Crab && grip >= 0)
				modifiers.Knockback *= 0f;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			switch (Form)
			{
				case Crab:
					if (grip < 0 && cooldown <= 0)
					{
						grip = target.whoAmI;
						Timer = 0f;
						Projectile.netUpdate = true;
					}
					break;
				case Sprite:
					target.AddBuff(Tier == 0 ? BuffID.Frostburn : BuffID.Frostburn2, 180);
					RimeFX.Shatter(target.Center, 0.45f + Tier * 0.08f, 6);
					State = 0f;
					Timer = 0f;
					cooldown = 22;
					Projectile.velocity *= -0.5f;
					Projectile.netUpdate = true;
					break;
				case Beetle:
					FX.Sparks(target.Center, new Color(255, 214, 120, 0), 6, 4f, 0.9f, 16);
					break;
			}
		}

		// ---------------------------------------------------------------------------------------
		private static readonly Color[] TierGlow =
		{
			Color.Transparent, new(53, 201, 184), new(194, 224, 218), new(255, 215, 154),
		};

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Rectangle frame = tex.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
			Vector2 origin = frame.Size() * 0.5f;
			// the sheet faces left, so a beast facing right is the flipped one
			SpriteEffects fx = Projectile.spriteDirection > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Vector2 at = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
			Player owner = Main.player[Projectile.owner];
			bool assembled = Tier >= 1 && Assembled(owner, Type);

			// the court's ring, drawn once by the leader: a faint line beast to beast
			if (assembled && IsLeader())
			{
				List<Projectile> pack = Pack();
				float pulse = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3f);
				for (int i = 0; i < pack.Count; i++)
					RiftDraw.Line(pack[i].Center, pack[(i + 1) % pack.Count].Center,
						RiftDraw.Glow(255, 215, 120, 0.12f + 0.08f * pulse), 1.5f);
			}

			// afterimages while it is committed: the dive and the dart
			bool streaking = (Form == Beetle && State == Diving) || (Form == Sprite && State == 1f);
			if (streaking)
			{
				Color c = FormColor[Form];
				for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
				{
					if (Projectile.oldPos[i] == Vector2.Zero)
						continue;
					float f = 1f - i / (float)Projectile.oldPos.Length;
					Main.EntitySpriteDraw(tex, Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition, frame,
						new Color(c.R, c.G, c.B, 0) * (0.45f * f), Projectile.oldRot[i], origin, Projectile.scale * (0.8f + 0.2f * f), fx, 0);
				}
			}

			// the tier's rim: none on the Horn (T2 stays matte), the node's own colour above it,
			// gold while the court is in session
			if (Tier >= 1)
			{
				Color rim = assembled ? new Color(255, 205, 110) : TierGlow[Tier];
				float s = assembled ? 0.55f + 0.2f * MathF.Sin(Main.GlobalTimeWrappedHourly * 4f + Projectile.whoAmI) : 0.35f;
				for (int k = 0; k < 4; k++)
				{
					Vector2 o = new Vector2(2f, 0f).RotatedBy(k * MathHelper.PiOver2);
					Main.EntitySpriteDraw(tex, at + o, frame, new Color(rim.R, rim.G, rim.B, 0) * s, Projectile.rotation,
						origin, Projectile.scale, fx, 0);
				}
			}

			Main.EntitySpriteDraw(tex, at, frame, lightColor, Projectile.rotation, origin, Projectile.scale, fx, 0);
			return false;
		}
	}

	/// <summary>Menagerie Horn: one beast per summon, cycling crab, beetling, sprite, hatchling.</summary>
	public class HornBeast : MenagerieBeast
	{
		public override int Tier => 0;
		protected override int BuffType => ModContent.BuffType<MenagerieHornBuff>();
	}

	/// <summary>Wild Court: the Horn's beasts, plus the court in session.</summary>
	public class CourtBeast : MenagerieBeast
	{
		public override int Tier => 1;
		protected override int BuffType => ModContent.BuffType<WildCourtBuff>();
	}

	/// <summary>Grove Conclave: the Court's, plus the trade of places every six seconds.</summary>
	public class ConclaveBeast : MenagerieBeast
	{
		public override int Tier => 2;
		protected override int BuffType => ModContent.BuffType<GroveConclaveBuff>();
	}

	/// <summary>The Ark: all four at a quarter slot each, plus the volley every three seconds.</summary>
	public class ArkBeast : MenagerieBeast
	{
		public override int Tier => 3;
		protected override int BuffType => ModContent.BuffType<ArkBuff>();
	}
}
