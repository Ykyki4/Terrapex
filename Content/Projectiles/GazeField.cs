using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// Plan item #116's spell. It does not travel and it does not aim — it simply decides that
	/// a circle of the world is being looked at, and everything inside it is.
	///
	/// Four pulses over a second and a half, each drawn as a ring closing on the centre before
	/// it lands, so the caster can see what will be caught and the enemies inside it get the
	/// same warning every hostile attack in this mod gives.
	/// </summary>
	public class GazeField : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_1";

		public const float Radius = 150f;
		private const int Beat = 28;
		private const int Pulses = 4;
		public const int Life = Beat * Pulses + 12;

		private int Age => Life - Projectile.timeLeft;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = Beat - 4;
		}

		public override bool ShouldUpdatePosition() => false;

		/// <summary>Live only on the frame a pulse lands, so the four beats are four hits.</summary>
		private bool Landing => Age > 0 && Age % Beat == 0;

		public override void AI()
		{
			if (Landing)
			{
				SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.5f, Pitch = 0.7f }, Projectile.Center);
				for (int i = 0; i < 18; i++)
				{
					Dust d = Dust.NewDustPerfect(
						Projectile.Center + Main.rand.NextVector2CircularEdge(Radius, Radius),
						DustID.WhiteTorch, Vector2.Zero, 110, default, 1.1f);
					d.noGravity = true;
					d.velocity = (Projectile.Center - d.position) * 0.05f;
				}
			}
			Lighting.AddLight(Projectile.Center, 0.6f, 0.6f, 0.65f);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (!Landing)
				return false;
			return Vector2.DistanceSquared(targetHitbox.Center.ToVector2(), Projectile.Center)
				<= Radius * Radius;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float scale = Radius / 32f;
			RiftDraw.Ring(Projectile.Center, RiftDraw.Glow(210, 216, 240, 0.35f), scale * 0.55f,
				Age * 0.012f);
			RiftDraw.Ring(Projectile.Center, RiftDraw.Glow(40, 44, 60, 0.45f), scale * 0.44f,
				Age * -0.02f);

			// the pulse closing in: the circle is the warning, and it arrives at the middle on
			// exactly the frame the damage does
			float f = (Age % Beat) / (float)Beat;
			RiftDraw.Ring(Projectile.Center, RiftDraw.Glow(255, 255, 255, 0.55f * (1f - f)),
				scale * 0.55f * (1f - f * 0.85f), Age * 0.05f);
			RiftDraw.Bloom(Projectile.Center,
				RiftDraw.Glow(255, 255, 255, 0.18f + (1f - f) * 0.35f), 0.7f + (1f - f) * 0.8f);
			return false;
		}
	}
}
