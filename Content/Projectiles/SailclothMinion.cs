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
	/// The Sailcloth panel. It stays put and lashes anything inside its reach, so a summoner
	/// running it is choosing ground instead of chasing.
	/// </summary>
	public class SailclothMinion : ModProjectile
	{
		private const int Every = 40;
		private const float Reach = 320f;

		private ref float Timer => ref Projectile.ai[0];

		public override void SetStaticDefaults()
		{
			Main.projFrames[Type] = 4;
			Main.projPet[Type] = true;
			ProjectileID.Sets.MinionSacrificable[Type] = true;
			ProjectileID.Sets.MinionTargettingFeature[Type] = true;
		}

		public override void SetDefaults()
		{
			Projectile.width = 36;
			Projectile.height = 44;
			Projectile.friendly = true;
			Projectile.minion = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.minionSlots = 1f;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 2;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.netImportant = true;
		}

		public override bool ShouldUpdatePosition() => false;

		public override bool MinionContactDamage() => false;

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (owner.dead || !owner.active)
			{
				owner.ClearBuff(ModContent.BuffType<SailclothBuff>());
				return;
			}
			if (owner.HasBuff(ModContent.BuffType<SailclothBuff>()))
				Projectile.timeLeft = 2;

			Timer++;
			if (++Projectile.frameCounter >= 9)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
			}
			Lighting.AddLight(Projectile.Center, 0.16f, 0.40f, 0.38f);

			if (Timer % Every != 0f || Projectile.owner != Main.myPlayer)
				return;

			NPC target = null;
			float best = Reach;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.CanBeChasedBy(Projectile))
					continue;
				float d = Vector2.Distance(n.Center, Projectile.Center);
				if (d < best)
				{
					best = d;
					target = n;
				}
			}
			if (target == null)
				return;

			FriendlyThread.Between(Projectile.GetSource_FromThis(), Projectile.Center,
				target.Center, Projectile.damage, Projectile.owner, 60);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			RiftDraw.Head(Projectile, lightColor);
			RiftDraw.Bloom(Projectile.Center, RiftDraw.Glow(60, 210, 195, 0.28f), 0.34f);
			return false;
		}
	}
}
