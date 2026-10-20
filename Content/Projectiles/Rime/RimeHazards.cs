using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Graphics.CameraModifiers;
using Terrapex.Common;
using Terrapex.Common.Particles;
using Terrapex.Content.NPCs.Bosses;

namespace Terrapex.Content.Projectiles.Rime
{
	/// <summary>
	/// The Rime's slam: a run of icicles erupting along the floor. It hugs the ground and never
	/// rises - the answer is a jump - and it stops dead against a wall. The cracks the boss draws
	/// during the crouch are exactly the line this travels.
	/// </summary>
	public class RimeShockwave : ModProjectile
	{
		public const int Life = 92;

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 6;
			ProjectileID.Sets.TrailingMode[Type] = 0;
		}

		public override void SetDefaults()
		{
			Projectile.width = 34;
			Projectile.height = 44;
			Projectile.aiStyle = -1;
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
		}

		public override void AI()
		{
			Projectile.ai[0]++;
			Projectile.spriteDirection = Projectile.direction = Projectile.velocity.X >= 0f ? 1 : -1;

			// platforms count: a wave slammed onto a platform runs along it instead of dropping through
			bool ground = Collision.SolidCollision(Projectile.BottomLeft + new Vector2(2f, 0f), Projectile.width - 4, 6)
				|| RimeFX.Floor(Projectile.Bottom + new Vector2(-10f, 2f)) || RimeFX.Floor(Projectile.Bottom + new Vector2(10f, 2f));
			if (ground)
			{
				Projectile.velocity.Y = 0f;
				Projectile.position.Y = (float)Math.Floor(Projectile.position.Y);
			}
			else
				Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.4f, 10f);

