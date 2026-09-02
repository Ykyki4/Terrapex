using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// Plan item #41's minion. A Riftling that never grew: it walks the floor instead of
	/// flying, which is the point — it holds a lane the player is not standing in, and it
	/// cannot follow them up a shaft. The Sleeper Eye already covers the flying darter.
	/// </summary>
	public class RiftlingHatchling : ModProjectile
	{
		private const float Range = 560f;
		private const float WalkSpeed = 5.4f;

		private ref float Jump => ref Projectile.ai[0];

		public override void SetStaticDefaults()
		{
			Main.projPet[Type] = true;
			ProjectileID.Sets.MinionSacrificable[Type] = true;
			ProjectileID.Sets.MinionTargettingFeature[Type] = true;
			Main.projFrames[Type] = 6;
		}

		public override void SetDefaults()
		{
			Projectile.width = 22;
			Projectile.height = 24;
			Projectile.friendly = true;
			Projectile.minion = true;
			Projectile.minionSlots = 1f;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 18000;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = false;
			Projectile.netImportant = true;
			Projectile.aiStyle = -1;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 18;
		}

		public override bool MinionContactDamage() => true;

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (owner.dead || !owner.active)
			{
				owner.ClearBuff(ModContent.BuffType<HatchlingBuff>());
				Projectile.Kill();
				return;
			}
			if (owner.HasBuff(ModContent.BuffType<HatchlingBuff>()))
				Projectile.timeLeft = 2;

			// a ground minion strands itself constantly; teleporting home is the only
			// thing that keeps one usable, and vanilla's own walkers do exactly this
			if (Projectile.Distance(owner.Center) > 1400f)
			{
				Projectile.Center = owner.Center;
				Projectile.velocity = Vector2.Zero;
			}

			NPC target = FindTarget(owner);
			Vector2 goal = target?.Center ?? owner.Center + new Vector2(-owner.direction * 48f, 0f);
			float dx = goal.X - Projectile.Center.X;

			bool close = Math.Abs(dx) < (target != null ? 12f : 40f);
			if (close)
				Projectile.velocity.X *= 0.8f;
			else
				Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, Math.Sign(dx) * WalkSpeed, 0.14f);

			Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.35f, 12f);

			// hop over a step, or up toward a target standing above it
			bool grounded = Projectile.velocity.Y == 0f;
			if (grounded && Jump <= 0f)
			{
				bool blocked = Collision.SolidCollision(
					Projectile.position + new Vector2(Math.Sign(dx) * 10f, 0f), Projectile.width, Projectile.height - 8);
				bool above = target != null && target.Center.Y < Projectile.Center.Y - 40f;
				if (blocked || above)
				{
					Projectile.velocity.Y = -7.6f;
					Jump = 14f;
				}
			}
			if (Jump > 0f)
				Jump--;

			Projectile.spriteDirection = Projectile.direction = Projectile.velocity.X >= 0f ? 1 : -1;

			// 0-3 walk, 4-5 airborne
			if (!grounded)
			{
				Projectile.frame = Projectile.velocity.Y < 0f ? 4 : 5;
			}
			else if (Math.Abs(Projectile.velocity.X) > 0.4f)
			{
				if (++Projectile.frameCounter >= 6)
				{
					Projectile.frameCounter = 0;
					Projectile.frame = (Projectile.frame + 1) % 4;
				}
			}
			else
			{
				Projectile.frame = 0;
			}

			Lighting.AddLight(Projectile.Center, 0.24f, 0.09f, 0.32f);
			if (Main.rand.NextBool(12))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(7f, 8f),
					DustID.PurpleTorch, Vector2.Zero, 150, default, 0.7f);
				d.noGravity = true;
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			if (oldVelocity.Y > 0f)
				Projectile.velocity.Y = 0f;
			return false;
		}

		private NPC FindTarget(Player owner)
		{
			if (owner.HasMinionAttackTargetNPC)
			{
				NPC forced = Main.npc[owner.MinionAttackTargetNPC];
				if (forced.CanBeChasedBy(Projectile) && Projectile.Distance(forced.Center) < Range * 1.5f)
					return forced;
			}

			NPC best = null;
			float bestDist = Range;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.CanBeChasedBy(Projectile))
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
	}
}
