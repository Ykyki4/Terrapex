using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Particles;
using Terrapex.Content.NPCs.Bosses;

namespace Terrapex.Content.Projectiles.Harvester
{
	/// <summary>
	/// A jack-o'-lantern lobbed in phase two. It is conjured over the Harvester and hangs there for
	/// <see cref="Hold"/> harmless ticks while its whole arc is drawn as a dotted line and a ring is
	/// laid on the ground where it will burst - the arc is the same sum it then flies, so the drawn
	/// line is not a hint but the path. It bursts on the ring after exactly <see cref="Flight"/>
	/// ticks. ai[0], ai[1]: the landing point. ai[2]: the tick counter.
	///
	/// Pumpkin orange and ember yellow on purpose: this is a night fight, and the phase-two
	/// lantern is the colour that reads on a dark sky.
	/// </summary>
	public class HarvesterPumpkin : ModProjectile
	{
		public const int Hold = 34;
		public const int Flight = 52;
		public const float Gravity = 0.24f;
		public const float BlastRadius = 72f;
		private const int BlastTicks = 8;

		private static readonly Color Pumpkin = new(255, 150, 60);
		private static readonly Color Ember = new(255, 226, 122);

		private Vector2 Target => new(Projectile.ai[0], Projectile.ai[1]);
		private ref float Timer => ref Projectile.ai[2];
		private bool Bursting => Timer > Hold + Flight;

		/// <summary>The launch that lands on <paramref name="to"/> after <see cref="Flight"/> ticks; gravity is added before each move.</summary>
		public static Vector2 Launch(Vector2 from, Vector2 to)
		{
			Vector2 d = to - from;
			float t = Flight;
			return new Vector2(d.X / t, (d.Y - Gravity * t * (t + 1f) / 2f) / t);
		}

		public override void SetDefaults()
		{
			Projectile.width = 22;
			Projectile.height = 22;
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Hold + Flight + BlastTicks + 2;
		}

		public override void AI()
		{
			Timer++;
			if (Timer == 1f)
			{
				// where it was conjured: the arc is drawn from here for the whole tell
				Projectile.localAI[0] = Projectile.Center.X;
				Projectile.localAI[1] = Projectile.Center.Y;
				if (!Main.dedServ)
				{
					ParticleSystem.Spawn(new Bloom(Projectile.Center, Pumpkin, 0.3f, 0.9f, 16));
					FX.Sparks(Projectile.Center, Ember, 6, 3f, 0.8f, 18, -0.02f);
				}
			}

			if (Timer <= Hold)
			{
				Projectile.velocity = Vector2.Zero;
				Projectile.rotation = 0.12f * MathF.Sin(Timer * 0.3f);
			}
			else if (!Bursting)
			{
				if (Timer == Hold + 1)
				{
					Projectile.velocity = Launch(Projectile.Center, Target);
					SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.4f }, Projectile.Center);
				}
				Projectile.velocity.Y += Gravity;
				Projectile.rotation += Projectile.velocity.X * 0.04f;
				if (Timer == Hold + Flight)
					Burst();
			}
			else
				Projectile.velocity = Vector2.Zero;

			float glow = Bursting ? 1.2f : 0.55f;
			Lighting.AddLight(Projectile.Center, 0.9f * glow, 0.5f * glow, 0.15f * glow);

