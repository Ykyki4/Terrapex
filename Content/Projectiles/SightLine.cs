using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terrapex.Common;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// The line the First Shard travels along when it reaches something that was watching. It
	/// does no damage at all — the sword has already dealt it — and exists only so the player
	/// can see which enemies the swing actually reached.
	///
	/// A weapon whose whole rule is "it hits everything that can see you" is unusable without
	/// this: the numbers pop somewhere off screen and it looks like the sword is doing nothing.
	/// </summary>
	public class SightLine : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_1";

		private const int Life = 16;

		private ref float ToX => ref Projectile.ai[0];
		private ref float ToY => ref Projectile.ai[1];

		private int Age => Life - Projectile.timeLeft;

		public override void SetDefaults()
		{
			Projectile.width = 4;
			Projectile.height = 4;
			Projectile.aiStyle = -1;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
		}

		public override bool ShouldUpdatePosition() => false;

		public static void Draw(IEntitySource source, Vector2 from, Vector2 to, int owner)
		{
			Projectile p = Projectile.NewProjectileDirect(source, from, Vector2.Zero,
				ModContent.ProjectileType<SightLine>(), 0, 0f, owner, to.X, to.Y);
			p.Center = from;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float f = 1f - Age / (float)Life;
			Vector2 to = new Vector2(ToX, ToY);
			RiftDraw.Line(Projectile.Center, to, RiftDraw.Glow(200, 208, 235, f * 0.5f), 3.5f * f);
			RiftDraw.Line(Projectile.Center, to, Color.White * (f * 0.7f), 1.2f * f);
			RiftDraw.Bloom(to, RiftDraw.Glow(255, 255, 255, f * 0.6f), 0.4f * f);
			return false;
		}
	}
}
