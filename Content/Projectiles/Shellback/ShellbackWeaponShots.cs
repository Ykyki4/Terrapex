using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Reaper;
using Terrapex.Content.Buffs;
using Terrapex.Content.Projectiles.Reaper;

namespace Terrapex.Content.Projectiles.Shellback
{
	/// <summary>A barnacle knocked loose by the Wrecker's Cutlass: it tumbles, falls and cuts once.</summary>
	public class BarnacleShrapnel : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = 1;
			Projectile.tileCollide = true;
			Projectile.timeLeft = 90;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void AI()
		{
			Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.25f, 12f);
			Projectile.rotation += Projectile.velocity.X * 0.08f + 0.1f;
		}

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 3; i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.Sand, Main.rand.NextVector2Circular(1.5f, 1.5f), 80, default, 0.8f);
		}
	}

	/// <summary>
	/// The Brine Conch's breath: one bubble that drifts out slowing and swelling, and bursts into
	/// six falling droplets when it touches anything or its breath runs out.
	/// </summary>
	public class ConchBubble : ModProjectile
	{
		public const int Droplets = 6;
		public const float DropletShare = 0.5f;

		public override void SetDefaults()
		{
			Projectile.width = 24;
			Projectile.height = 24;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = 1;
			Projectile.tileCollide = true;
			Projectile.timeLeft = 70;
		}

		public override void AI()
		{
			Projectile.velocity *= 0.985f;
			Projectile.scale = MathHelper.Lerp(1.3f, 0.8f, Projectile.timeLeft / 70f);
			Lighting.AddLight(Projectile.Center, 0.08f, 0.22f, 0.26f);
		}

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Item54, Projectile.Center);
			for (int i = 0; i < 8; i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.Water, Main.rand.NextVector2Circular(3f, 3f), 80, default, 1.1f);
			if (Projectile.owner != Main.myPlayer)
				return;
			for (int i = 0; i < Droplets; i++)
			{
				Vector2 v = Vector2.UnitY.RotatedBy(MathHelper.TwoPi * i / Droplets + MathHelper.Pi / Droplets) * -4.5f;
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, v,
					ModContent.ProjectileType<ConchDroplet>(), (int)(Projectile.damage * DropletShare), 1f, Projectile.owner);
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Color c = Color.Lerp(lightColor, Color.White, 0.5f) * 0.9f;
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, c, 0f,
				tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
			return false;
		}
	}

	/// <summary>A drop of brine out of a burst conch bubble. It arcs and falls.</summary>
	public class ConchDroplet : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = 1;
			Projectile.tileCollide = true;
			Projectile.timeLeft = 60;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void AI()
		{
			Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.22f, 10f);
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
		}

		public override void OnKill(int timeLeft)
			=> Dust.NewDustPerfect(Projectile.Center, DustID.Water, Main.rand.NextVector2Circular(1f, 1f), 80, default, 0.9f);
	}

	/// <summary>
	/// The Undertow's whirlpool, opened on the point after the cut. For 50 ticks it drags what
	/// stands near it toward the middle and hits each thing in it every 15 ticks. Bosses and
	/// anything that ignores knockback are not moved, only worn at.
	/// </summary>
	public class UndertowWhirl : ModProjectile
	{
		public const float PullRadius = 150f;
		private const int Life = 50;

		public override void SetDefaults()
		{
			Projectile.width = 96;
			Projectile.height = 96;
			Projectile.friendly = true;
			Projectile.DamageType = ModContent.GetInstance<ReapingDamage>();
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Life;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 15;
		}

		public override void AI()
		{
			Projectile.rotation -= 0.25f;
			Lighting.AddLight(Projectile.Center, 0.12f, 0.32f, 0.34f);
			if (Main.rand.NextBool(2))
			{
				Vector2 off = Main.rand.NextVector2CircularEdge(PullRadius * 0.6f, PullRadius * 0.6f);
				Dust d = Dust.NewDustPerfect(Projectile.Center + off, DustID.Water, -off.RotatedBy(1.1f) * 0.05f, 80, default, 1f);
				d.noGravity = true;
			}

			// the pull is an NPC position change, so the server does it and the NPC syncs
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			foreach (NPC n in Main.ActiveNPCs)
			{
				if (n.boss || n.friendly || n.knockBackResist <= 0f || n.dontTakeDamage)
					continue;
				Vector2 to = Projectile.Center - n.Center;
				float d = to.Length();
				if (d > PullRadius || d < 8f)
					continue;
				n.velocity = Vector2.Lerp(n.velocity, to / d * 4f * n.knockBackResist, 0.18f);
				if (Projectile.timeLeft % 10 == 0)
					n.netUpdate = true;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => ReapingSlash.RangedHit(Projectile, target, hit);

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			float f = Projectile.timeLeft / (float)Life;
			float fade = f > 0.8f ? (1f - f) / 0.2f : (f < 0.3f ? f / 0.3f : 1f);
			Vector2 at = Projectile.Center - Main.screenPosition;
			Main.EntitySpriteDraw(tex, at, null, RiftDraw.Glow(140, 230, 220, 0.7f * fade), Projectile.rotation,
				tex.Size() / 2f, 1f, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(tex, at, null, Color.White * 0.6f * fade, Projectile.rotation * 1.6f + 1f,
				tex.Size() / 2f, 0.6f, SpriteEffects.None, 0);
			return false;
		}
	}

	/// <summary>
	/// The Rusted Crab Pot's hermit crab. It walks the ground like the Riftling Hatchling - a
	/// lane holder, not a flyer - and hops at what it is chasing. Teleports home when stranded.
	/// </summary>
	public class PotCrabMinion : ModProjectile
	{
		private const float Range = 520f;
		private const float WalkSpeed = 5f;

		private ref float Jump => ref Projectile.ai[0];

		public override void SetStaticDefaults()
		{
			Main.projPet[Type] = true;
			ProjectileID.Sets.MinionSacrificable[Type] = true;
			ProjectileID.Sets.MinionTargettingFeature[Type] = true;
			Main.projFrames[Type] = 4;
		}

		public override void SetDefaults()
		{
			Projectile.width = 24;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.minion = true;
			Projectile.minionSlots = 1f;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 18000;
			Projectile.tileCollide = true;
			Projectile.netImportant = true;
			Projectile.aiStyle = -1;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
		}

		public override bool MinionContactDamage() => true;

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (owner.dead || !owner.active)
			{
				owner.ClearBuff(ModContent.BuffType<PotCrabBuff>());
				Projectile.Kill();
				return;
			}
			if (owner.HasBuff(ModContent.BuffType<PotCrabBuff>()))
				Projectile.timeLeft = 2;

			if (Projectile.Distance(owner.Center) > 1400f)
			{
				Projectile.Center = owner.Center;
				Projectile.velocity = Vector2.Zero;
			}

			NPC target = FindTarget(owner);
			// several crabs spread out behind the player instead of stacking on one spot
			Vector2 goal = target?.Center ?? owner.Center + new Vector2(-owner.direction * (40f + Projectile.minionPos * 24f), 0f);
			float dx = goal.X - Projectile.Center.X;

			if (Math.Abs(dx) < (target != null ? 10f : 30f))
				Projectile.velocity.X *= 0.8f;
			else
				Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, Math.Sign(dx) * WalkSpeed, 0.15f);

			Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.35f, 12f);

			bool grounded = Projectile.velocity.Y == 0f;
			if (grounded && Jump <= 0f)
			{
				bool blocked = Collision.SolidCollision(
					Projectile.position + new Vector2(Math.Sign(dx) * 10f, 0f), Projectile.width, Projectile.height - 8);
				bool above = target != null && target.Center.Y < Projectile.Center.Y - 30f;
				bool lunge = target != null && Math.Abs(dx) < 70f;
				if (blocked || above || lunge)
				{
					Projectile.velocity.Y = lunge && !blocked ? -5f : -7.4f;
					Jump = 16f;
				}
			}
			if (Jump > 0f)
				Jump--;

			Projectile.spriteDirection = Projectile.direction = Projectile.velocity.X >= 0f ? 1 : -1;

			// 0-2 walk, 3 airborne
			if (!grounded)
				Projectile.frame = 3;
			else if (Math.Abs(Projectile.velocity.X) > 0.4f)
			{
				if (++Projectile.frameCounter >= 5)
				{
					Projectile.frameCounter = 0;
					Projectile.frame = (Projectile.frame + 1) % 3;
				}
			}
			else
				Projectile.frame = 0;
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
			foreach (NPC n in Main.ActiveNPCs)
			{
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
