using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Particles;

namespace Terrapex.Content.Projectiles.Scarab
{
	/// <summary>
	/// A column of sand driven up out of the floor. For <see cref="Tell"/> ticks it is only a crack
	/// that widens and sheds grit, with the column it will become outlined above it; then it rises,
	/// stands, and sinks. Harmless until the rise, like every line hazard in this mod.
	///
	/// Spawned with its centre on the floor (see <see cref="FindGround"/>); the hitbox is computed
	/// from that point upward. SandPillar.png is two 36x192 frames of churning sand, stacked.
	/// </summary>
	public class SandPillar : ModProjectile
	{
		public const int Tell = 50;
		public const int Rise = 8;
		public const int Hold = 26;
		public const int Sink = 14;
		public const float Height = 192f;
		public const float Width = 36f;

		private static readonly Color Sand = new(240, 200, 120);

		private ref float Timer => ref Projectile.ai[0];

		protected virtual int TellTicks => Tell;
		protected virtual float ColumnHeight => Height;
		protected virtual float ColumnWidth => Width;

		public override void SetStaticDefaults() => Main.projFrames[Type] = 2;

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Tell + Rise + Hold + Sink;
		}

		/// <summary>
		/// The floor under (or around) a point: the top of the first solid tile below it, or - when
		/// the point is already buried - of the surface above it. Falls back to the point itself.
		/// </summary>
		public static Vector2 FindGround(Vector2 from, int maxTiles = 40)
		{
			int x = (int)(from.X / 16f);
			int y = (int)(from.Y / 16f);
			if (Solid(x, y))
			{
				for (int up = 0; up < 15 && Solid(x, y); up++)
					y--;
				return new Vector2(from.X, (y + 1) * 16f);
			}
			for (int k = 0; k < maxTiles; k++)
			{
				if (Solid(x, y + k))
					return new Vector2(from.X, (y + k) * 16f);
			}
			return from;
		}

		private static bool Solid(int x, int y)
		{
			if (!WorldGen.InWorld(x, y, 2))
				return false;
			Tile t = Main.tile[x, y];
			return t.HasTile && !t.IsActuated && (Main.tileSolid[t.TileType] || Main.tileSolidTop[t.TileType]);
		}

		/// <summary>0 before the rise, 1 standing, back to 0 as it sinks.</summary>
		protected float Up
		{
			get
			{
				float t = Timer - TellTicks;
				if (t <= 0f)
					return 0f;
				if (t < Rise)
					return t / Rise;
				if (t < Rise + Hold)
					return 1f;
				return Math.Max(0f, 1f - (t - Rise - Hold) / Sink);
			}
		}

		public override void AI()
		{
			Timer++;
			Projectile.velocity = Vector2.Zero;
			if (++Projectile.frameCounter >= 4)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
			}

			Vector2 floor = Projectile.Center;
			if (Timer < TellTicks)
			{
				// grit spitting out of the crack, thicker as it comes
				if (Main.rand.NextFloat() < (0.25f + Timer / TellTicks) * 0.5f)
				{
					Dust d = Dust.NewDustDirect(floor - new Vector2(ColumnWidth * 0.5f, 4f), (int)ColumnWidth, 4, DustID.Sand,
						0f, -Main.rand.NextFloat(1f, 3f), 80, default, 0.9f);
					d.noGravity = false;
				}
				Lighting.AddLight(floor - new Vector2(0f, 6f), 0.35f * Timer / TellTicks, 0.22f * Timer / TellTicks, 0.06f * Timer / TellTicks);
			}
			else if (Timer == TellTicks)
			{
				SoundEngine.PlaySound(SoundID.Item69 with { Volume = 0.6f, Pitch = 0.2f }, floor);
				for (int i = 0; i < 8; i++)
					Dust.NewDustDirect(floor - new Vector2(ColumnWidth * 0.5f, ColumnHeight * 0.5f), (int)ColumnWidth,
						(int)(ColumnHeight * 0.5f), DustID.Sand, 0f, -4f, 60, default, 1.3f).noGravity = true;
			}
			Lighting.AddLight(floor - new Vector2(0f, ColumnHeight * 0.5f * Up), 0.4f * Up, 0.3f * Up, 0.1f * Up);

