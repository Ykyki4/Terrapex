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
	/// A ball that rolls by <see cref="BallPhysics"/> - the Scarab Sultan's sun, and the Sand
	/// Roller's ball, which is the same thing small and dull so the mob teaches the boss.
	///
	/// ai[0] is the kind: <see cref="Sun"/>, <see cref="Cracked"/> (phase two: smaller, and it
	/// bursts) or <see cref="Dung"/> (the Sand Roller's). ai[1]/ai[2] hold the ball's true
	/// velocity; the engine's velocity is only the displacement for this tick.
	///
	/// SunBall.png is three 48x48 frames, one per kind, stacked.
	///
	/// The look (all cosmetic, client-side, none of it touches the hitbox or the timing): a sun is
	/// a sprite over a three-layer halo with a corona of rays turning at three rates
	/// (<see cref="SunDraw.Halo"/>), shedding heat motes, scorching the floor it rolls over and
	/// throwing sand on every bounce. The cracked sun adds light leaking through its cracks - wider
	/// as the fuse runs - and a heartbeat that speeds up toward the burst.
	/// </summary>
	public class SunBall : ModProjectile
	{
		public const int Sun = 0, Cracked = 1, Dung = 2;
		public const int Size = 36;

		public const int SunLife = 210;
		public const int DungLife = 240;
		public const int Fade = 24;

		/// <summary>The cracked sun's fuse, and the tell inside it: it flashes and draws its eight spokes.</summary>
		public const int Fuse = 170;
		public const int BurstTell = 40;
		public const int Shards = 8;

		private static readonly Color SunLight = new(255, 196, 84);

		private int Kind => (int)Projectile.ai[0];

		/// <summary>0 at the kick, 1 at the burst - how far the cracked sun's fuse has burnt.</summary>
		private float FuseBurnt => MathHelper.Clamp(1f - Projectile.timeLeft / (float)Fuse, 0f, 1f);

		/// <summary>The cracked sun's heartbeat, 0..1. Its phase lives in localAI[1].</summary>
		private float Pulse => 0.5f + 0.5f * (float)Math.Sin(Projectile.localAI[1]);

		/// <summary>The drawn body is a little larger than the hitbox and sits on the floor.</summary>
		private float Visual => Kind == Sun ? 22f : 17f;
		private Vector2 VisualCenter => Projectile.Bottom - new Vector2(0f, Visual);

		public override void SetStaticDefaults() => Main.projFrames[Type] = 3;

		public override void SetDefaults()
		{
			Projectile.width = Size;
			Projectile.height = Size;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = SunLife;
		}

		/// <summary>The life a ball of this kind is given. Set by whoever spawns it.</summary>
		public static int LifeOf(int kind) => kind == Cracked ? Fuse : kind == Dung ? DungLife : SunLife;

		public override void AI()
		{
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				Projectile.timeLeft = LifeOf(Kind);
				Projectile.frame = Math.Clamp(Kind, 0, 2);
			}

			Vector2 before = new(Projectile.ai[1], Projectile.ai[2]);
			Vector2 vel = before;
			Vector2 pos = Projectile.position;
			Vector2 moved = BallPhysics.Step(ref pos, ref vel, Projectile.width, Projectile.height);
			Projectile.position = pos;
			Projectile.velocity = moved;
			Projectile.ai[1] = vel.X;
			Projectile.ai[2] = vel.Y;

			// it rolls: the turn follows the distance covered, not a fixed spin
			Projectile.rotation += moved.X / (Size * 0.5f);

			if (Kind == Dung)
			{
				if (Main.rand.NextBool(6) && Math.Abs(moved.X) > 1f)
					Dust.NewDustDirect(Projectile.BottomLeft - new Vector2(0f, 6f), Projectile.width, 6, DustID.Sand,
						-moved.X * 0.2f, -0.6f, 80, default, 0.9f);
				if (before.Y > 0f && vel.Y < 0f)
					ScarabFX.SandKick(Projectile.Bottom, 0.6f, 4, 2);
			}
			else
			{
				Lighting.AddLight(Projectile.Center, SunLight.ToVector3() * (Kind == Cracked ? 0.9f : 0.6f));
				if (Main.rand.NextBool(5))
				{
					Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
						Kind == Cracked ? DustID.SolarFlare : DustID.GoldFlame, 0f, -1f, 80, default, 1f);
					d.noGravity = true;
				}
				SunEffects(before, vel, moved);
			}

			if (Kind == Cracked)
			{
				// the heartbeat quickens as the fuse burns: slow and heavy at the kick, a flutter at the end
				float f = FuseBurnt;
				Projectile.localAI[1] += MathHelper.Lerp(0.07f, 0.65f, f * f);
				if (Projectile.timeLeft == BurstTell)
					SoundEngine.PlaySound(SoundID.Item13 with { Pitch = 0.4f }, Projectile.Center);
				if (Projectile.timeLeft <= 2)
				{
					Burst();
					Projectile.Kill();
				}
			}
			else
				Projectile.alpha = Projectile.timeLeft < Fade ? (Fade - Projectile.timeLeft) * 255 / Fade : 0;
		}

		/// <summary>Heat motes, the scorched floor, sand on the bounce, sparks off a wall. Cosmetic only.</summary>
		private void SunEffects(Vector2 before, Vector2 vel, Vector2 moved)
		{
			if (Main.dedServ)
				return;
			float fade = 1f - Projectile.alpha / 255f;
			if (fade <= 0.05f)
				return;
			bool cracked = Kind == Cracked;
			float r = Visual - 2f;
			Vector2 c = VisualCenter;

			// heat rising off it
			if (Main.rand.NextFloat() < (cracked ? 0.7f : 0.5f) * fade)
			{
				Vector2 off = Main.rand.NextVector2Unit() * r * Main.rand.NextFloat(0.5f, 0.9f);
				off.Y = -Math.Abs(off.Y);
				ParticleSystem.Spawn(new SunEmber(c + off, new Vector2(Main.rand.NextFloat(-0.4f, 0.4f) - moved.X * 0.15f,
					-Main.rand.NextFloat(0.5f, 1.3f)), Main.rand.NextFloat(0.7f, 1.1f), Main.rand.Next(36, 60), 0.15f));
			}

			bool rolling = vel.Y == 0f && Math.Abs(moved.X) > 0.4f;
			if (rolling)
			{
				// the floor it has rolled over stays burnt for a moment
				if (Projectile.timeLeft % 3 == 0)
					ParticleSystem.Spawn(new SunScorch(Projectile.Bottom - new Vector2(0f, 1f), (cracked ? 0.8f : 1f) * fade, 50));
				if (Main.rand.NextBool(2))
					ParticleSystem.Spawn(new SandGrain(Projectile.Bottom + new Vector2(-Math.Sign(moved.X) * r * 0.6f, -2f),
						new Vector2(-moved.X * Main.rand.NextFloat(0.2f, 0.5f), -Main.rand.NextFloat(1f, 2.6f)),
						Main.rand.NextBool() ? SunPalette.Sand : SunPalette.SandDark, 1f, 40));
			}

			// a bounce off the floor throws sand and leaves a flash where it struck
			if (before.Y > 0f && vel.Y < 0f)
			{
				float hard = MathHelper.Clamp(before.Y / 8f, 0.4f, 1.3f);
				ScarabFX.SandKick(Projectile.Bottom, hard, (int)(10 * hard), (int)(5 * hard));
				ParticleSystem.Spawn(new Shockwave(Projectile.Bottom, SunPalette.Gold, 0.1f, 1.1f * hard, 18, 0.25f));
				ParticleSystem.Spawn(new SunScorch(Projectile.Bottom - new Vector2(0f, 1f), 1.4f * hard, 60));
				FX.Sparks(Projectile.Bottom, SunPalette.Gold, (int)(6 * hard), 5f * hard, 0.9f, 18, 0.15f,
					MathHelper.Pi * 0.8f, -MathHelper.PiOver2);
			}

			// a wall bounce spits sparks back the way it came
			if (before.X != 0f && vel.X != 0f && Math.Sign(before.X) != Math.Sign(vel.X))
			{
				Vector2 side = Projectile.Center + new Vector2(Math.Sign(before.X) * Projectile.width * 0.5f, 0f);
				FX.Sparks(side, SunPalette.Pale, 8, 6f, 1f, 18, 0.1f, MathHelper.Pi * 0.7f, before.X > 0f ? MathHelper.Pi : 0f);
				ParticleSystem.Spawn(new Bloom(side, SunPalette.Orange, 0.3f, 0.9f, 14));
			}

			// the cracked sun spits from its cracks, more and more as it nears the burst
			if (cracked && Main.rand.NextFloat() < 0.1f + 0.7f * FuseBurnt * FuseBurnt)
			{
				Vector2 dir = Main.rand.NextVector2Unit();
				ParticleSystem.Spawn(new Spark(c + dir * r * 0.8f, dir * Main.rand.NextFloat(2f, 5f), SunPalette.Pale,
					Main.rand.NextFloat(0.6f, 1f), Main.rand.Next(10, 18), 0.08f));
			}
		}

		/// <summary>Eight shards along the eight spokes the tell drew - the same lines, no aim at anyone.</summary>
		private void Burst()
		{
			SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
			for (int i = 0; i < 12; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.SolarFlare, Main.rand.NextVector2Circular(7f, 7f), 60, default, 1.5f);
				d.noGravity = true;
			}
			// the flash, the rings and a ray down each spoke - exactly the spokes, so the burst
			// confirms the tell instead of muddying it
			ScarabFX.Sunburst(Projectile.Center, 1f, Shards, 0f);
			for (int i = 0; i < 10; i++)
				ParticleSystem.Spawn(new Chip(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
					Main.rand.NextVector2Circular(5f, 5f) - new Vector2(0f, 2f),
					Main.rand.NextBool() ? new Color(240, 168, 56) : new Color(192, 106, 42), Main.rand.NextFloat(0.9f, 1.4f), 50));
			if (Projectile.Bottom.Y > 0f)
				ScarabFX.SandKick(Projectile.Bottom, 1f, 8, 6);

			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			for (int i = 0; i < Shards; i++)
			{
				Vector2 dir = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / Shards);
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, dir * 2.6f,
					ModContent.ProjectileType<SunShard>(), Projectile.damage * 3 / 5, 0f, Main.myPlayer);
			}
		}

		public override void OnKill(int timeLeft)
		{
			// a sun snuffed early (the Sultan's death clears them) goes out with a puff, not a pop
			if (Kind == Dung || timeLeft <= 3 || (Kind == Sun && timeLeft <= Fade))
				return;
			ParticleSystem.Spawn(new Bloom(VisualCenter, SunPalette.Orange, 0.6f, 1.6f, 20));
			for (int i = 0; i < 8; i++)
				ParticleSystem.Spawn(new SunEmber(VisualCenter + Main.rand.NextVector2Circular(12f, 12f),
					Main.rand.NextVector2Circular(2f, 2f), 1f, Main.rand.Next(30, 50)));
			ScarabFX.SandKick(Projectile.Bottom, 0.8f, 0, 5);
		}

		public override bool CanHitPlayer(Player target)
			=> Kind == Cracked || Projectile.timeLeft > Fade;

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			// round, not square: a ball that clips you by its corner is a ball nobody can jump
			Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
			return Vector2.DistanceSquared(nearest, Projectile.Center) <= Size * Size * 0.25f;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			SpriteBatch sb = Main.spriteBatch;
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Rectangle frame = tex.Frame(1, Main.projFrames[Type], 0, Math.Clamp(Kind, 0, 2));
			Vector2 origin = frame.Size() * 0.5f;

			Vector2 world = VisualCenter;
			Vector2 at = world - Main.screenPosition;
			float fade = 1f - Projectile.alpha / 255f;
			float time = Main.GlobalTimeWrappedHourly;
			float seed = Projectile.identity * 0.37f;
			float radius = Visual - 2f;

			if (Kind == Cracked && Projectile.timeLeft <= BurstTell)
				DrawSpokes(sb, 1f - Projectile.timeLeft / (float)BurstTell, time);

			if (Kind == Dung)
			{
				Main.EntitySpriteDraw(tex, at, frame, lightColor * fade, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
				return false;
			}

			Color body = Color.Lerp(lightColor, Color.White, 0.7f);
			if (Kind == Cracked)
			{
				float f = FuseBurnt;
				float pulse = Pulse;
				// the swell of the heartbeat shows in the halo and in the body's own size
				float beat = 1f + 0.05f * pulse * (0.3f + f);
				SunDraw.Halo(sb, world, radius * beat, 0.75f + 0.45f * pulse * (0.3f + 0.7f * f), time * (1f + f), seed);
				Main.EntitySpriteDraw(tex, at, frame, body, Projectile.rotation, origin, beat, SpriteEffects.None, 0);
				SunDraw.CrackLight(sb, world, radius * beat, Projectile.rotation, f, pulse, 1f);
				SunDraw.Core(sb, world, radius, 0.6f + 0.6f * pulse * f);

				if (Projectile.timeLeft <= BurstTell && Projectile.timeLeft % 8 < 4)
					Main.EntitySpriteDraw(tex, at, frame, new Color(255, 255, 255, 0) * 0.7f, Projectile.rotation, origin, 1.08f * beat,
						SpriteEffects.None, 0);
				return false;
			}

			SunDraw.Halo(sb, world, radius, fade, time, seed);
			Main.EntitySpriteDraw(tex, at, frame, body * fade, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
			SunDraw.Core(sb, world, radius, fade);
			return false;
		}

		/// <summary>
		/// The burst's tell: eight spokes along the lines the shards will fly. Each is a heat haze, a
		/// hot body (the same width and brightness the plain line had, so it is no harder to read),
		/// a white heart, motes running outward and a glint at the tip that swells to the burst.
		/// </summary>
		private void DrawSpokes(SpriteBatch sb, float f, float time)
		{
			Vector2 c = Projectile.Center;
			for (int i = 0; i < Shards; i++)
			{
				Vector2 dir = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / Shards);
				Vector2 a = c + dir * 20f;
				Vector2 b = c + dir * (60f + 160f * f);
				SunDraw.Line(sb, a, b, SunPalette.Glow(SunPalette.Orange, 0.1f + 0.22f * f), 6f + 6f * f);
				SunDraw.Line(sb, a, b, new Color(255, 170, 70, 0) * (0.2f + 0.6f * f), 2f + 2f * f);
				SunDraw.Line(sb, a, b, SunPalette.Glow(SunPalette.White, 0.15f + 0.5f * f), 1f);
				for (int k = 0; k < 2; k++)
				{
					float s = (time * 1.6f + k * 0.5f + i * 0.13f) % 1f;
					Vector2 m = Vector2.Lerp(a, b, s);
					SunDraw.Flare(sb, m, SunPalette.Glow(SunPalette.Pale, (0.3f + 0.5f * f) * (float)Math.Sin(s * MathHelper.Pi)),
						new Vector2(8f + 4f * f), 0f);
				}
				SunDraw.Star(sb, b, SunPalette.Gold, 10f + 16f * f, dir.ToRotation() + MathHelper.PiOver4, 0.25f + 0.65f * f);
			}
		}
	}

	/// <summary>
	/// A shard of the cracked sun. Flies out along its spoke and speeds up - it starts slow, so the
	/// player sees the ring open before it reaches them. It never turns. It leaves a short hot
	/// streak of sparks behind it, which also makes its line readable as it accelerates.
	/// </summary>
	public class SunShard : ModProjectile
	{
		public const float MaxSpeed = 9f;

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 6;
			ProjectileID.Sets.TrailingMode[Type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.hostile = true;
			Projectile.penetrate = 1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 150;
		}

		public override void AI()
		{
			if (Projectile.velocity.Length() < MaxSpeed)
				Projectile.velocity *= 1.045f;
			Projectile.rotation = Projectile.velocity.ToRotation();
			Lighting.AddLight(Projectile.Center, 0.6f, 0.4f, 0.1f);
			Projectile.alpha = Projectile.timeLeft < 15 ? (15 - Projectile.timeLeft) * 17 : 0;

			if (!Main.dedServ && Projectile.alpha < 200)
			{
				Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
				ParticleSystem.Spawn(new Spark(Projectile.Center + back * 5f + Main.rand.NextVector2Circular(2f, 2f),
					back * Main.rand.NextFloat(0.5f, 1.5f) + Main.rand.NextVector2Circular(0.4f, 0.4f),
					Main.rand.NextBool(3) ? SunPalette.Pale : SunPalette.Orange, Main.rand.NextFloat(0.6f, 0.9f), Main.rand.Next(8, 13)));
				if (Main.rand.NextBool(4))
					ParticleSystem.Spawn(new SunEmber(Projectile.Center, back * 0.5f, 0.7f, 30, 0.3f));
			}
		}

		public override void OnKill(int timeLeft)
		{
			if (timeLeft > 0)
				FX.Sparks(Projectile.Center, SunPalette.Gold, 6, 4f, 0.8f, 14);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float fade = 1f - Projectile.alpha / 255f;
			SpriteBatch sb = Main.spriteBatch;
			Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
			float streak = 10f + Projectile.velocity.Length() * 3f;
			// the hot streak: a tapering flare stretched behind it, then the trail, bloom and head
			SunDraw.Flare(sb, Projectile.Center + back * streak * 0.5f, SunPalette.Glow(SunPalette.Orange, 0.55f * fade),
				new Vector2(streak * 1.4f, 9f), Projectile.rotation);
			SunDraw.Flare(sb, Projectile.Center + back * streak * 0.35f, SunPalette.Glow(SunPalette.Pale, 0.5f * fade),
				new Vector2(streak * 0.8f, 3.5f), Projectile.rotation);
			RiftDraw.Trail(Projectile, f => new Color(255, 150, 60, 0) * (0.5f * f * fade), f => 0.6f + 0.4f * f);
			RiftDraw.Bloom(Projectile.Center, new Color(255, 170, 70, 0) * (0.4f * fade), 0.35f);
			RiftDraw.Head(Projectile, Color.White * fade);
			return false;
		}
	}
}
