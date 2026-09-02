using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// Drifts to a halt, sits blinking, then bursts into a ring of shards.
	/// Fuses are staggered by the spawner so a field of them detonates as a cascade.
	/// </summary>
	// Borrows RiftOrb.png. The orb's own class is gone - it was the homing projectile the
	// Weaver's playtest threw out ("розовые штуки вообще непредсказуемые") and nothing spawned
	// it any more - but the texture is still the right sprite for a mine, so the PNG stays.
	public class RiftMine : ModProjectile
	{
		private const int Fuse = 110;

		// reuses the orb artwork rather than shipping a near-identical sprite
		public override string Texture => "Terrapex/Content/Projectiles/RiftOrb";

		private ref float FuseOffset => ref Projectile.ai[0];
		private ref float Timer => ref Projectile.ai[1];

		public override void SetStaticDefaults()
		{
			Main.projFrames[Type] = 6;
		}

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 600;
			Projectile.aiStyle = -1;
			Projectile.penetrate = -1;
		}

		public override void AI()
		{
			Timer++;
			Projectile.velocity *= 0.94f;

			int fuse = Fuse + (int)FuseOffset;
			float charge = MathHelper.Clamp(Timer / fuse, 0f, 1f);

			// blinks faster and faster as the fuse runs down
			int frameTicks = (int)MathHelper.Lerp(9f, 2f, charge);
			if (++Projectile.frameCounter >= frameTicks)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
			}

			Projectile.scale = 1f + 0.35f * charge * (float)System.Math.Sin(Timer * 0.35f) * charge;
			Lighting.AddLight(Projectile.Center, 0.6f + charge * 0.5f, 0.2f, 0.8f + charge * 0.4f);

			if (Main.rand.NextBool(charge > 0.7f ? 1 : 3))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
					DustID.PinkTorch, Vector2.Zero, 120, default, 0.9f + charge);
				d.noGravity = true;
				d.velocity *= 0.2f;
			}

			if (Timer >= fuse)
				Projectile.Kill();
		}

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Item14 with { Pitch = 0.4f }, Projectile.Center);

			for (int i = 0; i < 34; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
					Main.rand.NextVector2CircularEdge(7f, 7f), 100, default, 1.7f);
				d.noGravity = true;
			}

			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			const int count = 10;
			float spin = Main.rand.NextFloat(MathHelper.TwoPi);
			for (int i = 0; i < count; i++)
			{
				Vector2 v = (MathHelper.TwoPi / count * i + spin).ToRotationVector2() * 4.6f;
				Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, v,
					ModContent.ProjectileType<RiftShard>(), Projectile.damage, 2f, Projectile.owner);
			}
		}
	}
}
