using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// The Glass Arrow in flight. It shatters on whatever it hits, throwing three
	/// splinters back the way it came — glass does not survive the impact, so the
	/// damage has to come out of the break.
	/// </summary>
	public class GlassArrowProjectile : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 420;
			Projectile.aiStyle = ProjAIStyleID.Arrow;
			AIType = ProjectileID.WoodenArrowFriendly;
			Projectile.arrow = true;
		}

		public override void OnKill(int timeLeft)
		{
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Shatter, Projectile.Center);
			for (int i = 0; i < 8; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Glass,
					Main.rand.NextVector2Circular(3f, 3f), 110, default, 0.9f);
				d.noGravity = true;
			}

			if (Main.myPlayer != Projectile.owner)
				return;

			Vector2 back = -Vector2.Normalize(Projectile.velocity.LengthSquared() > 0.01f
				? Projectile.velocity : Vector2.UnitY);
			for (int i = -1; i <= 1; i++)
			{
				Vector2 v = back.RotatedBy(i * 0.42f) * 5.5f;
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, v,
					ProjectileID.CrystalShard, (int)(Projectile.damage * 0.4f), 0f, Projectile.owner);
			}
		}
	}
}
