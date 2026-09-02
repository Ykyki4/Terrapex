using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Content.Buffs;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// Plan item #117's minion: a Keeper the size of your hand, with three plates of its own.
	///
	/// It is the only minion in the mod that throws nothing. It turns a pupil toward whatever
	/// you are fighting at a capped rate, and once the pupil is on target the line between them
	/// is simply live. That makes it the exact opposite of a homing minion — worth most against
	/// something that holds still, worst against something that circles — so a bench of four
	/// wants you to pin the enemy down rather than to kite it. Which is also the tier's whole
	/// argument, restated in a place the player controls.
	/// </summary>
	public class KeeperMinion : ModProjectile
	{
		private const float Reach = 430f;
		private const float TurnRate = 0.055f;
		private const float Aligned = 0.13f;

		private ref float Pupil => ref Projectile.localAI[0];
		private ref float Beat => ref Projectile.localAI[1];

		public override void SetStaticDefaults()
		{
			Main.projFrames[Type] = 4;
			Main.projPet[Type] = true;
			ProjectileID.Sets.MinionSacrificable[Type] = true;
			ProjectileID.Sets.MinionTargettingFeature[Type] = true;
		}

		public override void SetDefaults()
		{
			Projectile.width = 28;
			Projectile.height = 28;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.minion = true;
			Projectile.minionSlots = 1f;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 18000;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.netImportant = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 14;
		}

		/// <summary>Standard minion housekeeping: the buff is the lease on being alive.</summary>
		private bool CheckActive(Player owner)
		{
			if (owner.dead || !owner.active)
			{
				owner.ClearBuff(ModContent.BuffType<KeeperMinionBuff>());
				return false;
			}
			if (owner.HasBuff<KeeperMinionBuff>())
				Projectile.timeLeft = 2;
			return true;
		}

		/// <summary>The thing it is looking at, or null. Honours the player's own targeting.</summary>
		private NPC FindTarget(Player owner)
		{
			if (owner.HasMinionAttackTargetNPC)
			{
				NPC chosen = Main.npc[owner.MinionAttackTargetNPC];
				if (chosen.CanBeChasedBy(Projectile) && Projectile.Distance(chosen.Center) < Reach * 1.6f)
					return chosen;
			}

			NPC best = null;
			float dist = Reach * 1.4f;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.CanBeChasedBy(Projectile))
					continue;
				float d = Projectile.Distance(n.Center);
				if (d < dist) { dist = d; best = n; }
			}
			return best;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!CheckActive(owner))
				return;

			Beat++;
			NPC target = FindTarget(owner);

			// it stands off rather than chasing: a minion whose weapon is a line must not also
			// be the thing closing the distance, or the pupil never has to be aimed
			Vector2 seat = target != null
				? target.Center + (Projectile.Center - target.Center).SafeNormalize(Vector2.UnitX) * 260f
				: owner.MountedCenter + new Vector2(-owner.direction * 52f,
					-46f + (float)Math.Sin(Beat * 0.05f) * 8f);

			Vector2 want = (seat - Projectile.Center) * 0.06f;
			float cap = target != null ? 12f : 18f;
			if (want.Length() > cap)
				want = Vector2.Normalize(want) * cap;
			Projectile.velocity = Vector2.Lerp(Projectile.velocity, want, 0.2f);

			// stuck behind the world for too long? snap home, the way every vanilla minion does
			if (Vector2.DistanceSquared(Projectile.Center, owner.MountedCenter) > 2400f * 2400f)
			{
				Projectile.Center = owner.MountedCenter;
				Projectile.netUpdate = true;
			}

			if (target != null)
			{
				float wantAngle = (target.Center - Projectile.Center).ToRotation();
				Pupil = MathHelper.WrapAngle(Pupil
					+ MathHelper.Clamp(MathHelper.WrapAngle(wantAngle - Pupil), -TurnRate, TurnRate));
			}
			else
			{
				Pupil = MathHelper.WrapAngle(Pupil + 0.01f);
			}

			SettleAim(target);

			Projectile.rotation = Projectile.velocity.X * 0.03f;
			if (++Projectile.frameCounter >= 8)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
			}

			Lighting.AddLight(Projectile.Center, 0.35f, 0.35f, 0.4f);

			if (live && Main.rand.NextBool(2))
			{
				Vector2 at = Projectile.Center + Pupil.ToRotationVector2() * Main.rand.NextFloat(20f, Reach);
				Dust d = Dust.NewDustPerfect(at, DustID.WhiteTorch, Vector2.Zero, 130, default, 0.7f);
				d.noGravity = true;
			}
		}

		/// <summary>
		/// The beam is on only while the pupil has actually arrived. Settled once a tick in
		/// <see cref="AI"/> and cached: <see cref="Colliding"/> is called once per NPC per tick,
		/// so working this out in the property meant a two-hundred-NPC scan two hundred times a
		/// tick, per minion.
		/// </summary>
		private bool live;

		private void SettleAim(NPC target)
		{
			if (target == null)
			{
				live = false;
				return;
			}
			float to = (target.Center - Projectile.Center).ToRotation();
			live = Math.Abs(MathHelper.WrapAngle(to - Pupil)) <= Aligned;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (!live)
				return false;
			float point = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
				Projectile.Center, Projectile.Center + Pupil.ToRotationVector2() * Reach, 14f, ref point);
		}

		/// <summary>
		/// True, even though this minion never rams anything.
		///
		/// Returning false does not merely switch off contact damage — tModLoader skips the
		/// minion's whole NPC-damage pass, so <see cref="Colliding"/> is never called and the
		/// beam becomes decoration. A minion that wants a hitbox of its own has to say true and
		/// then narrow it here; a minion that says false has to deal its damage through a
		/// separate projectile, which is what <c>SailclothMinion</c> does with its thread.
		/// </summary>
		public override bool MinionContactDamage() => true;

		public override void PostDraw(Color lightColor)
		{

			// three plates of its own, each breathing on its own phase — the same shell shape
			// the mod has been drawing since the first boss, at a twelfth of the size
			for (int i = 0; i < 3; i++)
			{
				float a = Beat * 0.045f + i * MathHelper.TwoPi / 3f;
				Vector2 at = Projectile.Center + a.ToRotationVector2() * 22f;
				RiftDraw.Bloom(at, RiftDraw.Glow(210, 216, 240, 0.35f), 0.24f);
			}

			RiftDraw.Bloom(Projectile.Center + Pupil.ToRotationVector2() * 8f,
				RiftDraw.Glow(255, 255, 255, live ? 0.85f : 0.35f), live ? 0.45f : 0.3f);

			if (!live)
				return;

			Vector2 tip = Projectile.Center + Pupil.ToRotationVector2() * Reach;
			float pulse = 1f + 0.12f * (float)Math.Sin(Beat * 0.3f);
			RiftDraw.Line(Projectile.Center, tip, RiftDraw.Glow(26, 28, 40, 0.45f), 11f * pulse);
			RiftDraw.Line(Projectile.Center, tip, RiftDraw.Glow(200, 208, 235, 0.55f), 5.5f * pulse);
			RiftDraw.Line(Projectile.Center, tip, Color.White * 0.8f, 1.8f);
		}

	}
}
