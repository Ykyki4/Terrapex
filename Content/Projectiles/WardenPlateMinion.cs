using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;
using Terrapex.Common;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// Plan item #61's minion, and the fight's own idea handed back: a shell plate that keeps
	/// an orbit around the player, eats hostile shots that touch it, and leaves the orbit only
	/// to ram. It advances its orbit slot while ramming, exactly like the boss's plates, so it
	/// always comes back into formation.
	/// </summary>
	public class WardenPlateMinion : ModProjectile
	{
		private const float Radius = 62f;
		private const float Spin = 0.028f;
		private const float Range = 620f;
		private const int RamTime = 40;
		private const int BlockCooldown = 180;

		private ref float Angle => ref Projectile.ai[0];
		private ref float Ram => ref Projectile.ai[1];
		private ref float Blocked => ref Projectile.localAI[0];

		public override void SetStaticDefaults()
		{
			Main.projPet[Type] = true;
			ProjectileID.Sets.MinionSacrificable[Type] = true;
			ProjectileID.Sets.MinionTargettingFeature[Type] = true;
			ProjectileID.Sets.TrailCacheLength[Type] = 8;
			ProjectileID.Sets.TrailingMode[Type] = 3;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// the ram is the only thing it does that needs reading across a screen, so it is
			// the only thing that trails; the rest of the time it just holds station
			if (Ram > 0f)
			{
				float t = Ram / RamTime;
				RiftDraw.Trail(Projectile,
					f => RiftDraw.Glow(150, 60, 210, f * 0.45f * t),
					f => 0.60f + 0.35f * f);
			}

			// dim while the block is on cooldown, so you can see at a glance that the plate
			// is currently just a body and will not eat the next shot
			RiftDraw.Bloom(Projectile.Center,
				RiftDraw.Glow(190, 90, 230, Blocked > 0f ? 0.12f : 0.30f), 0.22f);
			RiftDraw.Head(Projectile, lightColor);
			return false;
		}

		public override void SetDefaults()
		{
			Projectile.width = 26;
			Projectile.height = 26;
			Projectile.friendly = true;
			Projectile.minion = true;
			Projectile.minionSlots = 1f;
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

		public override bool MinionContactDamage() => true;

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (owner.dead || !owner.active)
			{
				owner.ClearBuff(ModContent.BuffType<WardenPlateBuff>());
				Projectile.Kill();
				return;
			}
			if (owner.HasBuff(ModContent.BuffType<WardenPlateBuff>()))
				Projectile.timeLeft = 2;

			// the orbit slot keeps turning even mid-ram, so the plate returns to formation
			Angle += Spin;
			if (Blocked > 0f)
				Blocked--;

			int slot = 0, total = 0;
			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				Projectile p = Main.projectile[i];
				if (!p.active || p.owner != Projectile.owner || p.type != Type)
					continue;
				if (i < Projectile.whoAmI)
					slot++;
				total++;
			}
			float spread = MathHelper.TwoPi / Math.Max(1, total);
			Vector2 seat = owner.MountedCenter + Angle.ToRotationVector2().RotatedBy(slot * spread) * Radius;

			NPC target = FindTarget(owner);
			if (Ram > 0f)
			{
				Ram--;
				if (target != null)
					Projectile.velocity = Vector2.Lerp(Projectile.velocity,
						Projectile.DirectionTo(target.Center) * 15f, 0.2f);
			}
			else
			{
				Vector2 to = seat - Projectile.Center;
				if (to.Length() > 900f)
				{
					Projectile.Center = seat;
					Projectile.velocity = Vector2.Zero;
				}
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, to * 0.24f, 0.35f);

				if (target != null && Projectile.Distance(target.Center) < Range)
				{
					Ram = RamTime;
					Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.4f }, Projectile.Center);
				}
			}

			Projectile.rotation += 0.05f;

			// the plate's other half of the job: it stops shots that touch it
			if (Blocked <= 0f && Projectile.owner == Main.myPlayer)
				BlockNearbyShot();

			Lighting.AddLight(Projectile.Center, 0.26f, 0.10f, 0.34f);
			if (Main.rand.NextBool(14))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
					DustID.PurpleTorch, Vector2.Zero, 150, default, 0.8f);
				d.noGravity = true;
			}
		}

		private void BlockNearbyShot()
		{
			Rectangle box = Projectile.Hitbox;
			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				Projectile shot = Main.projectile[i];
				if (!shot.active || !shot.hostile || shot.damage <= 0 || !shot.Hitbox.Intersects(box))
					continue;

				shot.Kill();
				Blocked = BlockCooldown;
				Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit42 with { Volume = 0.5f }, Projectile.Center);
				for (int k = 0; k < 12; k++)
				{
					Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Stone,
						Main.rand.NextVector2Circular(4f, 4f), 90, default, 1.2f);
					d.noGravity = true;
				}
				return;
			}
		}

		private NPC FindTarget(Player owner)
		{
			if (owner.HasMinionAttackTargetNPC)
			{
				NPC forced = Main.npc[owner.MinionAttackTargetNPC];
				if (forced.CanBeChasedBy(Projectile) && Projectile.Distance(forced.Center) < Range * 1.6f)
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
				if (d < bestDist) { bestDist = d; best = n; }
			}
			return best;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			Ram = 0f;
			Projectile.velocity *= -0.4f;
			target.AddBuff(ModContent.BuffType<Cracked>(), 180);
		}
	}
}
