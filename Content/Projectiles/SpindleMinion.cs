using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.GlobalNPCs;
using Terrapex.Content.Buffs;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// Plan item #81's minion. It hangs off the player on a thread and runs down whatever is
	/// currently stitched.
	///
	/// Preferring the bound target is the whole design: the tier's other three weapons make
	/// the thread, and this is the thing that rewards having made it. A minion that simply
	/// picked the nearest enemy would have nothing to do with T4 at all.
	/// </summary>
	public class SpindleMinion : ModProjectile
	{
		private const float Hover = 58f;
		private const float Speed = 11f;

		private ref float Idle => ref Projectile.ai[0];

		public override void SetStaticDefaults()
		{
			Main.projFrames[Type] = 4;
			Main.projPet[Type] = true;
			ProjectileID.Sets.MinionSacrificable[Type] = true;
			ProjectileID.Sets.MinionTargettingFeature[Type] = true;
			ProjectileID.Sets.TrailCacheLength[Type] = 6;
			ProjectileID.Sets.TrailingMode[Type] = 3;
		}

		public override void SetDefaults()
		{
			Projectile.width = 22;
			Projectile.height = 22;
			Projectile.friendly = true;
			Projectile.minion = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.minionSlots = 1f;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 2;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.netImportant = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
		}

		public override bool MinionContactDamage() => true;

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (owner.dead || !owner.active)
			{
				owner.ClearBuff(ModContent.BuffType<SpindleBuff>());
				return;
			}
			if (owner.HasBuff(ModContent.BuffType<SpindleBuff>()))
				Projectile.timeLeft = 2;

			NPC target = FindTarget(owner);
			if (target != null)
			{
				Vector2 want = Vector2.Normalize(target.Center - Projectile.Center) * Speed;
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, want, 0.10f);
				Idle = 0f;
			}
			else
			{
				// hangs a little behind and above, the way something on a line would
				Vector2 seat = owner.MountedCenter
					+ new Vector2(-owner.direction * Hover, -Hover * 0.45f);
				Vector2 gap = seat - Projectile.Center;
				if (gap.Length() > 22f)
					Projectile.velocity = Vector2.Lerp(Projectile.velocity, gap * 0.10f, 0.20f);
				else
					Projectile.velocity *= 0.90f;

				Idle++;
				Projectile.Center += new Vector2(0f, (float)Math.Sin(Idle * 0.06f) * 0.35f);
			}

			if (Vector2.Distance(Projectile.Center, owner.Center) > 1600f)
			{
				Projectile.Center = owner.Center;
				Projectile.netUpdate = true;
			}

			Projectile.rotation = Projectile.velocity.X * 0.04f;
			Projectile.spriteDirection = Projectile.direction = Projectile.velocity.X > 0f ? 1 : -1;

			if (++Projectile.frameCounter >= 7)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
			}

			Lighting.AddLight(Projectile.Center, 0.14f, 0.34f, 0.32f);
		}

		/// <summary>
		/// A stitched enemy first, then the player's own manual target, then the nearest thing.
		/// </summary>
		private NPC FindTarget(Player owner)
		{
			NPC best = null;
			float bestDist = 900f;
			bool bestBound = false;

			if (owner.HasMinionAttackTargetNPC)
			{
				NPC forced = Main.npc[owner.MinionAttackTargetNPC];
				if (forced.CanBeChasedBy(Projectile))
					return forced;
			}

			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC npc = Main.npc[i];
				if (!npc.CanBeChasedBy(Projectile))
					continue;

				float dist = Vector2.Distance(npc.Center, Projectile.Center);
				if (dist > 900f)
					continue;

				bool bound = npc.GetGlobalNPC<BoundGlobalNPC>().IsBound;
				// a stitched target outranks a closer unstitched one, always
				if (bestBound && !bound)
					continue;
				if (bound && !bestBound)
				{
					best = npc;
					bestDist = dist;
					bestBound = true;
					continue;
				}
				if (dist < bestDist)
				{
					best = npc;
					bestDist = dist;
				}
			}
			return best;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// the line back to the player: without it a spider hanging in mid-air is just
			// another floating pet, and the tier's whole read is threads
			Player owner = Main.player[Projectile.owner];
			Vector2 a = owner.MountedCenter - Main.screenPosition;
			Vector2 b = Projectile.Center - Main.screenPosition;
			Texture2D px = TextureAssets.MagicPixel.Value;
			Rectangle src = new Rectangle(0, 0, 1, 1);

			Vector2 prev = a;
			float sag = MathHelper.Clamp(Vector2.Distance(a, b) * 0.10f, 2f, 20f);
			for (int i = 1; i <= 6; i++)
			{
				float t = i / 6f;
				Vector2 p = Vector2.Lerp(a, b, t);
				p.Y += (float)Math.Sin(t * MathHelper.Pi) * sag;
				Vector2 seg = p - prev;
				Main.EntitySpriteDraw(px, prev, src, new Color(53, 201, 184, 0) * 0.45f,
					seg.ToRotation(), new Vector2(0f, 0.5f),
					new Vector2(seg.Length() + 1f, 1.6f), SpriteEffects.None, 0);
				prev = p;
			}

			RiftDraw.Trail(Projectile, f => RiftDraw.Glow(40, 170, 158, f * 0.30f),
				f => 0.6f + 0.35f * f);
			RiftDraw.Head(Projectile, lightColor);
			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			for (int i = 0; i < 4; i++)
			{
				Dust d = Dust.NewDustPerfect(target.Center, DustID.Vortex,
					Main.rand.NextVector2Circular(2.2f, 2.2f), 120, default, 0.9f);
				d.noGravity = true;
			}
		}
	}
}
