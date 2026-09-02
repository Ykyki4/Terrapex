using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// Plan item #24's minion: a chip of the Dormant Eye that never quite woke up.
	/// It idles beside the player and darts at whatever they are fighting, then comes
	/// back — no bolts, because a contact darter reads at a glance and a tiny bullet
	/// from a tiny eye does not.
	/// </summary>
	public class SleeperEye : ModProjectile
	{
		private const float Range = 620f;
		private const int DartTime = 34;

		private ref float Dart => ref Projectile.ai[0];      // >0 while lunging
		private ref float TargetIndex => ref Projectile.ai[1];

		public override void SetStaticDefaults()
		{
			Main.projPet[Type] = true;
			ProjectileID.Sets.MinionSacrificable[Type] = true;
			ProjectileID.Sets.MinionTargettingFeature[Type] = true;
			Main.projFrames[Type] = 4;
		}

		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 20;
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
				owner.ClearBuff(ModContent.BuffType<SleeperEyeBuff>());
				Projectile.Kill();
				return;
			}
			if (owner.HasBuff(ModContent.BuffType<SleeperEyeBuff>()))
				Projectile.timeLeft = 2;

			NPC target = FindTarget(owner);

			if (Dart > 0f)
			{
				Dart--;
				if (target != null)
				{
					Vector2 to = target.Center - Projectile.Center;
					Projectile.velocity = Vector2.Lerp(Projectile.velocity,
						Vector2.Normalize(to) * 11f, 0.16f);
				}
				Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			}
			else
			{
				// idle: hold a slot beside the owner, bobbing
				int slot = 0;
				for (int i = 0; i < Projectile.whoAmI; i++)
					if (Main.projectile[i].active && Main.projectile[i].owner == Projectile.owner
						&& Main.projectile[i].type == Type)
						slot++;
				Vector2 rest = owner.Center
					+ new Vector2(-owner.direction * (44f + slot * 26f), -52f + (float)Math.Sin(Main.GameUpdateCount * 0.05f + slot) * 8f);
				Vector2 to = rest - Projectile.Center;
				if (to.Length() > 620f)
				{
					Projectile.Center = owner.Center;
					Projectile.velocity *= 0.2f;
				}
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, to * 0.10f, 0.2f);
				Projectile.rotation = 0f;

				if (target != null)
				{
					Dart = DartTime;
					TargetIndex = target.whoAmI;
					Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.4f }, Projectile.Center);
				}
			}

			if (++Projectile.frameCounter >= 8)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
			}

			Lighting.AddLight(Projectile.Center, 0.28f, 0.10f, 0.36f);
			if (Main.rand.NextBool(10))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
					DustID.PurpleTorch, Vector2.Zero, 150, default, 0.7f);
				d.noGravity = true;
			}
		}

		private NPC FindTarget(Player owner)
		{
			NPC best = null;
			float bestDist = Range;

			if (owner.HasMinionAttackTargetNPC)
			{
				NPC forced = Main.npc[owner.MinionAttackTargetNPC];
				if (forced.CanBeChasedBy(Projectile) && Projectile.Distance(forced.Center) < Range * 1.5f)
					return forced;
			}
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

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			Dart = 0f;
			Projectile.velocity *= -0.5f;
		}


	}
}
