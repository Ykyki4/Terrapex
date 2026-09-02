using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Content.Buffs;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// Plan item #63's round. It borrows the boss's own shard sprite, and its behaviour: slow
	/// out of the barrel, then winding up. That makes the gun reward leading a target rather
	/// than tracking one, which is the only thing separating it from a vanilla repeater.
	/// </summary>
	public class CasterShard : ModProjectile
	{
		public override string Texture => "Terrapex/Content/Projectiles/RiftShard";

		private const int AccelDelay = 10;
		private const float MaxBoost = 2.1f;

		private ref float LaunchSpeed => ref Projectile.ai[0];
		private ref float Timer => ref Projectile.ai[1];

		public override void SetStaticDefaults()
		{
			Main.projFrames[Type] = 8;
			ProjectileID.Sets.TrailCacheLength[Type] = 7;
			ProjectileID.Sets.TrailingMode[Type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 300;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 10;
		}

		public override void AI()
		{
			Timer++;
			if (LaunchSpeed == 0f)
				LaunchSpeed = Projectile.velocity.Length();

			if (Timer > AccelDelay)
			{
				float want = LaunchSpeed * MaxBoost;
				if (Projectile.velocity.Length() < want)
					Projectile.velocity *= 1.035f;
			}

			if (++Projectile.frameCounter >= 4)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
			}

			Lighting.AddLight(Projectile.Center, 0.3f, 0.12f, 0.4f);
			if (Main.rand.NextBool(3))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
					Projectile.velocity * -0.07f, 120, default, 0.95f);
				d.noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// the round's whole point is that it winds up, so the trail is the readout: it
			// reaches further back and burns brighter the faster the shot is already going
			float wind = LaunchSpeed > 0f
				? Utils.GetLerpValue(LaunchSpeed, LaunchSpeed * MaxBoost, Projectile.velocity.Length(), true)
				: 0f;

			RiftDraw.Trail(Projectile,
				f => RiftDraw.Glow(150, 62, 220, f * (0.22f + 0.50f * wind)),
				f => 0.50f + 0.42f * f,
				0.35f + 0.65f * wind);
			RiftDraw.Bloom(Projectile.Center, RiftDraw.Glow(216, 118, 240, 0.30f + 0.45f * wind),
				0.14f + 0.14f * wind);
			RiftDraw.Head(Projectile, Color.White);
			return false;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
			=> target.AddBuff(ModContent.BuffType<Cracked>(), 180);

		public override void OnKill(int timeLeft)
		{
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.4f }, Projectile.Center);
			for (int i = 0; i < 8; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
					Main.rand.NextVector2Circular(3f, 3f), 110, default, 1.1f);
				d.noGravity = true;
			}
		}
	}
}