			if (Main.dedServ || Bursting)
				return;
			if (Main.rand.NextBool(3))
				ParticleSystem.Spawn(new Spark(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
					new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), -Main.rand.NextFloat(0.6f, 1.6f)) - Projectile.velocity * 0.15f,
					Main.rand.NextBool(2) ? Pumpkin : Ember, 0.7f, Main.rand.Next(16, 28), -0.02f) { Drag = 0.97f });
		}

		private void Burst()
		{
			Projectile.velocity = Vector2.Zero;
			Projectile.Center = Target;
			Projectile.Resize((int)(BlastRadius * 2f), (int)(BlastRadius * 2f));
			Projectile.Center = Target;
			SoundEngine.PlaySound(SoundID.Item14 with { Pitch = 0.1f }, Target);
			if (Main.dedServ)
				return;
			ParticleSystem.Spawn(new Bloom(Target, Pumpkin, 0.6f, 2.2f, 26));
			ParticleSystem.Spawn(new Bloom(Target, Ember, 0.4f, 1.1f, 16));
			ParticleSystem.Spawn(new Shockwave(Target, Pumpkin, 0.1f, BlastRadius * 2f / 64f, 20));
			FX.Sparks(Target, Ember, 18, 8f, 1.1f, 30, 0.08f);
			FX.Sparks(Target, Pumpkin, 12, 6f, 1f, 26, 0.1f);
			for (int i = 0; i < 10; i++)
				ParticleSystem.Spawn(new Chip(Target, Main.rand.NextVector2Circular(5f, 5f) - new Vector2(0f, 2f),
					Main.rand.NextBool(2) ? new Color(240, 138, 42) : new Color(184, 84, 30), Main.rand.NextFloat(0.9f, 1.4f), 50));
			for (int i = 0; i < 6; i++)
				ParticleSystem.Spawn(new Smoke(Target + Main.rand.NextVector2Circular(20f, 20f), Main.rand.NextVector2Circular(1.5f, 1.5f),
					new Color(60, 40, 40), Main.rand.NextFloat(0.4f, 0.7f), 44));
			if (Main.LocalPlayer.Distance(Target) < 900f)
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(Target, Main.rand.NextVector2Unit(), 4f, 8f, 12, 900f, "HarvesterPumpkin"));
		}

		public override bool CanHitPlayer(Player target) => Timer > Hold && Timer <= Hold + Flight + BlastTicks - 2;

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (!Bursting)
				return null;
			Vector2 nearest = Vector2.Clamp(Target, targetHitbox.TopLeft(), targetHitbox.BottomRight());
			return Vector2.DistanceSquared(nearest, Target) <= BlastRadius * BlastRadius;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (Bursting)
				return false;
			SpriteBatch sb = Main.spriteBatch;
			Texture2D px = TextureAssets.MagicPixel.Value;
			Texture2D flare = ParticleSystem.Flare;
			float time = Main.GlobalTimeWrappedHourly;
			float tell = MathHelper.Clamp(Timer / Hold, 0f, 1f);

			// the arc, as dots on the same sum it flies, drawn from where it was conjured
			Vector2 from = new(Projectile.localAI[0], Projectile.localAI[1]);
			if (from != Vector2.Zero)
			{
				Vector2 v0 = Launch(from, Target);
				int start = Timer > Hold ? (int)(Timer - Hold) : 0;
				for (int n = start + 2; n < Flight; n += 3)
				{
					Vector2 p = from + v0 * n + new Vector2(0f, Gravity * n * (n + 1) / 2f);
					float run = 0.5f + 0.5f * MathF.Sin(n * 0.5f - time * 9f);
					sb.Draw(px, p - Main.screenPosition, new Rectangle(0, 0, 1, 1),
						new Color(255, 170, 80, 0) * ((0.25f + 0.45f * tell) * (0.6f + 0.4f * run)), 0f, new Vector2(0.5f), 3f,
						SpriteEffects.None, 0f);
				}
			}

			// the ring where it bursts, exactly the blast's radius, tightening its dashes as it comes
			float come = Timer <= Hold ? tell * 0.5f : 0.5f + 0.5f * (Timer - Hold) / Flight;
			int dashes = 40;
			for (int i = 0; i < dashes; i++)
			{
				float a = MathHelper.TwoPi * i / dashes + time * 0.6f;
				Vector2 at = Target + a.ToRotationVector2() * BlastRadius - Main.screenPosition;
				sb.Draw(px, at, new Rectangle(0, 0, 1, 1), new Color(255, 150, 60, 0) * (0.3f + 0.55f * come), a + MathHelper.PiOver2,
					new Vector2(0.5f), new Vector2(4f + 3f * come, 2f), SpriteEffects.None, 0f);
			}
			sb.Draw(flare, Target - Main.screenPosition, null, new Color(255, 120, 40, 0) * (0.12f + 0.25f * come), 0f,
				flare.Size() * 0.5f, BlastRadius * 2f / flare.Width, SpriteEffects.None, 0f);

			// the lantern: a warm glow behind, the carved sprite unlit, the face flickering
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Vector2 c = Projectile.Center - Main.screenPosition;
			float flicker = 0.8f + 0.2f * MathF.Sin(time * 14f) * MathF.Sin(time * 5f);
			sb.Draw(flare, c, null, new Color(255, 140, 50, 0) * (0.55f * flicker), 0f, flare.Size() * 0.5f, 0.55f, SpriteEffects.None, 0f);
			Main.EntitySpriteDraw(tex, c, null, Color.White, Projectile.rotation, tex.Size() * 0.5f, 1f, SpriteEffects.None);
			Main.EntitySpriteDraw(tex, c, null, new Color(255, 220, 140, 0) * (0.25f * flicker), Projectile.rotation, tex.Size() * 0.5f,
				1f, SpriteEffects.None);
			return false;
		}
	}

	/// <summary>
	/// Phase three's Last Harvest: the Harvester's own reach turns into the blade. The circle stands
	/// at full size for <see cref="Tell"/> harmless ticks with a door in it, lit at both posts, and
	/// then closes on the spot it was cast round. The rim cuts; the door does not. Go out through the
	/// door, or be inside when it reaches the middle.
	///
	/// One projectile because the ring moves - the same reason the Weaver's net is one. ai[0]: the
	/// door's angle. ai[1]: the tick counter.
	/// </summary>
	public class HarvesterField : ModProjectile
	{
		public const int Tell = 44;
		public const int Close = 120;
		private const int Fade = 14;
		public const float EndRadius = 70f;
		private const float Band = 12f;
		public const float DoorHalf = 0.42f;

		private static readonly Color Ghost = new(190, 255, 210);

		private float DoorAngle => Projectile.ai[0];
		private ref float Timer => ref Projectile.ai[1];

		public override string Texture => "Terrapex/Content/Projectiles/Harvester/HarvesterScythe";

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Tell + Close + Fade;
		}

		private float Radius
		{
			get
			{
				if (Timer <= Tell)
					return HollowHarvester.ReachRadius;
				float t = MathHelper.Clamp((Timer - Tell) / Close, 0f, 1f);
				t = t * t * (3f - 2f * t);
				return MathHelper.Lerp(HollowHarvester.ReachRadius, EndRadius, t);
			}
		}

		private bool Cutting => Timer > Tell && Timer < Tell + Close;

		public override void AI()
		{
			Timer++;
			Projectile.velocity = Vector2.Zero;
			if (Timer == Tell)
				SoundEngine.PlaySound(SoundID.Item71 with { Pitch = -0.7f, Volume = 1.2f }, Projectile.Center);
			if (Main.dedServ)
				return;

			float r = Radius;
			// straw torn off the closing rim, and the door's posts burning
			if (Cutting)
			{
				float a = Main.rand.NextFloat(MathHelper.TwoPi);
				if (Math.Abs(MathHelper.WrapAngle(a - DoorAngle)) > DoorHalf)
				{
					Vector2 n = a.ToRotationVector2();
					ParticleSystem.Spawn(new Straw(Projectile.Center + n * r, -n * Main.rand.NextFloat(0.5f, 2f) + Main.rand.NextVector2Circular(1f, 1f),
						HarvestPalette.RandomStraw(), Main.rand.NextFloat(4f, 7f), Main.rand.Next(30, 50)));
				}
			}
			for (int s = -1; s <= 1; s += 2)
			{
				Vector2 post = Projectile.Center + (DoorAngle + s * DoorHalf).ToRotationVector2() * r;
				Lighting.AddLight(post, 0.9f, 0.5f, 0.15f);
				if (Main.rand.NextBool(3))
					ParticleSystem.Spawn(new Spark(post, new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -Main.rand.NextFloat(0.8f, 2f)),
						HarvestPalette.Pumpkin, 0.8f, Main.rand.Next(16, 26), -0.02f) { Drag = 0.97f });
			}
		}

		public override bool CanHitPlayer(Player target) => Cutting;

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Vector2 to = targetHitbox.Center.ToVector2() - Projectile.Center;
			float d = to.Length();
			if (Math.Abs(d - Radius) > Band + 14f)
				return false;
			return Math.Abs(MathHelper.WrapAngle(to.ToRotation() - DoorAngle)) > DoorHalf;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			SpriteBatch sb = Main.spriteBatch;
			Texture2D px = TextureAssets.MagicPixel.Value;
			Rectangle one = new(0, 0, 1, 1);
			Vector2 c = Projectile.Center;
			float time = Main.GlobalTimeWrappedHourly;
			float r = Radius;
			float on = Timer <= Tell ? Timer / Tell : 1f;
			float fade = Timer < Tell + Close ? 1f : 1f - (Timer - Tell - Close) / Fade;
			float bright = (Cutting ? 0.95f : 0.25f + 0.45f * on) * fade;

			const int segs = 140;
			for (int i = 0; i < segs; i++)
			{
				float a0 = MathHelper.TwoPi * i / segs, a1 = MathHelper.TwoPi * (i + 1) / segs;
				float mid = (a0 + a1) * 0.5f;
				if (Math.Abs(MathHelper.WrapAngle(mid - DoorAngle)) < DoorHalf)
					continue;
				Vector2 p0 = c + a0.ToRotationVector2() * r, p1 = c + a1.ToRotationVector2() * r;
				Vector2 seg = p1 - p0;
				float rot = seg.ToRotation();
				float pulse = MathF.Pow(Math.Max(0f, MathF.Cos(mid * 3f - time * 6f)), 8f);
				// the blade's edge: a pale haze, a bright body, a white core - bright on purpose, it is night
				sb.Draw(px, p0 - Main.screenPosition, one, new Color(Ghost.R, Ghost.G, Ghost.B, 0) * (0.22f * bright), rot,
					new Vector2(0f, 0.5f), new Vector2(seg.Length() + 1f, Band * 2f), SpriteEffects.None, 0f);
				sb.Draw(px, p0 - Main.screenPosition, one, new Color(Ghost.R, Ghost.G, Ghost.B, 0) * ((0.55f + 0.35f * pulse) * bright), rot,
					new Vector2(0f, 0.5f), new Vector2(seg.Length() + 1f, 4f), SpriteEffects.None, 0f);
				if (Cutting)
					sb.Draw(px, p0 - Main.screenPosition, one, new Color(255, 255, 255, 0) * (0.6f * fade), rot,
						new Vector2(0f, 0.5f), new Vector2(seg.Length() + 1f, 1.5f), SpriteEffects.None, 0f);
				// teeth pointing inward along the rim: the way it is going
				if (i % 4 == 0)
				{
					Vector2 n = mid.ToRotationVector2();
					Vector2 root = c + n * r;
					Vector2 tip = root - n * (10f + 4f * pulse) + n.RotatedBy(MathHelper.PiOver2) * 4f;
					Vector2 t = tip - root;
					sb.Draw(px, root - Main.screenPosition, one, new Color(Ghost.R, Ghost.G, Ghost.B, 0) * (0.7f * bright), t.ToRotation(),
						new Vector2(0f, 0.5f), new Vector2(t.Length(), 2f), SpriteEffects.None, 0f);
				}
			}

			// the door: two lit posts and a warm glow across the gap - the way out, drawn from tick one
			Texture2D flare = ParticleSystem.Flare;
			for (int s = -1; s <= 1; s += 2)
			{
				Vector2 post = c + (DoorAngle + s * DoorHalf).ToRotationVector2() * r - Main.screenPosition;
				sb.Draw(flare, post, null, new Color(255, 150, 60, 0) * (0.7f * fade), 0f, flare.Size() * 0.5f, 0.3f, SpriteEffects.None, 0f);
				sb.Draw(flare, post, null, new Color(255, 230, 160, 0) * (0.8f * fade), 0f, flare.Size() * 0.5f, 0.1f, SpriteEffects.None, 0f);
			}
			Vector2 gap = c + DoorAngle.ToRotationVector2() * r - Main.screenPosition;
			sb.Draw(flare, gap, null, new Color(255, 170, 80, 0) * ((0.12f + 0.1f * MathF.Sin(time * 5f)) * fade), 0f,
				flare.Size() * 0.5f, r * DoorHalf * 2.2f / flare.Width, SpriteEffects.None, 0f);
			return false;
		}
	}
}
