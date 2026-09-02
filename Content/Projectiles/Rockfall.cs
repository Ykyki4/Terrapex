using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// A stone dropped by the Dormant Eye's rockfall. It hangs in the air for
	/// <see cref="Telegraph"/> ticks with no hitbox while a marker shrinks onto the
	/// spot it will land, then falls. The mod's readability rule is that nothing hurts
	/// the player before it has announced itself.
	/// </summary>
	public class Rockfall : ModProjectile
	{
		public const int Telegraph = 40;

		/// <summary>The Rockfall Staff drops the same stone, only it is the player's.</summary>
		protected virtual bool PlayerOwned => false;

		private ref float Timer => ref Projectile.ai[0];
		private ref float GroundY => ref Projectile.ai[1];

		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.hostile = !PlayerOwned;
			Projectile.friendly = PlayerOwned;
			if (PlayerOwned)
				Projectile.DamageType = DamageClass.Magic;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.aiStyle = -1;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 600;
		}

		public override void AI()
		{
			if (Timer == 0f)
			{
				// find the floor under the spawn once, so the marker sits on something
				int tx = (int)(Projectile.Center.X / 16f);
				int ty = (int)(Projectile.Center.Y / 16f);
				GroundY = Projectile.Center.Y + 700f;
				for (int i = 0; i < 60; i++)
				{
					int y = ty + i;
					if (!WorldGen.InWorld(tx, y, 2))
						break;
					Tile tile = Main.tile[tx, y];
					if (tile.HasTile && Main.tileSolid[tile.TileType])
					{
						GroundY = y * 16f;
						break;
					}
				}
			}

			Timer++;

			if (Timer < Telegraph)
			{
				// held in place, harmless, while the marker counts it down
				Projectile.velocity = Vector2.Zero;
				if (Main.rand.NextBool(5))
				{
					Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
						DustID.Stone, 0f, 0f, 140, default, 0.8f);
					d.noGravity = true;
					d.velocity *= 0.3f;
				}
				return;
			}

			Projectile.velocity.Y = MathHelper.Min(Projectile.velocity.Y + 0.42f, 15f);
			Projectile.rotation += 0.14f;

			if (Main.rand.NextBool(3))
			{
				Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
					DustID.Stone, 0f, 0f, 120, default, 1f);
				d.velocity *= 0.4f;
			}
		}

		public override bool? CanDamage() => Timer >= Telegraph ? null : (bool?)false;

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			Projectile.Kill();
			return false;
		}

		public override void OnKill(int timeLeft)
		{
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
			for (int i = 0; i < 10; i++)
			{
				Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
					DustID.Stone, Main.rand.NextFloat(-3f, 3f), -Main.rand.NextFloat(1f, 4f), 90, default, 1.2f);
				d.velocity *= 1.2f;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (Timer < Telegraph)
			{
				// the marker: a ring closing on the impact point, plus a thin drop line,
				// so the player reads both WHERE and WHEN
				var ring = ModContent.Request<Texture2D>("Terrapex/Content/Projectiles/RiftRing").Value;
				float t = Timer / Telegraph;
				float scale = MathHelper.Lerp(0.55f, 0.16f, t);
				Vector2 spot = new Vector2(Projectile.Center.X, GroundY);
				Main.EntitySpriteDraw(ring, spot - Main.screenPosition, null,
					Color.White * (0.35f + 0.45f * t), 0f, ring.Size() * 0.5f, scale, SpriteEffects.None, 0);

				var pixel = TextureAssets.MagicPixel.Value;
				var line = new Rectangle(0, 0, 1, 1);
				float h = GroundY - Projectile.Center.Y;
				Main.EntitySpriteDraw(pixel, Projectile.Center - Main.screenPosition, line,
					Color.White * (0.10f + 0.18f * t), 0f, Vector2.Zero, new Vector2(1f, h), SpriteEffects.None, 0);
				return false;
			}

			return true;
		}
	}
}
