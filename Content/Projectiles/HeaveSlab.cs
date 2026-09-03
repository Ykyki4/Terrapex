using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// One plate of the Heave's Upthrust: a slab of floor driven up out of the ground.
	///
	/// It is a projectile and not a tile on purpose — see EVENT_BREACH.md. Changing the world
	/// from a boss attack means syncing tiles and leaving the arena scarred once the fight is
	/// over, and a projectile answers the same requirement for a tenth of the cost.
	///
	/// The shape of it is the mod's readability rule: <see cref="Crack"/> ticks of a widening
	/// split drawn on the floor, with no hitbox, before a single pixel of stone moves. A wall
	/// that simply appeared through the player would be the same unfairness as a thread with no
	/// tightening.
	/// </summary>
	public class HeaveSlab : ModProjectile
	{
		/// <summary>Split drawn on the floor. Harmless, and the only warning the player gets.</summary>
		public const int Crack = 44;
		/// <summary>How long the stone takes to come up.</summary>
		public const int Rise = 30;
		/// <summary>How long it stands before it drops again — ignored once it is permanent.</summary>
		public const int Stand = 90;
		private const int Sink = 26;

		public const int SlabWidth = 32;
		public const int SlabHeight = 96;

		/// <summary>The Slabmaul raises the same stone, only it is the player's.</summary>
		protected virtual bool PlayerOwned => false;

		private ref float Timer => ref Projectile.ai[0];
		/// <summary>Phase two: the plates stay up and the arena closes in from below.</summary>
		private ref float Permanent => ref Projectile.ai[1];
		private ref float GroundY => ref Projectile.ai[2];

		/// <summary>Standing far enough out of the ground for the Slabmaul to break it.</summary>
		public bool Standing => Risen > 0.4f;

		/// <summary>Where the stone actually is, which is not where the projectile sits.</summary>
		public Vector2 Face => new Vector2(Projectile.Center.X, GroundY - SlabHeight * Risen * 0.6f);

		/// <summary>0 while it is still a crack, 1 once the slab is all the way out.</summary>
		private float Risen => MathHelper.Clamp((Timer - Crack) / Rise, 0f, 1f)
			* (1f - MathHelper.Clamp((Timer - Crack - Rise - Stand) / (float)Sink, 0f, 1f));

		public override void SetDefaults()
		{
			Projectile.width = SlabWidth;
			Projectile.height = SlabHeight;
			Projectile.hostile = !PlayerOwned;
			Projectile.friendly = PlayerOwned;
			if (PlayerOwned)
				Projectile.DamageType = DamageClass.Melee;
			Projectile.aiStyle = -1;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 60 * 60 * 10;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 30;
		}

		public override void AI()
		{
			if (Timer == 0f)
			{
				// find the floor once and hang the whole attack off it, so a slab raised on a
				// slope or over a pit still comes out of something solid
				int tx = (int)(Projectile.Center.X / 16f);
				int ty = (int)(Projectile.Bottom.Y / 16f);
				GroundY = Projectile.Bottom.Y;
				for (int i = -3; i < 24; i++)
				{
					if (!WorldGen.InWorld(tx, ty + i, 2))
						break;
					Tile tile = Main.tile[tx, ty + i];
					if (tile.HasTile && Main.tileSolid[tile.TileType])
					{
						GroundY = (ty + i) * 16f;
						break;
					}
				}
				Projectile.Bottom = new Vector2(Projectile.Center.X, GroundY);
				SoundEngine.PlaySound(SoundID.Item62 with { Pitch = -0.7f, Volume = 0.5f }, Projectile.Center);
			}

			Timer++;

			if (Timer == Crack)
			{
				SoundEngine.PlaySound(SoundID.Item70 with { Pitch = -0.5f }, Projectile.Center);
				for (int i = 0; i < 18; i++)
				{
					Dust d = Dust.NewDustPerfect(new Vector2(Projectile.Center.X, GroundY),
						DustID.Stone, new Vector2(Main.rand.NextFloat(-3f, 3f), -Main.rand.NextFloat(1f, 5f)),
						90, default, 1.3f);
					d.velocity.X *= 1.2f;
				}
			}

			if (Timer < Crack)
			{
				// the split itself: dust climbing out of the floor along the slab's width
				if (Main.rand.NextBool(2))
				{
					Dust d = Dust.NewDustPerfect(
						new Vector2(Projectile.Center.X + Main.rand.NextFloat(-SlabWidth * 0.5f, SlabWidth * 0.5f),
							GroundY - 2f),
						DustID.Stone, new Vector2(0f, -Main.rand.NextFloat(0.4f, 1.8f)), 120, default, 0.9f);
					d.noGravity = true;
				}
				return;
			}

			float risen = Risen;
			Lighting.AddLight(new Vector2(Projectile.Center.X, GroundY - SlabHeight * risen * 0.5f),
				0.30f * risen, 0.10f * risen, 0.38f * risen);

			if (risen >= 1f && Main.rand.NextBool(9))
			{
				Dust d = Dust.NewDustPerfect(
					new Vector2(Projectile.Center.X + Main.rand.NextFloat(-14f, 14f),
						GroundY - Main.rand.NextFloat(0f, SlabHeight)),
					DustID.PurpleTorch, new Vector2(0f, -Main.rand.NextFloat(0.3f, 1.1f)), 130, default, 0.85f);
				d.noGravity = true;
			}

			if (Permanent != 0f)
				return;

			if (Timer >= Crack + Rise + Stand + Sink)
				Projectile.Kill();
		}

		/// <summary>
		/// Only the part actually out of the ground can hit anything. The projectile keeps its
		/// final pose the whole time and the hitbox is narrowed here instead of by moving it,
		/// which is the same trick the minions use, and it keeps the draw and the damage from
		/// drifting apart.
		/// </summary>
		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			float risen = Risen;
			if (risen <= 0.02f)
				return false;

			int h = (int)(SlabHeight * risen);
			Rectangle box = new Rectangle((int)(Projectile.Center.X - SlabWidth * 0.5f),
				(int)(GroundY - h), SlabWidth, h);
			return box.Intersects(targetHitbox);
		}

		public override bool? CanDamage() => Timer >= Crack ? null : (bool?)false;

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			float risen = Risen;

			if (risen > 0.005f)
			{
				// clipped from the bottom of the sheet, so the stone reads as coming out of the
				// floor instead of as a rectangle sliding upward past it
				int h = Math.Max(1, (int)(tex.Height * risen));
				Rectangle src = new Rectangle(0, tex.Height - h, tex.Width, h);
				Vector2 pos = new Vector2(Projectile.Center.X, GroundY) - Main.screenPosition;
				Main.EntitySpriteDraw(tex, pos, src, lightColor, 0f,
					new Vector2(tex.Width * 0.5f, h), 1f, SpriteEffects.None, 0);
			}

			if (Timer < Crack)
			{
				// the crack: a bar on the floor that brightens and widens as the stone comes
				float t = Timer / Crack;
				Texture2D pixel = TextureAssets.MagicPixel.Value;
				Rectangle one = new Rectangle(0, 0, 1, 1);
				Vector2 spot = new Vector2(Projectile.Center.X, GroundY) - Main.screenPosition;
				Main.EntitySpriteDraw(pixel, spot, one,
					new Color(255, 150, 230) * (0.25f + 0.55f * t), 0f, new Vector2(0.5f, 0.5f),
					new Vector2(SlabWidth * (0.35f + 0.65f * t), 2f + 2f * t), SpriteEffects.None, 0);
			}

			return false;
		}

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.6f }, Projectile.Center);
			for (int i = 0; i < 12; i++)
			{
				Dust.NewDustPerfect(
					new Vector2(Projectile.Center.X, GroundY - Main.rand.NextFloat(0f, SlabHeight)),
					DustID.Stone, Main.rand.NextVector2Circular(2.4f, 2.4f), 110, default, 1.1f);
			}
		}
	}
}
