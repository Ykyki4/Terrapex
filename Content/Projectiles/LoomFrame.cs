using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// The frame the Loom Staff stands up. It holds its ground for ten seconds and threads
	/// whatever comes near, which makes the staff a way of claiming a corridor rather than a
	/// way of aiming.
	/// </summary>
	public class LoomFrame : ModProjectile
	{
		private const int Life = 60 * 10;
		private const int Every = 34;
		private const float Range = 480f;

		private ref float Timer => ref Projectile.ai[0];

		public override void SetStaticDefaults() => Main.projFrames[Type] = 4;

		public override void SetDefaults()
		{
			Projectile.width = 34;
			Projectile.height = 42;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void AI()
		{
			Timer++;
			if (++Projectile.frameCounter >= 8)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
			}

			Lighting.AddLight(Projectile.Center, 0.22f, 0.52f, 0.48f);
			if (Main.rand.NextBool(4))
			{
				Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
					DustID.Vortex, 0f, 0f, 130, default, 0.8f);
				d.noGravity = true;
				d.velocity *= 0.3f;
			}

			if (Timer % Every != 0f || Projectile.owner != Main.myPlayer)
				return;

			NPC target = null;
			float best = Range;
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

			Terraria.Audio.SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.4f }, Projectile.Center);
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center,
				Vector2.Normalize(target.Center - Projectile.Center) * 12f,
				ModContent.ProjectileType<WeftBolt>(), (int)(Projectile.damage * 0.6f), 0f, Projectile.owner);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// it fades out over its last second rather than blinking away
			float fade = Math.Min(1f, Projectile.timeLeft / 60f);
			RiftDraw.Head(Projectile, Color.White * fade);
			RiftDraw.Bloom(Projectile.Center, RiftDraw.Glow(60, 210, 195, 0.35f * fade), 0.4f);
			return false;
		}
	}
}