			Lighting.AddLight(Projectile.Center, 0.2f, 0.4f, 0.55f);
			if (ground && Main.rand.NextBool(4))
				Dust.NewDustPerfect(Projectile.Bottom + new Vector2(Main.rand.NextFloat(-14f, 14f), -2f),
					Main.rand.NextBool(3) ? DustID.Ice : DustID.Snow,
					new Vector2(-Projectile.velocity.X * 0.1f, -Main.rand.NextFloat(0.8f, 2.6f)), 100, default, 1.1f);
			if (ground && !Main.dedServ)
				WaveFX();
		}

		public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
		{
			fallThrough = false;
			return true;
		}

		/// <summary>
		/// The wall of crystal behind the front: every few ticks a spike is raised where the front
		/// just was, stands a moment and shatters. Cosmetic - the hitbox is the front alone.
		/// </summary>
		private void WaveFX()
		{
			int dir = Projectile.direction;
			if ((int)Projectile.ai[0] % 3 == 0)
			{
				Vector2 foot = Projectile.Bottom + new Vector2(-dir * Main.rand.NextFloat(4f, 16f), 2f);
				ParticleSystem.Spawn(new RimeSpike(foot, Main.rand.NextFloat(0.5f, 0.75f), Main.rand.NextFloat(0.45f, 0.85f),
					Main.rand.Next(14, 22), -dir * Main.rand.NextFloat(0.02f, 0.16f), wave: true));
			}
			if (Main.rand.NextBool(2))
				ParticleSystem.Spawn(new Smoke(Projectile.Bottom + new Vector2(Main.rand.NextFloat(-12f, 12f), -6f),
					new Vector2(-dir * Main.rand.NextFloat(0.5f, 2f), -Main.rand.NextFloat(0.4f, 1.6f)),
					RimeFX.Snow * 0.55f, Main.rand.NextFloat(0.18f, 0.3f), Main.rand.Next(26, 40)));
			if (Main.rand.NextBool(3))
				ParticleSystem.Spawn(new Chip(Projectile.Top + new Vector2(dir * 8f, 10f),
					new Vector2(dir * Main.rand.NextFloat(0.5f, 3f), -Main.rand.NextFloat(2f, 5f)),
					Main.rand.NextBool() ? RimeFX.Ice : RimeFX.Snow, Main.rand.NextFloat(0.7f, 1.2f), Main.rand.Next(30, 45)));
			if (Main.rand.NextBool(7))
				ParticleSystem.Spawn(new RimeGlint(Projectile.Top + new Vector2(Main.rand.NextFloat(-8f, 8f), Main.rand.NextFloat(4f, 20f)),
					RimeFX.Pale, 0.7f, 14));
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			// a wall stops it, a floor does not; the grace lets it be born on the ground it was slammed into
			if (Projectile.ai[0] > 5f && Math.Abs(oldVelocity.X - Projectile.velocity.X) > 0.1f)
				return true;
			Projectile.velocity.X = oldVelocity.X;
			Projectile.velocity.Y = 0f;
			return false;
		}

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.5f }, Projectile.Center);
			for (int i = 0; i < 5; i++)
				Dust.NewDustPerfect(Projectile.Bottom, DustID.Ice,
					new Vector2(Main.rand.NextFloat(-2.4f, 2.4f), -Main.rand.NextFloat(0.5f, 3f)), 90, default, 1.1f);
			RimeFX.Shatter(Projectile.Bottom - new Vector2(0f, 22f), 0.7f, 10);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// the front: the tallest spike, with cold light round its edges and a bright foot where
			// it breaks the floor. The crystal it leaves behind is particles (WaveFX).
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Vector2 origin = new(tex.Width / 2f, tex.Height);
			SpriteEffects fx = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			float rise = Math.Min(1f, Projectile.ai[0] / 6f);
			Vector2 foot = Projectile.Bottom - Main.screenPosition;
			Vector2 scale = new(1f, rise);

			RimeFX.Soft(Projectile.Bottom - new Vector2(0f, 4f), RimeFX.Glow(RimeFX.Frost, 0.45f), 0.9f, 0.35f);
			Color rim = RimeFX.Glow(RimeFX.Frost, 0.35f);
			for (int i = 0; i < 4; i++)
				Main.EntitySpriteDraw(tex, foot + (MathHelper.PiOver2 * i).ToRotationVector2() * 2f, null, rim, 0f, origin, scale, fx);
			float flash = 1f - Math.Min(1f, Projectile.ai[0] / 8f);
			Main.EntitySpriteDraw(tex, foot, null, Color.Lerp(lightColor, Color.White, 0.3f + 0.6f * flash),
				0f, origin, scale, fx);
			// a sheen running up the front edge
			float sheen = (Main.GlobalTimeWrappedHourly * 3f + Projectile.identity * 0.37f) % 1f;
			RimeFX.Star(Projectile.Bottom + new Vector2(Projectile.direction * 6f, -tex.Height * rise * sheen),
				RimeFX.Glow(RimeFX.Pale, 0.5f * (float)Math.Sin(sheen * MathHelper.Pi)), 0.6f);
			return false;
		}
	}

	/// <summary>
	/// The block the Rime throws. It flies a fixed ballistic arc for <see cref="Flight"/> ticks to
	/// the mark the boss drew - the same formula draws the arc, so the drawn path is the real one -
	/// and shatters there into a straight fan of shards. ai[0] is the shard count, ai[1] the
	/// timer, ai[2] the shards' damage.
	/// </summary>
	public class RimeBoulder : ModProjectile
	{
		public const int Flight = 48;
		public const float Gravity = 0.3f;

		/// <summary>The launch velocity that lands on <paramref name="to"/> after exactly <see cref="Flight"/> ticks.</summary>
		public static Vector2 Launch(Vector2 from, Vector2 to)
			=> new((to.X - from.X) / Flight, (to.Y - from.Y - Gravity * Flight * (Flight + 1) / 2f) / Flight);

		private ref float Timer => ref Projectile.ai[1];

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 8;
			ProjectileID.Sets.TrailingMode[Type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.width = 40;
			Projectile.height = 40;
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Flight + 10;
		}

		public override void AI()
		{
			Timer++;
			if (Timer > Flight)
			{
				Shatter();
				Projectile.Kill();
				return;
			}
			Projectile.velocity.Y += Gravity;
			Projectile.rotation += 0.05f * Math.Sign(Projectile.velocity.X);
			if (Main.rand.NextBool(4))
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Snow,
					0f, 0f, 100, default, 1.1f).noGravity = true;
			Lighting.AddLight(Projectile.Center, 0.15f, 0.3f, 0.45f);
			if (!Main.dedServ)
			{
				// frost vapour boiling off it, and loose snow shed along the arc
				ParticleSystem.Spawn(new Smoke(Projectile.Center + Main.rand.NextVector2Circular(16f, 16f),
					-Projectile.velocity * 0.12f + new Vector2(0f, -0.3f), RimeFX.Pale * 0.3f,
					Main.rand.NextFloat(0.3f, 0.5f), Main.rand.Next(26, 40), glow: true));
				if (Main.rand.NextBool(3))
					ParticleSystem.Spawn(new RimeFlake(Projectile.Center + Main.rand.NextVector2Circular(18f, 18f),
						Projectile.velocity * 0.2f, RimeFX.Snow, Main.rand.NextFloat(0.4f, 0.65f), Main.rand.Next(40, 60))
					{ Gravity = 0.04f, Drag = 0.97f, Sway = 0.03f });
			}
		}

		private void Shatter()
		{
			SoundEngine.PlaySound(SoundID.Item27 with { Pitch = -0.5f, Volume = 1.2f }, Projectile.Center);
			for (int i = 0; i < 12; i++)
				Dust.NewDustPerfect(Projectile.Center, i % 2 == 0 ? DustID.Ice : DustID.Snow,
					Main.rand.NextVector2Circular(6f, 6f), 80, default, 1.4f).noGravity = true;
			if (!Main.dedServ)
			{
				RimeFX.Shatter(Projectile.Center, 1.5f, 24);
				ParticleSystem.Spawn(new RimeRing(Projectile.Center, RimeFX.Pale, 0.4f, 3f, 26) { Delay = 3 });
				for (int i = 0; i < 6; i++)
					ParticleSystem.Spawn(new RimeFlake(Projectile.Center, Main.rand.NextVector2Circular(4f, 4f), RimeFX.Pale,
						Main.rand.NextFloat(0.6f, 0.9f), Main.rand.Next(40, 70), glow: true) { Drag = 0.95f, Gravity = 0.03f });
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Main.rand.NextVector2Unit(),
					4f, 6f, 14, 900f, "RimeBoulder"));
			}
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			int count = Math.Max(3, (int)Projectile.ai[0]);
			for (int i = 0; i < count; i++)
			{
				// an even fan across the upper half, from just above one horizon to the other
				float a = MathHelper.Lerp(-MathHelper.Pi + 0.17f, -0.17f, i / (count - 1f));
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, a.ToRotationVector2() * RimeShard.Launch,
					ModContent.ProjectileType<RimeShard>(), (int)Projectile.ai[2], 2f, Main.myPlayer);
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Vector2 origin = tex.Size() / 2f;
			// afterimages of cold light down the arc it has flown
			for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
			{
				if (Projectile.oldPos[i] == Vector2.Zero)
					continue;
				float t = 1f - i / (float)Projectile.oldPos.Length;
				Vector2 at = Projectile.oldPos[i] + Projectile.Size / 2f;
				Main.EntitySpriteDraw(tex, at - Main.screenPosition, null, RimeFX.Glow(RimeFX.Frost, 0.22f * t),
					Projectile.oldRot[i], origin, 0.7f + 0.3f * t, SpriteEffects.None);
			}
			RimeFX.Soft(Projectile.Center, RimeFX.Glow(RimeFX.Frost, 0.35f), 1.2f);
			Color rim = RimeFX.Glow(RimeFX.Pale, 0.3f);
			for (int i = 0; i < 4; i++)
				Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition + (MathHelper.PiOver2 * i).ToRotationVector2() * 2f,
					null, rim, Projectile.rotation, origin, 1f, SpriteEffects.None);
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.Lerp(lightColor, RimeFX.Pale, 0.25f),
				Projectile.rotation, origin, 1f, SpriteEffects.None);
			// frost creeping over it as it nears the mark
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null,
				RimeFX.Glow(RimeFX.Pale, 0.1f + 0.25f * (Timer / Flight)), Projectile.rotation, origin, 1f, SpriteEffects.None);
			return false;
		}
	}

	/// <summary>
	/// A shard of the thrown block. Straight lines only: it launches slow, speeds up, and never
	/// turns. Harmless for its first few ticks so the fan opens before it can hurt.
	/// </summary>
	public class RimeShard : ModProjectile
	{
		public const float Launch = 4.2f;
		public const float Top = 9f;
		private const int Grace = 6;

		public override void SetDefaults()
		{
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.hostile = true;
			Projectile.penetrate = 1;
			Projectile.tileCollide = true;
			Projectile.timeLeft = 140;
		}

		public override void AI()
		{
			Projectile.ai[0]++;
			if (Projectile.velocity.Length() < Top)
				Projectile.velocity *= 1.035f;
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			Lighting.AddLight(Projectile.Center, 0.15f, 0.3f, 0.45f);
			if (Main.rand.NextBool(8))
				Dust.NewDustPerfect(Projectile.Center, DustID.Ice, Projectile.velocity * 0.1f, 120, default, 0.8f).noGravity = true;
			if (!Main.dedServ && Main.rand.NextBool(4))
				ParticleSystem.Spawn(new Spark(Projectile.Center, -Projectile.velocity * 0.15f + Main.rand.NextVector2Circular(0.6f, 0.6f),
					RimeFX.Frost, 0.6f, 12));
		}

		public override bool CanHitPlayer(Player target) => Projectile.ai[0] > Grace;

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 2; i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.Ice, Main.rand.NextVector2Circular(2f, 2f), 90, default, 0.9f);
			if (Main.dedServ)
				return;
			ParticleSystem.Spawn(new RimeGlint(Projectile.Center, RimeFX.Pale, 0.55f, 12));
			ParticleSystem.Spawn(new RimeBloom(Projectile.Center, RimeFX.Frost, 0.2f, 0.5f, 12));
			for (int i = 0; i < 4; i++)
				ParticleSystem.Spawn(new Chip(Projectile.Center, Main.rand.NextVector2Circular(2.5f, 2.5f) + new Vector2(0f, -1f),
					RimeFX.Ice, Main.rand.NextFloat(0.6f, 1f), Main.rand.Next(25, 40)));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// a short cold streak behind it that lengthens as it speeds up; dim while it is still harmless
			Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitY);
			float speed = Projectile.velocity.Length();
			float armed = Projectile.ai[0] > Grace ? 1f : 0.45f;
			Vector2 tail = Projectile.Center - dir * (6f + speed * 3.2f);
			RimeFX.Taper(Projectile.Center, tail, RimeFX.Glow(RimeFX.Frost, 0.45f * armed), 6f);
			RimeFX.Taper(Projectile.Center, tail, RimeFX.Glow(RimeFX.Pale, 0.7f * armed), 2f);
			RimeFX.Soft(Projectile.Center, RimeFX.Glow(RimeFX.Frost, 0.3f * armed), 0.35f);
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.Lerp(lightColor, Color.White, 0.45f),
				Projectile.rotation, tex.Size() / 2f, 1f, SpriteEffects.None);
			return false;
		}
	}

	/// <summary>
	/// An icicle hung off the ceiling (or the top of the sky). For <see cref="Tell"/> ticks it
	/// shivers and glints while its shadow darkens on the floor below, with no hitbox; then it
	/// drops straight down and shatters on that floor.
	/// </summary>
	public class RimeIcicle : ModProjectile
	{
		public const int Tell = 54;

		private ref float Timer => ref Projectile.ai[0];
		private ref float GroundY => ref Projectile.ai[1];

		public override void SetDefaults()
		{
			Projectile.width = 22;
			Projectile.height = 44;
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 400;
		}

		public override void AI()
		{
			if (Timer == 0f)
				GroundY = RimeColossus.FloorBelow(Projectile.Center, 60);
			Timer++;
			if (Timer <= Tell)
			{
				Projectile.velocity = Vector2.Zero;
				if (Main.rand.NextBool(12))
					Dust.NewDustPerfect(Projectile.Bottom, DustID.Snow, new Vector2(0f, 1f), 120, default, 0.8f);
				if (!Main.dedServ)
					TellFX(Timer / Tell);
				return;
			}
			if (Timer == Tell + 1)
				Projectile.velocity.Y = 3f;
			Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.9f, 22f);
			if (!Main.dedServ && Main.rand.NextBool(2))
				ParticleSystem.Spawn(new Spark(Projectile.Top + new Vector2(Main.rand.NextFloat(-6f, 6f), 0f),
					new Vector2(0f, -1.5f), RimeFX.Frost, 0.6f, 10));
			if (Projectile.Bottom.Y >= GroundY)
				Projectile.Kill();
		}

		/// <summary>The hang: snow sifting off the ceiling round it, meltwater off its tip, a glint growing.</summary>
		private void TellFX(float f)
		{
			if (Main.rand.NextFloat() < 0.04f + 0.14f * f)
				RimeFX.Sift(Projectile.Top - new Vector2(0f, 2f), 16f);
			if ((int)Timer % 18 == 9)
				ParticleSystem.Spawn(new RimeDrip(Projectile.Bottom, 120));
			if (f > 0.55f && (int)Timer % 10 == 0)
				ParticleSystem.Spawn(new RimeGlint(Projectile.Bottom + new Vector2(Main.rand.NextFloat(-4f, 4f), -Main.rand.NextFloat(4f, 28f)),
					RimeFX.Pale, 0.5f, 12));
		}

		public override bool CanHitPlayer(Player target) => Timer > Tell;

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.6f }, Projectile.Center);
			for (int i = 0; i < 5; i++)
				Dust.NewDustPerfect(Projectile.Bottom, DustID.Ice,
					new Vector2(Main.rand.NextFloat(-3f, 3f), -Main.rand.NextFloat(1f, 4f)), 90, default, 1.1f);
			if (Main.dedServ)
				return;
			Vector2 floor = new(Projectile.Center.X, Math.Min(Projectile.Bottom.Y, GroundY));
			RimeFX.Shatter(floor - new Vector2(0f, 8f), 0.8f, 12);
			ParticleSystem.Spawn(new RimeRing(floor, RimeFX.Frost, 0.2f, 1.6f, 22, 0.28f));
			// a stub of the icicle stays on the floor a moment, then breaks too
			for (int i = -1; i <= 1; i += 2)
				ParticleSystem.Spawn(new RimeSpike(floor + new Vector2(i * Main.rand.NextFloat(4f, 10f), 2f),
					Main.rand.NextFloat(0.7f, 1f), Main.rand.NextFloat(0.6f, 1f), Main.rand.Next(30, 50), i * 0.25f));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Vector2 draw = Projectile.Center;
			if (Timer <= Tell)
			{
				float f = Timer / Tell;
				// where it lands: a shadow, and frost spreading out over it - both widen together
				float w = MathHelper.Lerp(12f, 40f, f);
				Vector2 floor = new(Projectile.Center.X, GroundY);
				RimeFX.Soft(floor, new Color(20, 24, 60) * (0.3f + 0.45f * f), w / 64f * 1.3f, 0.22f);
				RimeFX.Soft(floor - new Vector2(0f, 1f), RimeFX.Glow(RimeFX.Frost, 0.18f + 0.4f * f), w / 64f, 0.18f);
				RimeFX.Line(floor - new Vector2(w / 2f, 1f), floor + new Vector2(w / 2f, -1f), RimeFX.Glow(RimeFX.Pale, 0.3f + 0.5f * f), 2f);
				// rime crystals growing at the patch's edge
				for (int k = 0; k < 3; k++)
				{
					float x = (RimeFX.Hash(Projectile.identity, k) - 0.5f) * w;
					float grow = MathHelper.Clamp(f * 1.6f - k * 0.25f, 0f, 1f);
					RimeFX.Crystal(floor + new Vector2(x, 2f), 0.5f, 0.45f * grow, x * 0.02f, 0.3f * f, k == 1);
				}
				// a faint column down to it
				RimeFX.Line(Projectile.Bottom, floor, RimeFX.Glow(RimeFX.Frost, 0.06f + 0.12f * f), 2f);
				// shivering, and a glint on its tip that grows into the drop
				draw.X += (float)Math.Sin(Timer * 1.3f) * f * 2f;
				Vector2 tip = Projectile.Bottom + new Vector2(draw.X - Projectile.Center.X, -2f);
				RimeFX.Soft(tip, RimeFX.Glow(RimeFX.Pale, 0.3f + 0.4f * f), 0.25f + 0.25f * f);
				RimeFX.Star(tip, RimeFX.Glow(RimeFX.Pale, 0.4f + 0.6f * f), 0.5f + 0.7f * f, Main.GlobalTimeWrappedHourly * 1.5f);
			}
			else
			{
				// falling: a cold streak above it
				Vector2 top = Projectile.Top;
				RimeFX.Taper(top, top - Projectile.velocity * 2.2f, RimeFX.Glow(RimeFX.Frost, 0.35f), 12f);
				RimeFX.Taper(top, top - Projectile.velocity * 1.6f, RimeFX.Glow(RimeFX.Pale, 0.4f), 4f);
			}
			Main.EntitySpriteDraw(tex, draw - Main.screenPosition, null, Color.Lerp(lightColor, Color.White, 0.35f), 0f,
				tex.Size() / 2f, 1f, SpriteEffects.None);
			return false;
		}
	}

	/// <summary>
	/// One of the two walls the Rime raises round the player. It rises out of the floor for
	/// <see cref="Tell"/> harmless ticks, then closes inward until the pair is 80 px apart and
	/// breaks. ai[0] is its signed speed, ai[1] is 1 when it carries the door, ai[2] how far the
	/// door sits off the floor. The door is drawn - two lit posts - from the first frame.
	/// </summary>
	public class RimeWall : ModProjectile
	{
		public const int Tell = 40;
		public const int Height = 352;
		public const int DoorHeight = 84;

		/// <summary>How long a wall moving at <paramref name="speed"/> closes before it breaks.</summary>
		public static int LifeFor(float speed) => (int)((RimeColossus.WallStart - 40f) / Math.Abs(speed));

		private ref float Timer => ref Projectile.localAI[0];
		private bool HasDoor => Projectile.ai[1] == 1f;

		public override void SetDefaults()
		{
			Projectile.width = 30;
			Projectile.height = Height;
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 600;
		}

		public override void AI()
		{
			if (Timer == 0f)
				Projectile.timeLeft = Tell + LifeFor(Projectile.ai[0] == 0f ? 1f : Projectile.ai[0]);
			Timer++;
			Projectile.velocity = Timer > Tell ? new Vector2(Projectile.ai[0], 0f) : Vector2.Zero;
			Lighting.AddLight(Projectile.Center, 0.15f, 0.3f, 0.45f);
			if (Timer <= Tell && Main.rand.NextBool(3))
				Dust.NewDustPerfect(new Vector2(Projectile.Center.X + Main.rand.NextFloat(-15f, 15f), Projectile.Bottom.Y - 2f),
					DustID.Snow, new Vector2(0f, -Main.rand.NextFloat(1f, 3f)), 100, default, 1.1f);
			if (!Main.dedServ)
				WallFX();
		}

		/// <summary>Mist at the foot, chips thrown while it rises, glints on its face, embers off the door posts.</summary>
		private void WallFX()
		{
			float rise = Timer <= Tell ? Timer / Tell : 1f;
			float bottom = Projectile.Bottom.Y;
			if (Main.rand.NextBool(2))
				ParticleSystem.Spawn(new Smoke(new Vector2(Projectile.Center.X + Main.rand.NextFloat(-24f, 24f), bottom - 6f),
					new Vector2(Main.rand.NextFloat(-1.2f, 1.2f), -Main.rand.NextFloat(0.1f, 0.6f)), RimeFX.Pale * 0.28f,
					Main.rand.NextFloat(0.35f, 0.6f), Main.rand.Next(40, 60), glow: true));
			if (Timer <= Tell && Main.rand.NextBool(2))
				ParticleSystem.Spawn(new Chip(new Vector2(Projectile.Center.X + Main.rand.NextFloat(-14f, 14f), bottom - 4f),
					new Vector2(Main.rand.NextFloat(-3f, 3f), -Main.rand.NextFloat(2f, 6f)),
					Main.rand.NextBool() ? RimeFX.Snow : RimeFX.Ice, Main.rand.NextFloat(0.8f, 1.4f), Main.rand.Next(30, 50)));
			if (Main.rand.NextBool(10))
			{
				float y = bottom - Main.rand.NextFloat(10f, Height * (0.25f + 0.75f * rise) - 6f);
				if (!HasDoor || y < DoorTop - 6f || y > DoorBottom + 6f)
					ParticleSystem.Spawn(new RimeGlint(new Vector2(Projectile.Center.X + Main.rand.NextFloat(-12f, 12f), y), RimeFX.Pale,
						Main.rand.NextFloat(0.5f, 0.8f), 16));
			}
			if (HasDoor && Main.rand.NextBool(3))
			{
				float side = Main.rand.NextBool() ? -1f : 1f;
				Vector2 at = new(Projectile.Center.X + side * (Projectile.width / 2f + 4f), MathHelper.Lerp(DoorTop, DoorBottom, Main.rand.NextFloat()));
				ParticleSystem.Spawn(new Spark(at, new Vector2(side * Main.rand.NextFloat(0.2f, 0.9f), -Main.rand.NextFloat(0.6f, 1.8f)),
					RimeFX.Warm, Main.rand.NextFloat(0.5f, 0.8f), Main.rand.Next(26, 40), -0.02f) { Drag = 0.97f });
			}
		}

		private float DoorBottom => Projectile.Bottom.Y - Projectile.ai[2];
		private float DoorTop => DoorBottom - DoorHeight;

		public override bool CanHitPlayer(Player target) => Timer > Tell;

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (!HasDoor)
				return projHitbox.Intersects(targetHitbox);
			int doorTop = (int)DoorTop, doorBottom = (int)DoorBottom;
			Rectangle upper = new(projHitbox.X, projHitbox.Y, projHitbox.Width, doorTop - projHitbox.Y);
			Rectangle lower = new(projHitbox.X, doorBottom, projHitbox.Width, projHitbox.Bottom - doorBottom);
			return upper.Intersects(targetHitbox) || (lower.Height > 0 && lower.Intersects(targetHitbox));
		}

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Item27 with { Pitch = -0.3f }, Projectile.Center);
			for (int i = 0; i < 14; i++)
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Ice,
					0f, Main.rand.NextFloat(-2f, 1f), 90, default, 1.2f);
			if (Main.dedServ)
				return;
			// the whole height comes apart, top to bottom
			for (int i = 0; i < 6; i++)
			{
				Vector2 at = new(Projectile.Center.X, Projectile.Bottom.Y - Height * (i + 0.5f) / 6f);
				RimeFX.Shatter(at, 0.9f, 8);
			}
			for (int i = 0; i < 8; i++)
				ParticleSystem.Spawn(new RimeIceBit(new Vector2(Projectile.Center.X, Projectile.Bottom.Y - Main.rand.NextFloat(Height)),
					new Vector2(Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-5f, 0f)), Main.rand.NextFloat(0.7f, 1.1f), 70));
			ParticleSystem.Spawn(new RimeRing(Projectile.Bottom, RimeFX.Frost, 0.4f, 3.2f, 28, 0.25f));
			ParticleSystem.Spawn(new RimeBloom(Projectile.Center, RimeFX.Frost, 2f, 4f, 20, 3f));
			Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Vector2.UnitX, 5f, 6f, 16, 1200f, "RimeWall"));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			float rise = Timer <= Tell ? Timer / Tell : 1f;
			float alpha = Timer <= Tell ? 0.35f + 0.3f * rise : 0.9f;
			float bottom = Projectile.Bottom.Y;
			float top = bottom - Height * (0.25f + 0.75f * rise);
			float left = Projectile.Center.X - tex.Width / 2f;
			Color tint = Color.Lerp(lightColor, Color.White, 0.25f) * alpha;

			// the final volume, faintly filled from the first frame, under everything
			if (Timer <= Tell)
				RimeFX.Line(new Vector2(left, bottom - Height / 2f), new Vector2(left + tex.Width, bottom - Height / 2f),
					RimeFX.Glow(RimeFX.Frost, 0.05f + 0.06f * rise), Height);
			RimeFX.Soft(new Vector2(Projectile.Center.X, bottom), RimeFX.Glow(RimeFX.Frost, 0.35f), 1.1f, 0.3f);

			// tiled up from the floor, skipping the door
			for (float y = bottom - tex.Height; y + tex.Height > top; y -= tex.Height)
			{
				float y0 = Math.Max(y, top);
				float y1 = y + tex.Height;
				if (HasDoor)
				{
					if (y1 > DoorTop && y0 < DoorBottom)
					{
						// split the tile round the door
						DrawSlice(tex, left, y, y0, Math.Min(y1, DoorTop), tint);
						DrawSlice(tex, left, y, Math.Max(y0, DoorBottom), y1, tint);
						continue;
					}
				}
				DrawSlice(tex, left, y, y0, y1, tint);
			}

			DrawIce(left, tex.Width, top, bottom, rise);

			// the outline of the full wall while it rises, so its final height is known at once
			if (Timer <= Tell)
			{
				Color edge = RiftDraw.Glow(150, 225, 255, 0.25f + 0.3f * rise);
				Vector2 tl = new(left, bottom - Height), tr = new(left + tex.Width, bottom - Height);
				RiftDraw.Line(tl, new Vector2(left, bottom), edge, 2f);
				RiftDraw.Line(tr, new Vector2(left + tex.Width, bottom), edge, 2f);
				RiftDraw.Line(tl, tr, edge, 2f);
				RimeFX.Star(tl, RimeFX.Glow(RimeFX.Pale, 0.3f + 0.4f * rise), 0.6f);
				RimeFX.Star(tr, RimeFX.Glow(RimeFX.Pale, 0.3f + 0.4f * rise), 0.6f);
			}

			// the door: two warm lit posts and its sill and lintel, from the very first frame
			if (HasDoor)
			{
				float beat = 0.6f + 0.3f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f);
				Color post = RiftDraw.Glow(255, 200, 110, beat);
				Vector2 a = new(left - 4f, DoorTop), b = new(left + tex.Width + 4f, DoorTop);
				Vector2 c = new(left - 4f, DoorBottom), d = new(left + tex.Width + 4f, DoorBottom);
				// warm air in the opening: the way through is the one warm thing in the fight
				RimeFX.Line(new Vector2(left, (DoorTop + DoorBottom) / 2f), new Vector2(left + tex.Width, (DoorTop + DoorBottom) / 2f),
					RimeFX.Glow(RimeFX.Warm, 0.07f + 0.04f * beat), DoorHeight);
				RimeFX.Soft((a + b + c + d) / 4f, RimeFX.Glow(RimeFX.Warm, 0.18f * beat), 1.5f, 1.4f);
				RiftDraw.Line(a, b, post, 3f);
				RiftDraw.Line(c, d, post, 3f);
				RiftDraw.Line(a, c, post * 0.5f, 2f);
				RiftDraw.Line(b, d, post * 0.5f, 2f);
				RimeFX.Line(a, c, RimeFX.Glow(RimeFX.Warm, 0.25f * beat), 6f);
				RimeFX.Line(b, d, RimeFX.Glow(RimeFX.Warm, 0.25f * beat), 6f);
				foreach (Vector2 p in new[] { a, b, c, d })
				{
					RimeFX.Soft(p, RimeFX.Glow(RimeFX.Warm, 0.5f * beat), 0.35f);
					RimeFX.Star(p, RimeFX.Glow(new Color(255, 236, 190), 0.7f * beat), 0.55f);
				}
				RimeFX.Soft((a + c) / 2f, RimeFX.Glow(RimeFX.Warm, 0.35f * beat), 0.6f, 1.8f);
				RimeFX.Soft((b + d) / 2f, RimeFX.Glow(RimeFX.Warm, 0.35f * beat), 0.6f, 1.8f);
			}
			return false;
		}

		/// <summary>
		/// What makes the tiles read as ice and not as a column of blocks: refraction streaks
		/// sliding up inside it, a facet edge down its length, a lit rim with a sheen running up it
		/// and a bright cap. Every line runs edge to edge across the width, so nothing leaves the body;
		/// all of it skips the door.
		/// </summary>
		private void DrawIce(float left, float width, float top, float bottom, float rise)
		{
			float right = left + width;
			bool Open(float y) => HasDoor && y > DoorTop - 2f && y < DoorBottom + 2f;
			float strength = Timer <= Tell ? 0.4f + 0.4f * rise : 1f;
			float t = Main.GlobalTimeWrappedHourly;

			// refraction streaks: slanted bands drifting slowly up through the body
			for (int k = 0; k < 7; k++)
			{
				float h = RimeFX.Hash(Projectile.identity, k);
				float slope = (RimeFX.Hash(Projectile.identity, k + 40) - 0.5f) * 1.1f;
				float y = bottom - ((h * Height + t * 14f * (0.6f + h)) % Height);
				float y2 = y + slope * width;
				if (Math.Min(y, y2) < top + 4f || Math.Max(y, y2) > bottom - 2f || Open(y) || Open(y2))
					continue;
				RimeFX.Line(new Vector2(left + 2f, y), new Vector2(right - 2f, y2), RimeFX.Glow(Color.White, (0.08f + 0.12f * h) * strength), 1.5f + h * 2f);
			}

			// a facet edge down its length, and the rim on both faces
			float facetX = left + width * 0.32f;
			Color facet = RimeFX.Glow(RimeFX.Ice, 0.12f * strength);
			Color rim = RimeFX.Glow(RimeFX.Pale, 0.32f * strength);
			if (HasDoor)
			{
				RimeFX.Line(new Vector2(facetX, top), new Vector2(facetX, Math.Max(top, DoorTop)), facet, 2f);
				RimeFX.Line(new Vector2(facetX, Math.Max(top, DoorBottom)), new Vector2(facetX, bottom), facet, 2f);
				foreach (float x in new[] { left + 1f, right - 1f })
				{
					RimeFX.Line(new Vector2(x, top), new Vector2(x, Math.Max(top, DoorTop)), rim, 2f);
					RimeFX.Line(new Vector2(x, Math.Max(top, DoorBottom)), new Vector2(x, bottom), rim, 2f);
				}
			}
			else
			{
				RimeFX.Line(new Vector2(facetX, top), new Vector2(facetX, bottom), facet, 2f);
				RimeFX.Line(new Vector2(left + 1f, top), new Vector2(left + 1f, bottom), rim, 2f);
				RimeFX.Line(new Vector2(right - 1f, top), new Vector2(right - 1f, bottom), rim, 2f);
			}

			// a sheen climbing the rim
			float sheenY = bottom - ((t * 90f + Projectile.identity * 31f) % Height);
			if (sheenY > top && !Open(sheenY))
			{
				RimeFX.Soft(new Vector2(left + 1f, sheenY), RimeFX.Glow(RimeFX.Pale, 0.35f * strength), 0.25f, 3f);
				RimeFX.Soft(new Vector2(right - 1f, sheenY), RimeFX.Glow(RimeFX.Pale, 0.35f * strength), 0.25f, 3f);
			}

			// the cap
			RimeFX.Line(new Vector2(left - 1f, top), new Vector2(right + 1f, top), RimeFX.Glow(RimeFX.Pale, 0.6f * strength), 3f);
			RimeFX.Soft(new Vector2((left + right) / 2f, top), RimeFX.Glow(RimeFX.Frost, 0.35f * strength), 0.7f, 0.35f);
		}

		private static void DrawSlice(Texture2D tex, float left, float tileTop, float y0, float y1, Color tint)
		{
			if (y1 <= y0)
				return;
			Rectangle src = new(0, (int)(y0 - tileTop), tex.Width, (int)(y1 - y0));
			Main.EntitySpriteDraw(tex, new Vector2(left, y0) - Main.screenPosition, src, tint, 0f, Vector2.Zero, 1f,
				SpriteEffects.None);
		}
	}

	/// <summary>
	/// The Frost Wisp's trail: a mote of cold that hangs where the wisp passed. It stings a
	/// little and chills - the wisp's whole threat is where it has been, not where it is.
	/// </summary>
	public class FrostMote : ModProjectile
	{
		public const int Life = 150;
		private const int Grace = 12;

		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Life;
		}

		public override void AI()
		{
			Projectile.velocity *= 0.92f;
			Projectile.velocity.Y += 0.005f;
			Projectile.rotation += 0.05f;
			Lighting.AddLight(Projectile.Center, 0.1f, 0.25f, 0.35f);
			if (Main.dedServ)
				return;
			if (Life - Projectile.timeLeft == 1)
				ParticleSystem.Spawn(new RimeBloom(Projectile.Center, RimeFX.Frost, 0.15f, 0.6f, 14));
			if (Main.rand.NextBool(24))
				ParticleSystem.Spawn(new RimeFlake(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), new Vector2(0f, 0.3f),
					RimeFX.Pale, 0.35f, 40, glow: true) { Sway = 0.02f });
		}

		public override bool CanHitPlayer(Player target) => Life - Projectile.timeLeft > Grace && Projectile.timeLeft > 20;

		public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(BuffID.Chilled, 150);

		public override void OnKill(int timeLeft)
		{
			if (Main.dedServ)
				return;
			ParticleSystem.Spawn(new RimeGlint(Projectile.Center, RimeFX.Pale, 0.4f, 12));
			for (int i = 0; i < 3; i++)
				ParticleSystem.Spawn(new RimeFlake(Projectile.Center, Main.rand.NextVector2Circular(1.2f, 1.2f), RimeFX.Pale, 0.3f, 30, glow: true));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float life = Projectile.timeLeft < 30 ? Projectile.timeLeft / 30f : 1f;
			// it gathers over its harmless first ticks, so a fresh mote reads as not-yet
			float born = Math.Min(1f, (Life - Projectile.timeLeft) / (float)Grace);
			float pulse = 0.85f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f + Projectile.identity);
			RimeFX.Soft(Projectile.Center, RimeFX.Glow(RimeFX.Frost, 0.3f * life * born * pulse), 0.45f);
			RimeFX.Flake(Projectile.Center, RimeFX.Glow(RimeFX.Pale, 0.45f * life * born), 0.9f + 0.3f * pulse, -Projectile.rotation * 0.7f);
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White * (0.85f * life * (0.4f + 0.6f * born)),
				Projectile.rotation, tex.Size() / 2f, 0.6f + 0.4f * born, SpriteEffects.None);
			return false;
		}
	}
}