			if (!Main.dedServ)
				Effects(floor);
		}

		/// <summary>
		/// Everything the pillar does that is only for the eye, run on every client. The boss's pillar
		/// gets the whole tell - a crack that runs out across the floor, sand pouring into it, heat
		/// rising out of it. Both pillars get the eruption, the sand streaming off the column's sides
		/// while it stands and the cascade as it sinks, scaled to the column.
		/// </summary>
		private void Effects(Vector2 floor)
		{
			float size = ColumnWidth / Width;
			float half = ColumnWidth * 0.5f;
			float t = Timer - TellTicks;

			if (TellTicks > 1 && Timer < TellTicks)
			{
				float f = Timer / TellTicks;
				// the crack: it runs to full length over the tell, then fades under the column
				if (Timer == 1f)
					ScarabFX.FloorCrack(floor, half * 1.7f, TellTicks * 2, 1.6f, SunPalette.Orange);
				if (Timer == (int)(TellTicks * 0.45f))
					ScarabFX.FloorCrack(floor, half * 1.1f, TellTicks + 10, 1f, SunPalette.Pale);

				// sand sliding off the floor either side and pouring into the crack
				if (Main.rand.NextFloat() < 0.4f + 0.9f * f)
				{
					float side = Main.rand.NextBool() ? 1f : -1f;
					Vector2 from = floor + new Vector2(side * Main.rand.NextFloat(half * 0.6f, half * 1.6f), -Main.rand.NextFloat(2f, 7f));
					ParticleSystem.Spawn(new SandGrain(from, new Vector2(-side * Main.rand.NextFloat(0.6f, 1.4f), -0.4f),
						Main.rand.NextBool() ? SunPalette.Sand : SunPalette.SandDark, Main.rand.NextFloat(0.8f, 1.1f), 30) { Gravity = 0.12f });
				}
				// grit spat back up as the floor gives
				if (Main.rand.NextFloat() < 0.35f * f)
					ParticleSystem.Spawn(new SandGrain(floor + new Vector2(Main.rand.NextFloat(-half, half), -3f),
						new Vector2(Main.rand.NextFloat(-0.8f, 0.8f), -Main.rand.NextFloat(1.5f, 3.5f)), SunPalette.Sand, 1f, 40));
				// heat out of the crack
				if (Main.rand.NextFloat() < 0.15f + 0.4f * f)
					ParticleSystem.Spawn(new SunEmber(floor + new Vector2(Main.rand.NextFloat(-half, half), -2f),
						new Vector2(0f, -Main.rand.NextFloat(0.6f, 1.6f)), Main.rand.NextFloat(0.6f, 0.9f), Main.rand.Next(30, 50), 0.35f));
				return;
			}

			if (Timer == TellTicks)
			{
				// the eruption: a ring along the floor, a fountain of sand, chips and a flash from below
				FX.GroundSlam(floor, SunPalette.Gold, SunPalette.Sand * 0.8f, 0.8f * size);
				ParticleSystem.Spawn(new Bloom(floor - new Vector2(0f, 8f), SunPalette.Orange, 0.5f * size, 1.6f * size, 16));
				for (int i = 0; i < 10 * size; i++)
					ParticleSystem.Spawn(new Smoke(floor + new Vector2(Main.rand.NextFloat(-half, half), -Main.rand.NextFloat(0f, 30f)),
						new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), -Main.rand.NextFloat(3f, 9f)) * size,
						SunPalette.Sand * 0.75f, Main.rand.NextFloat(0.3f, 0.5f) * size, Main.rand.Next(30, 46)));
				for (int i = 0; i < 8 * size; i++)
					ParticleSystem.Spawn(new Chip(floor - new Vector2(Main.rand.NextFloat(-half, half), 4f),
						new Vector2(Main.rand.NextFloat(-4f, 4f), -Main.rand.NextFloat(3f, 8f)) * size,
						Main.rand.NextBool() ? new Color(214, 184, 138) : new Color(168, 134, 96), Main.rand.NextFloat(1f, 1.5f), 45));
				ScarabFX.SandKick(floor, size, (int)(12 * size), 0);
				return;
			}

			if (t > 0f && t < Rise + Hold)
			{
				// sand streaming off both sides of the standing column
				float top = ColumnHeight * Up;
				for (int k = 0; k < 2; k++)
				{
					if (!Main.rand.NextBool(k == 0 ? 1 : 2))
						continue;
					float side = Main.rand.NextBool() ? 1f : -1f;
					Vector2 from = floor + new Vector2(side * half, -Main.rand.NextFloat(top * 0.15f, top));
					ParticleSystem.Spawn(new SandGrain(from, new Vector2(side * Main.rand.NextFloat(0.2f, 1.1f), 0f),
						Main.rand.NextBool(3) ? SunPalette.SandShade : SunPalette.Sand, Main.rand.NextFloat(0.8f, 1.2f), 50));
				}
				// the crest boils
				if (Main.rand.NextBool(3))
					ParticleSystem.Spawn(new Smoke(floor + new Vector2(Main.rand.NextFloat(-half, half) * 0.8f, -top),
						new Vector2(Main.rand.NextFloat(-1f, 1f), -Main.rand.NextFloat(0.5f, 1.5f)),
						SunPalette.Sand * 0.6f, Main.rand.NextFloat(0.2f, 0.32f) * size, 30));
				return;
			}

			if (t >= Rise + Hold)
			{
				// it collapses into a cascade: grains rain from the sinking crest, dust rolls out at the base
				float top = ColumnHeight * Up;
				for (int k = 0; k < 3; k++)
					ParticleSystem.Spawn(new SandGrain(floor + new Vector2(Main.rand.NextFloat(-half, half) * 1.2f, -top * Main.rand.NextFloat(0.6f, 1f)),
						new Vector2(Main.rand.NextFloat(-1.6f, 1.6f), Main.rand.NextFloat(-1f, 0.5f)),
						Main.rand.NextBool() ? SunPalette.Sand : SunPalette.SandDark, Main.rand.NextFloat(0.9f, 1.3f), 50));
				if ((int)t % 2 == 0)
				{
					float dir = Main.rand.NextBool() ? 1f : -1f;
					ParticleSystem.Spawn(new Smoke(floor + new Vector2(dir * half * 0.6f, -6f),
						new Vector2(dir * Main.rand.NextFloat(1.5f, 3f), -Main.rand.NextFloat(0.2f, 0.8f)) * size,
						SunPalette.Sand * 0.6f, Main.rand.NextFloat(0.25f, 0.4f) * size, Main.rand.Next(30, 44)));
				}
			}
		}

		/// <summary>The column as it stands this tick, from the floor up.</summary>
		protected Rectangle Column()
		{
			float h = ColumnHeight * Up;
			Vector2 floor = Projectile.Center;
			return new Rectangle((int)(floor.X - ColumnWidth * 0.5f), (int)(floor.Y - h), (int)ColumnWidth, (int)h);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
			=> Up > 0.5f && Column().Intersects(targetHitbox);

		public override bool PreDraw(ref Color lightColor)
		{
			Vector2 floor = Projectile.Center;
			if (Timer < TellTicks)
			{
				// the tell: a crack across the floor that widens and brightens, and the outline of the
				// column it will become, so the player sees exactly how far up it reaches
				// (the running crack itself is a particle - see Effects)
				float f = Timer / TellTicks;
				float half = ColumnWidth * (0.3f + 0.7f * f);
				SpriteBatch sb = Main.spriteBatch;
				float time = Main.GlobalTimeWrappedHourly;

				// light from below: a flat glow on the floor, swelling and flickering as the floor gives
				float flicker = 0.85f + 0.15f * (float)Math.Sin(time * 22f + Projectile.identity);
				SunDraw.Flare(sb, floor, SunPalette.Glow(SunPalette.Orange, (0.2f + 0.5f * f) * flicker),
					new Vector2(ColumnWidth * (1.6f + 1.4f * f), 10f + 10f * f), 0f);
				// the slit the column comes out of - as wide and as bright as the old line, so no less readable
				RiftDraw.Line(floor - new Vector2(half, 1f), floor + new Vector2(half, -1f), Sand * (0.4f + 0.6f * f), 2f + 2f * f);
				SunDraw.Line(sb, floor - new Vector2(half * 0.8f, 1f), floor + new Vector2(half * 0.8f, -1f),
					SunPalette.Glow(SunPalette.Pale, 0.3f + 0.6f * f), 1f + f);

				// the column it will become: a faint body of heat with lit edges, the full height,
				// and motes rising through it - exactly how far up it reaches, from the first tick
				Vector2 top = floor - new Vector2(0f, ColumnHeight);
				SunDraw.Line(sb, floor, top, SunPalette.Glow(SunPalette.Sand, 0.03f + 0.07f * f), ColumnWidth);
				foreach (float side in new[] { -1f, 1f })
				{
					Vector2 a = floor + new Vector2(side * ColumnWidth * 0.5f, -4f);
					Vector2 b = top + new Vector2(side * ColumnWidth * 0.5f, 0f);
					SunDraw.Line(sb, a, b, SunPalette.Glow(SunPalette.Sand, 0.1f + 0.3f * f), 1.5f);
				}
				SunDraw.Line(sb, top - new Vector2(ColumnWidth * 0.5f, 0f), top + new Vector2(ColumnWidth * 0.5f + 1f, 0f),
					SunPalette.Glow(SunPalette.Sand, 0.1f + 0.3f * f), 1.5f);
				for (int k = 0; k < 4; k++)
				{
					float s = (time * 0.7f + k / 4f + Projectile.identity * 0.17f) % 1f;
					float x = (float)Math.Sin(s * 9f + k * 2f) * ColumnWidth * 0.3f;
					SunDraw.Flare(sb, floor + new Vector2(x, -s * ColumnHeight),
						SunPalette.Glow(SunPalette.Gold, (0.15f + 0.4f * f) * (float)Math.Sin(s * MathHelper.Pi)), new Vector2(7f), 0f);
				}
				return false;
			}

			Texture2D tex = TextureAssets.Projectile[ModContent.ProjectileType<SandPillar>()].Value;
			Rectangle frame = tex.Frame(1, 2, 0, Projectile.frame);
			float up = Up;
			if (up <= 0f)
				return false;
			Vector2 scale = new(ColumnWidth / frame.Width, ColumnHeight * up / frame.Height);
			SpriteBatch batch = Main.spriteBatch;
			// heat behind the column and at its foot, so it reads as driven up by something hot below
			SunDraw.Flare(batch, floor - new Vector2(0f, ColumnHeight * up * 0.5f), SunPalette.Glow(SunPalette.Orange, 0.18f * up),
				new Vector2(ColumnWidth * 2.6f, ColumnHeight * up * 1.1f), 0f);
			Main.EntitySpriteDraw(tex, floor - Main.screenPosition, frame, Color.Lerp(lightColor, Color.White, 0.5f),
				0f, new Vector2(frame.Width * 0.5f, frame.Height), scale, SpriteEffects.None, 0);
			SunDraw.Flare(batch, floor, SunPalette.Glow(SunPalette.Gold, 0.45f * up), new Vector2(ColumnWidth * 2.4f, 12f), 0f);
			SunDraw.Flare(batch, floor - new Vector2(0f, ColumnHeight * up), SunPalette.Glow(SunPalette.Pale, 0.3f * up),
				new Vector2(ColumnWidth * 1.8f, 14f), 0f);
			return false;
		}
	}

	/// <summary>
	/// The Sun Scepter's pillar: the Sultan's move in the player's hands. It does not need a tell -
	/// it is the player's - so it rises at once, shorter and narrower than the boss's.
	/// </summary>
	public class ScepterPillar : SandPillar
	{
		public override string Texture => "Terrapex/Content/Projectiles/Scarab/SandPillar";

		protected override int TellTicks => 1;
		protected override float ColumnHeight => 128f;
		protected override float ColumnWidth => 28f;

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.timeLeft = 1 + Rise + Hold + Sink;
		}
	}
}
