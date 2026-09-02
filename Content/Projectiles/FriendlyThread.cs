using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// A thread the player leaves behind: a stationary segment that hurts whatever crosses it.
	///
	/// The Weaver's own threads and this one are the same idea pointed in opposite directions,
	/// which is the point of the tier — you take the loom off the boss. One projectile serves
	/// the Warp's wall and the Shuttle's tether, because both are just "a line, here, for a
	/// while": the only difference is where the two ends came from.
	/// </summary>
	public class FriendlyThread : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_1";

		private const float Width = 13f;
		private const int Life = 150;

		/// <summary>Half-length of the segment; the direction lives in <c>velocity</c>.</summary>
		private ref float Reach => ref Projectile.ai[0];

		public override void SetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 24;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
		{
			// the Weaver Treads' own line, not the set's: your threads outlast the ones you cut
			if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers
				&& Main.player[Projectile.owner].active
				&& Main.player[Projectile.owner].GetModPlayer<TerrapexPlayer>().weaverTreads)
			{
				Projectile.timeLeft = (int)(Projectile.timeLeft
					* TerrapexPlayer.WeaverThreadLife);
			}
		}

		private void Ends(out Vector2 a, out Vector2 b)
		{
			Vector2 half = Projectile.velocity * Reach;
			a = Projectile.Center - half;
			b = Projectile.Center + half;
		}

		public override void AI()
		{
			Lighting.AddLight(Projectile.Center, 0.16f, 0.42f, 0.40f);
			if (!Main.rand.NextBool(2))
				return;

			Ends(out Vector2 a, out Vector2 b);
			Dust d = Dust.NewDustPerfect(Vector2.Lerp(a, b, Main.rand.NextFloat()),
				DustID.Vortex, Vector2.Zero, 130, default, 0.8f);
			d.noGravity = true;
			d.velocity *= 0.2f;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Ends(out Vector2 a, out Vector2 b);
			float point = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
				a, b, Width, ref point);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Ends(out Vector2 a, out Vector2 b);
			a -= Main.screenPosition;
			b -= Main.screenPosition;

			Texture2D px = TextureAssets.MagicPixel.Value;
			Rectangle src = new Rectangle(0, 0, 1, 1);
			float rot = (b - a).ToRotation();
			float run = Vector2.Distance(a, b);

			// fades in over six ticks and out over the last twenty, so it never pops
			float fade = Math.Min(1f, (Life - Projectile.timeLeft) / 6f)
				* Math.Min(1f, Projectile.timeLeft / 20f);
			float beat = 1f + 0.2f * (float)Math.Sin(Projectile.timeLeft * 0.2f);

			Main.EntitySpriteDraw(px, a, src, new Color(24, 150, 145, 0) * (0.55f * fade), rot,
				new Vector2(0f, 0.5f), new Vector2(run, 10f * beat), SpriteEffects.None, 0);
			Main.EntitySpriteDraw(px, a, src, new Color(120, 240, 220, 0) * (0.85f * fade), rot,
				new Vector2(0f, 0.5f), new Vector2(run, 3.5f), SpriteEffects.None, 0);
			Main.EntitySpriteDraw(px, a, src, new Color(216, 255, 247, 0) * fade, rot,
				new Vector2(0f, 0.5f), new Vector2(run, 1.4f), SpriteEffects.None, 0);
			return false;
		}

		/// <summary>Strings one between two world points.</summary>
		public static void Between(Terraria.DataStructures.IEntitySource source, Vector2 a, Vector2 b,
			int damage, int owner, int life = Life)
		{
			Vector2 mid = (a + b) * 0.5f;
			Vector2 dir = b - a;
			float half = dir.Length() * 0.5f;
			if (half < 4f)
				return;

			Projectile p = Projectile.NewProjectileDirect(source, mid, Vector2.Normalize(dir),
				ModContent.ProjectileType<FriendlyThread>(), damage, 0f, owner, half);
			p.timeLeft = life;
		}
	}
}
