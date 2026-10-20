using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Particles;
using SFX = Terrapex.Common.Particles.ShellbackFX;

namespace Terrapex.Content.Projectiles.Shellback
{
	/// <summary>
	/// The Old Shellback's bubble. Launched slow, it picks up speed in a straight line - the fan
	/// is laid on where the player stood when it was spat, and nothing in it turns. Pops on walls.
	///
	/// Drawn as a bubble rather than a sprite: a soft body that wobbles out of round (the hitbox
	/// does not - it is a drawn wobble), a dark rim on the lower side, a bright crescent on the
	/// upper one, a glint that slides along the crescent, and pinhead bubbles left behind it.
	/// </summary>
	public class ShellbackBubble : ModProjectile
	{
		private const int AccelDelay = 14;
		private const float MaxSpeed = 8.5f;

		private ref float Timer => ref Projectile.ai[0];

		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.penetrate = 1;
			Projectile.tileCollide = true;
			Projectile.timeLeft = 180;
			Projectile.alpha = 30;
		}

		public override void AI()
		{
			Timer++;
			if (Timer > AccelDelay && Projectile.velocity.Length() < MaxSpeed)
				Projectile.velocity *= 1.035f;
			// a wobble drawn, not flown: the path stays straight
			Projectile.scale = 1f + 0.06f * MathF.Sin(Timer * 0.35f);
			Lighting.AddLight(Projectile.Center, 0.10f, 0.28f, 0.32f);

			// pinhead bubbles shed off the back
			if ((int)Timer % 5 == 0)
			{
				Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
				ParticleSystem.Spawn(new SFX.MicroBubble(Projectile.Center + back * 10f + Main.rand.NextVector2Circular(5f, 5f),
					back * 0.6f, Main.rand.NextFloat(0.7f, 1.2f), Main.rand.Next(22, 36)));
			}
		}

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Item54 with { Volume = 0.5f }, Projectile.Center);
			Vector2 c = Projectile.Center;
			// the pop: a thin ring, a flash, droplets thrown out and a few survivors drifting up
			ParticleSystem.Spawn(new Shockwave(c, SFX.SeaL, 0.12f, 0.55f, 14));
			ParticleSystem.Spawn(new Bloom(c, SFX.SeaW, 0.2f, 0.45f, 8));
			SFX.Splash(c, 9, 4f, -MathHelper.PiOver2, MathHelper.TwoPi);
			for (int i = 0; i < 3; i++)
				ParticleSystem.Spawn(new SFX.MicroBubble(c + Main.rand.NextVector2Circular(8f, 8f),
					Main.rand.NextVector2Circular(1.2f, 1.2f), Main.rand.NextFloat(0.8f, 1.3f), Main.rand.Next(20, 32)));
			for (int i = 0; i < 2; i++)
				Dust.NewDustPerfect(c, DustID.Water, Main.rand.NextVector2Circular(2.5f, 2.5f), 80, default, 1f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Vector2 c = Projectile.Center;
			float t = Timer;
			// out of round: wide while short, tall while narrow
			float w = MathF.Sin(t * 0.35f);
			Vector2 squash = new(1f + 0.08f * w, 1f - 0.08f * w);
			float r = 11f;

			// the water it carries with it
			RiftDraw.Bloom(c, RiftDraw.Glow(60, 190, 210, 0.32f), 0.55f);

			// the film: the sprite, see-through
			Color body = Color.Lerp(lightColor, Color.White, 0.5f) * 0.72f;
			Main.EntitySpriteDraw(tex, c - Main.screenPosition, null, body, 0f, tex.Size() / 2f, squash, SpriteEffects.None, 0);

			// dark rim on the lower right, bright crescent on the upper left: light bent through it
			Texture2D px = TextureAssets.MagicPixel.Value;
			for (int i = 0; i <= 8; i++)
			{
				float a = MathHelper.Lerp(-0.2f, MathHelper.PiOver2 + 0.5f, i / 8f);
				Dot(px, c + new Vector2(MathF.Cos(a) * r * squash.X, MathF.Sin(a) * r * squash.Y), SFX.SeaK * 0.8f, 2f);
			}
			for (int i = 0; i <= 7; i++)
			{
				float a = MathHelper.Lerp(MathHelper.Pi + 0.25f, MathHelper.Pi * 1.5f - 0.1f, i / 7f);
				float rr = r - 2.5f;
				Dot(px, c + new Vector2(MathF.Cos(a) * rr * squash.X, MathF.Sin(a) * rr * squash.Y),
					new Color(220, 255, 250, 0) * 0.85f, 2f);
			}

			// the glint slides back and forth along the crescent, and now and then flares
			float ga = MathHelper.Pi * 1.25f + 0.35f * MathF.Sin(t * 0.09f);
			Vector2 glint = c + new Vector2(MathF.Cos(ga) * (r - 4.5f) * squash.X, MathF.Sin(ga) * (r - 4.5f) * squash.Y);
			Dot(px, glint, Color.White, 2f);
			int beat = (int)t % 36;
			if (beat < 8)
			{
				float s = MathF.Sin(beat / 8f * MathHelper.Pi);
				Color star = new Color(255, 255, 255, 0) * (0.8f * s);
				RiftDraw.Line(glint - new Vector2(4f + 3f * s, 0f), glint + new Vector2(4f + 3f * s, 0f), star, 1f);
				RiftDraw.Line(glint - new Vector2(0f, 4f + 3f * s), glint + new Vector2(0f, 4f + 3f * s), star, 1f);
			}
			// a second, small reflection low on the far side
			Dot(px, c + new Vector2(4f * squash.X, 5f * squash.Y), new Color(160, 240, 230, 0) * 0.5f, 2f);
			return false;
		}

		/// <summary>A 2x2 pixel on the 2-px grid, so the bubble keeps the sprites' chunky look.</summary>
		internal static void Dot(Texture2D px, Vector2 world, Color color, float size)
		{
			Vector2 snapped = new(MathF.Floor(world.X / 2f) * 2f, MathF.Floor(world.Y / 2f) * 2f);
			Main.EntitySpriteDraw(px, snapped - Main.screenPosition, new Rectangle(0, 0, 1, 1), color, 0f,
				Vector2.Zero, size, SpriteEffects.None, 0);
		}
	}

	/// <summary>
	/// A cannonball lobbed off the wreck's deck. Its arc was marked for fifty ticks before it left,
	/// with a reticle where it lands; it flies exactly that arc (gravity added before the move, as
	/// <c>OldShellback.LaunchVelocity</c> assumes) and bursts on the reticle, on the ground, or on
	/// whoever is standing in its way.
	/// </summary>
	public class ShellbackCannonball : ModProjectile
	{
		public const float BlastRadius = 56f;
		private const int BlastTicks = 8;

		private ref float Timer => ref Projectile.ai[0];
		private ref float Bursting => ref Projectile.ai[1];

		/// <summary>Client-side: the burst's particles were already thrown here.</summary>
		private bool burstShown;

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 8;
			ProjectileID.Sets.TrailingMode[Type] = 0;
		}

		public override void SetDefaults()
		{
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.timeLeft = 300;
		}

		public override void AI()
		{
			if (Bursting > 0f)
			{
				Projectile.velocity = Vector2.Zero;
				// a client that only heard of the burst through a net update still gets to see it
				ShowBurst();
				return;
			}
			Timer++;
			Projectile.velocity.Y += Content.NPCs.Bosses.OldShellback.CannonGravity;
			Projectile.rotation += Projectile.velocity.X * 0.05f;
			// clear of the boss's own sand before it can strike ground
			Projectile.tileCollide = Timer > 20f;
			if (Timer >= Content.NPCs.Bosses.OldShellback.CannonFlight + 1)
				Burst();
			if (Bursting > 0f)
				return;

			// powder smoke behind it, and embers off the fuse
			Vector2 c = Projectile.Center;
			Smoke s = new(c - Projectile.velocity * 0.5f + Main.rand.NextVector2Circular(3f, 3f),
				-Projectile.velocity * 0.08f + new Vector2(0f, -0.3f), SFX.Lit(SFX.Soot, c) * 0.6f,
				Main.rand.NextFloat(0.12f, 0.18f), Main.rand.Next(26, 36));
			s.Grow = 1.03f;
			ParticleSystem.Spawn(s);
			if ((int)Timer % 2 == 0)
			{
				Vector2 fuse = c + new Vector2(0f, -8f).RotatedBy(Projectile.rotation);
				ParticleSystem.Spawn(new Spark(fuse, -Projectile.velocity * 0.15f + Main.rand.NextVector2Circular(1.2f, 1.2f),
					Main.rand.NextBool() ? SFX.Powder : new Color(255, 230, 150), 0.7f, Main.rand.Next(12, 20), 0.06f));
			}
			Lighting.AddLight(c, 0.35f, 0.2f, 0.08f);
		}

		private void Burst()
		{
			if (Bursting > 0f)
				return;
			Bursting = 1f;
			Projectile.tileCollide = false;
			Projectile.Resize((int)(BlastRadius * 2f), (int)(BlastRadius * 2f));
			Projectile.timeLeft = BlastTicks;
			Projectile.netUpdate = true;
			SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
			ShowBurst();
		}

		private void ShowBurst()
		{
			if (burstShown)
				return;
			burstShown = true;
			Vector2 c = Projectile.Center;
			float ringEnd = BlastRadius * 2f / 64f;
			// the core, the fireball, and a ring that stops exactly at the blast's edge
			ParticleSystem.Spawn(new Bloom(c, new Color(255, 245, 210), 0.5f, ringEnd * 0.9f, 12));
			ParticleSystem.Spawn(new Bloom(c, SFX.Powder, 0.8f, ringEnd * 1.25f, 20));
			ParticleSystem.Spawn(new Shockwave(c, SFX.Powder, 0.2f, ringEnd, 16));
			ParticleSystem.Spawn(new Shockwave(c, new Color(255, 230, 170), 0.1f, ringEnd * 1.4f, 24, 0.35f));
			SFX.PowderSmoke(c, Vector2.Zero, 10, 0f, 1.3f);
			for (int i = 0; i < 10; i++)
			{
				Smoke s = new(c, Main.rand.NextVector2Circular(3.5f, 3.5f) + new Vector2(0f, -0.8f),
					SFX.Lit(SFX.Soot, c) * 0.9f, Main.rand.NextFloat(0.3f, 0.5f), Main.rand.Next(40, 60));
				s.Grow = 1.01f;
				ParticleSystem.Spawn(s);
			}
			// the shell's own pieces: iron casing and the wood of the deck it was packed in
			for (int i = 0; i < 12; i++)
			{
				Vector2 v = Main.rand.NextVector2Circular(6f, 6f) + new Vector2(0f, -3f);
				if (i % 2 == 0)
					ParticleSystem.Spawn(new Chip(c, v, i % 4 == 0 ? SFX.IronD : SFX.IronK, Main.rand.NextFloat(0.8f, 1.2f), Main.rand.Next(40, 60)));
				else
					ParticleSystem.Spawn(new SFX.Splinter(c, v, i % 3 == 0 ? SFX.WoodB : SFX.WoodD, Main.rand.NextFloat(4f, 7f), 2f, Main.rand.Next(40, 60)));
			}
			FX.Sparks(c, SFX.Powder, 14, 9f, 1f, 24, 0.15f);
			FX.Sparks(c, new Color(255, 240, 190), 6, 6f, 0.8f, 16);
			if (!Main.dedServ)
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(c, Main.rand.NextVector2Unit(), 3.5f, 6f, 12, 900f,
					"ShellbackCannon"));
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			Burst();
			return false;
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info) => Burst();

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (Bursting <= 0f)
				return null;
			Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
			return Vector2.DistanceSquared(nearest, Projectile.Center) <= BlastRadius * BlastRadius;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (Bursting > 0f)
			{
				float f = 1f - Projectile.timeLeft / (float)BlastTicks;
				RiftDraw.Bloom(Projectile.Center, RiftDraw.Glow(255, 170, 90, 0.9f * (1f - f)), 0.9f + f * 0.6f);
				return false;
			}
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Color lit = lightColor;
			// a smeared, sooty afterimage: weight, not light
			RiftDraw.Trail(Projectile, f => Color.Lerp(new Color(40, 34, 44), lit, 0.3f) * (0.35f * f), f => 0.6f + 0.4f * f);
			// heat behind the ball
			RiftDraw.Bloom(Projectile.Center - Projectile.velocity * 0.6f, RiftDraw.Glow(255, 140, 70, 0.22f), 0.42f);
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation,
				tex.Size() / 2f, 1f, SpriteEffects.None, 0);
			// the lit fuse, flickering
			float flick = 0.8f + 0.2f * MathF.Sin(Timer * 1.3f) + Main.rand.NextFloat(-0.1f, 0.1f);
			Vector2 fuse = Projectile.Center + new Vector2(0f, -8f).RotatedBy(Projectile.rotation);
			RiftDraw.Bloom(fuse, RiftDraw.Glow(255, 190, 110, 0.55f * flick), 0.2f * flick);
			RiftDraw.Bloom(fuse, RiftDraw.Glow(255, 255, 230, 0.7f * flick), 0.07f);
			return false;
		}
	}

	/// <summary>
	/// The wave the slam sends along the sand. It hugs the ground - stepping up and down a few
	/// tiles - and breaks on a wall, so jumping it is the whole answer.
	///
	/// Drawn as water: a wet sheen left on the sand behind it, the sprite as the body, a crest of
	/// foam blobs churning along the lip, and spray thrown back off the top.
	/// </summary>
	public class ShellbackWave : ModProjectile
	{
		public override void SetStaticDefaults() => Main.projFrames[Type] = 3;

		public override void SetDefaults()
		{
			Projectile.width = 30;
			Projectile.height = 40;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.timeLeft = 75;
		}

		public override void AI()
		{
			// find the sand under it: from a few tiles up to a few tiles down
			int tx = (int)(Projectile.Center.X / 16f);
			int top = (int)((Projectile.Bottom.Y - 48f) / 16f);
			int ground = -1;
			for (int ty = top; ty < top + 8; ty++)
			{
				// a platform at or below the wave's own level carries it; one overhead does not catch it
				bool plank = ty * 16f >= Projectile.Bottom.Y - 8f && NPCs.Bosses.OldShellback.Standable(tx, ty);
				if (WorldGen.InWorld(tx, ty) && (WorldGen.SolidTile(tx, ty) || plank))
				{
					ground = ty;
					break;
				}
			}
			if (ground < 0 || ground == top)
			{
				// a cliff edge or a wall: the wave breaks
				Projectile.Kill();
				return;
			}
			Projectile.position.Y = ground * 16f - Projectile.height;

			Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;
			if (++Projectile.frameCounter >= 5)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
			}
			if (Main.rand.NextBool(3))
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Water,
					Projectile.velocity.X * 0.2f, -2f, 80, default, 1.1f);
			Projectile.alpha = Projectile.timeLeft < 12 ? (12 - Projectile.timeLeft) * 20 : 0;
			Lighting.AddLight(Projectile.Center, 0.08f, 0.22f, 0.26f);

			// spray off the crest, thrown back over the shoulder
			int dir = Projectile.spriteDirection;
			Vector2 crest = new(Projectile.Center.X + dir * Main.rand.NextFloat(-4f, 12f), Projectile.Bottom.Y - 36f);
			Spark spray = new(crest, new Vector2(-dir * Main.rand.NextFloat(0.5f, 2.2f) + Projectile.velocity.X * 0.35f,
				-Main.rand.NextFloat(1.5f, 3.6f)), Main.rand.NextBool(3) ? SFX.SeaW : SFX.SeaL,
				Main.rand.NextFloat(0.6f, 0.9f), Main.rand.Next(16, 26), 0.18f);
			spray.Drag = 0.98f;
			spray.Length = 1.6f;
			ParticleSystem.Spawn(spray);
			if (Main.rand.NextBool(3))
				ParticleSystem.Spawn(new Smoke(crest, new Vector2(-dir * 0.8f, -0.6f), SFX.Lit(SFX.SeaW, crest) * 0.6f,
					Main.rand.NextFloat(0.1f, 0.18f), Main.rand.Next(16, 24)));
			// the sand it passed over stays wet a moment
			if (Projectile.timeLeft % 3 == 0)
			{
				Vector2 at = new(Projectile.Center.X - dir * 10f, Projectile.Bottom.Y + 1f);
				ParticleSystem.Spawn(new SFX.GroundMark(at, 20f, 2f, SFX.SeaK * 0.55f, 50));
				ParticleSystem.Spawn(new SFX.GroundMark(at - new Vector2(0f, 2f), 16f, 1f, SFX.SeaL * 0.5f, 26, true));
			}
		}

		public override void OnKill(int timeLeft)
		{
			// breaking: it throws itself forward and up, and leaves foam
			int dir = Projectile.spriteDirection;
			Vector2 at = new(Projectile.Center.X + dir * 8f, Projectile.Bottom.Y - 20f);
			SFX.Splash(at, timeLeft > 0 ? 14 : 6, 5f, dir > 0 ? -1.1f : -2.04f, 1.4f, 5);
			ParticleSystem.Spawn(new Shockwave(new Vector2(at.X, Projectile.Bottom.Y), SFX.SeaL, 0.1f, 0.8f, 18, 0.3f));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Rectangle frame = tex.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
			SpriteEffects fx = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			float op = Projectile.Opacity;
			int dir = Projectile.spriteDirection;
			Vector2 foot = Projectile.Bottom + new Vector2(0f, 2f);

			// the wet sheen behind it, longest where it has just been
			float age = 75 - Projectile.timeLeft;
			float behind = Math.Min(96f, age * Math.Abs(Projectile.velocity.X) + 8f);
			for (float x = 0f; x < behind; x += 6f)
			{
				float u = x / behind;
				Vector2 a = new(foot.X - dir * (x + 8f), foot.Y - 3f);
				RiftDraw.Line(a, a + new Vector2(-dir * 6f, 0f), RiftDraw.Glow(150, 240, 225, 0.35f * (1f - u) * op), 2f);
			}

			// body glow, then the sprite
			RiftDraw.Bloom(Projectile.Center, RiftDraw.Glow(60, 190, 200, 0.3f * op), 0.6f);
			Color c = Color.Lerp(lightColor, Color.White, 0.4f) * op;
			Main.EntitySpriteDraw(tex, foot - Main.screenPosition, frame, c, 0f,
				new Vector2(frame.Width / 2f, frame.Height), 1f, fx, 0);

			// the foam crest: blobs churning along the lip, leaning into the direction of travel
			Texture2D flare = ModContent.Request<Texture2D>("Terrapex/Content/Projectiles/RiftFlare").Value;
			float t = (float)Main.timeForVisualEffects;
			Color foam = Color.Lerp(lightColor, Color.White, 0.6f);
			for (int k = 0; k < 5; k++)
			{
				float lx = -12f + k * 6f;
				float ly = -34f - (k * 1.2f) + MathF.Sin(t * 0.25f + k * 1.7f) * 2f;
				Vector2 p = new(Projectile.Center.X + dir * lx, foot.Y + ly);
				Main.EntitySpriteDraw(flare, p - Main.screenPosition, null, foam * (0.75f * op), 0f, flare.Size() * 0.5f,
					0.13f + 0.03f * MathF.Sin(t * 0.3f + k), SpriteEffects.None, 0);
			}
			// the hard white lip on the pixel grid
			Texture2D px = TextureAssets.MagicPixel.Value;
			for (int k = 0; k < 6; k++)
			{
				float lx = -10f + k * 5f;
				float ly = -38f - k * 0.8f + MathF.Sin(t * 0.3f + k * 2.1f) * 1.5f;
				ShellbackBubble.Dot(px, new Vector2(Projectile.Center.X + dir * lx, foot.Y + ly),
					new Color(255, 255, 255, 0) * (0.8f * op), 2f);
			}
			return false;
		}
	}
}
