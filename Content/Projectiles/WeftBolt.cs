using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Players;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// The Weft round. On a hit it looks for the next body and jumps, drawing the thread as it
	/// goes, until it runs out of chain or out of neighbours.
	/// </summary>
	public class WeftBolt : ModProjectile
	{
		public const int MaxChain = 4;
		private const float JumpRange = 300f;
		private const float Falloff = 0.82f;

		private ref float Chain => ref Projectile.ai[0];
		private ref float CameFrom => ref Projectile.ai[1];

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 10;
			ProjectileID.Sets.TrailingMode[Type] = 3;
		}

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 220;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
		}

		public override void AI()
		{
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			Lighting.AddLight(Projectile.Center, 0.12f, 0.34f, 0.32f);
			if (Main.rand.NextBool(3))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Vortex,
					Projectile.velocity * -0.05f, 140, default, 0.7f);
				d.noGravity = true;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			int limit = MaxChain;
			Player shooter = Main.player[Projectile.owner];
			if (shooter.active && shooter.GetModPlayer<TerrapexPlayer>().weaverRanged)
				limit += 2;
			if (Projectile.owner != Main.myPlayer || Chain >= limit)
				return;

			NPC next = null;
			float best = JumpRange;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.CanBeChasedBy(Projectile) || n.whoAmI == target.whoAmI || n.whoAmI == (int)CameFrom)
					continue;
				float d = Vector2.Distance(n.Center, target.Center);
				if (d < best)
				{
					best = d;
					next = n;
				}
			}
			if (next == null)
				return;

			Vector2 aim = Vector2.Normalize(next.Center - target.Center) * Projectile.velocity.Length();
			Projectile p = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), target.Center,
				aim, Type, (int)(Projectile.damage * Falloff), Projectile.knockBack, Projectile.owner);
			p.ai[0] = Chain + 1f;
			p.ai[1] = target.whoAmI;
			p.netUpdate = true;

			for (int i = 0; i <= 12; i++)
			{
				Dust d = Dust.NewDustPerfect(Vector2.Lerp(target.Center, next.Center, i / 12f),
					DustID.Vortex, Vector2.Zero, 120, default, 0.85f);
				d.noGravity = true;
				d.velocity *= 0.2f;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			RiftDraw.Trail(Projectile,
				f => RiftDraw.Glow(60, 210, 195, f * 0.55f),
				f => 0.30f + 0.32f * f);
			RiftDraw.Head(Projectile, Color.White);
			return false;
		}
	}
}
