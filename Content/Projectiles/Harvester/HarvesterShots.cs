using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Particles;
using Terrapex.Content.NPCs.Bosses;

namespace Terrapex.Content.Projectiles.Harvester
{
	/// <summary>
	/// The Hollow Harvester's mark: a reticle that closes on a point for 42 harmless ticks, then a
	/// cut through it. It is the player's own strike (<c>ReapingSlash</c>) pointed the other way,
	/// and it borrows that texture - the class should recognise its own move being used on it.
	///
	/// The effects are layered on the same timeline and change nothing about it: a scythe rune
	/// burns in stroke by stroke over the tell while motes are drawn into it (the "when"), the
	/// closing ticks and the ring of reach are kept exactly (the "where"), and the rune whitens
	/// over the last eight ticks before the crescent cuts through with sparks and straw.
	/// </summary>
	public class HarvesterCut : ModProjectile
	{
		public const int Tell = 42;
		private const int Cut = 14;
		private const float Radius = 46f;
		private static readonly Color Ghost = new(190, 236, 200);

		private ref float Timer => ref Projectile.ai[0];

		/// <summary>The rune's own angle: cosmetic, so each client may roll its own.</summary>
		private ref float RuneAngle => ref Projectile.localAI[1];

		public override string Texture => "Terrapex/Content/Projectiles/Reaper/ReapingSlash";

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Tell + Cut;
		}

		public override void AI()
		{
			Timer++;
			Projectile.velocity = Vector2.Zero;
			if (Timer == Tell)
				SoundEngine.PlaySound(SoundID.Item71 with { Pitch = 0.2f, Volume = 0.7f }, Projectile.Center);
			if (Timer > Tell)
				Projectile.rotation = (Timer - Tell) / Cut * MathHelper.Pi;
			Lighting.AddLight(Projectile.Center, Ghost.ToVector3() * (Timer > Tell ? 0.5f : 0.15f + 0.15f * Timer / Tell));
			if (!Main.dedServ)
				Cosmetics();
		}

		private void Cosmetics()
		{
			Vector2 c = Projectile.Center;
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				RuneAngle = Main.rand.NextFloat(MathHelper.TwoPi);
			}

			if (Timer < Tell)
			{
				// motes drawn in to the rune, thicker as the cut nears
				float f = Timer / Tell;
				if (Main.rand.NextFloat() < 0.2f + 0.6f * f)
				{
					Vector2 from = c + Main.rand.NextVector2Unit() * Main.rand.NextFloat(48f, 86f);
					ParticleSystem.Spawn(new Mote(from, c, Main.rand.NextBool(3) ? HarvestPalette.Ember : Ghost,
						Main.rand.NextFloat(0.7f, 1.1f), 40, 0.4f + f));
				}
				// the rune scorching: a few embers lifting off the burnt stroke
				if (Main.rand.NextBool(5))
				{
					Vector2 on = c + (RuneAngle + Main.rand.NextFloat(4f)).ToRotationVector2() * 22f;
					ParticleSystem.Spawn(new Spark(on, new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(0.6f, 1.4f)),
						HarvestPalette.Ember, 0.6f, Main.rand.Next(18, 30), -0.02f) { Drag = 0.97f });
				}
				return;
			}

			if (Timer == Tell)
			{
				// the cut: a crescent across the rune, a hot core, sparks and cut straw thrown off it
				ParticleSystem.Spawn(new Crescent(c, Radius * 0.78f, -0.4f, MathHelper.Pi + 0.8f, 9f, Ghost, Color.White, 20));
				ParticleSystem.Spawn(new Crescent(c, Radius * 0.55f, -0.1f, MathHelper.Pi + 0.2f, 5f, Ghost * 0.6f, Ghost, 16));
				ParticleSystem.Spawn(new Bloom(c, Ghost, 0.4f, 1.3f, 16));
				ParticleSystem.Spawn(new Shockwave(c, Ghost * 0.6f, 0.15f, 0.9f, 20));
				FX.Sparks(c, Ghost, 10, 7f, 1f, 20);
				FX.Sparks(c, HarvestPalette.Ember, 6, 5f, 0.8f, 24, 0.12f);
				for (int i = 0; i < 12; i++)
				{
					Vector2 v = Main.rand.NextVector2Circular(4.5f, 3f) - new Vector2(0f, Main.rand.NextFloat(1f, 3f));
					ParticleSystem.Spawn(new Straw(c + Main.rand.NextVector2Circular(16f, 16f), v, HarvestPalette.RandomStraw(),
						Main.rand.NextFloat(5f, 9f), Main.rand.Next(50, 80)));
				}
			}
			else if (Timer < Tell + Cut - 2 && Main.rand.NextBool(2))
			{
				// sparks shed off the moving edge while the cut can hurt
				Vector2 edge = c + Projectile.rotation.ToRotationVector2() * Radius * 0.75f;
				ParticleSystem.Spawn(new Spark(edge, (Projectile.rotation + MathHelper.PiOver2).ToRotationVector2() * 4f,
					Ghost, 0.8f, 14));
			}
		}

		public override bool CanHitPlayer(Player target) => Timer > Tell + 1 && Timer < Tell + Cut - 2;

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
			return Vector2.DistanceSquared(nearest, Projectile.Center) <= Radius * Radius;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D px = TextureAssets.MagicPixel.Value;
			SpriteBatch sb = Main.spriteBatch;
			Vector2 c = Projectile.Center;
			Vector2 at = c - Main.screenPosition;
			if (Timer <= Tell)
			{
				float f = Timer / Tell;

				// scorch: a dark patch under the rune, so it reads as burnt into something
				sb.Draw(ParticleSystem.Flare, at, null, new Color(14, 10, 18) * (0.45f * f), 0f,
					ParticleSystem.Flare.Size() * 0.5f, 0.75f, SpriteEffects.None, 0f);

				// the reach of the cut: a faint ring, brightening, with one pulse running round it
				int dashes = 36;
				for (int i = 0; i < dashes; i++)
				{
					float a = MathHelper.TwoPi * i / dashes;
					float run = (float)Math.Pow(Math.Max(0.0, Math.Cos(a - Timer * 0.18f)), 12);
					sb.Draw(px, at + a.ToRotationVector2() * Radius, new Rectangle(0, 0, 1, 1),
						new Color(Ghost.R, Ghost.G, Ghost.B, 0) * (0.15f + 0.3f * f + 0.4f * run), a + MathHelper.PiOver2,
						new Vector2(0.5f), new Vector2(5f, 2f), SpriteEffects.None, 0f);
				}

				// the four ticks closing in: the original tell, kept exactly as it was
				float gap = MathHelper.Lerp(40f, 8f, f);
				for (int i = 0; i < 4; i++)
				{
					Vector2 d = Vector2.UnitX.RotatedBy(MathHelper.PiOver2 * i + MathHelper.PiOver4);
					for (int k = 0; k < 3; k++)
						Main.EntitySpriteDraw(px, at + d * (gap + k * 3f), new Rectangle(0, 0, 1, 1),
							Ghost * (0.35f + 0.6f * f), 0f, new Vector2(0.5f), 2f, SpriteEffects.None);
				}

				DrawRune(sb, c, f, Timer >= Tell - 8 ? (Timer - (Tell - 8)) / 8f : 0f);
				return false;
			}

			Texture2D tex = TextureAssets.Projectile[Type].Value;
			float t = (Timer - Tell) / Cut;
			float fade = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
			// the rune burning out under the cut
			DrawRune(sb, c, 1f, 0f, 1f - t);
			Vector2 origin = new(tex.Width * 0.25f, tex.Height / 2f);
			for (int i = 3; i >= 0; i--)
			{
				Main.EntitySpriteDraw(tex, at, null, new Color(Ghost.R, Ghost.G, Ghost.B, 0) * (fade * (i == 0 ? 0.95f : 0.2f)),
					Projectile.rotation - i * 0.22f, origin, 1.1f, SpriteEffects.None);
			}
			return false;
		}

		/// <summary>
		/// The rune: a scythe's crescent and the slash through it, written stroke by stroke over the
		/// tell. A dark burnt stroke under a ghost-light one, with an ember at the pen's tip while it
		/// is still being drawn. <paramref name="flash"/> whitens it over the last eight ticks - the
		/// "now" of the cut. <paramref name="alpha"/> lets the cut burn it out.
		/// </summary>
		private void DrawRune(SpriteBatch sb, Vector2 c, float f, float flash, float alpha = 1f)
		{
			float a0 = RuneAngle;
			float arc = MathHelper.Clamp(f * 1.6f, 0f, 1f);
			float slash = MathHelper.Clamp(f * 1.8f - 0.75f, 0f, 1f);
			float glow = (0.25f + 0.65f * f) * alpha;
			Color burnt = new Color(30, 16, 22) * (0.7f * alpha);
			Color light = Color.Lerp(Ghost, Color.White, flash);
			light = new Color(light.R, light.G, light.B, 0) * glow;
			Color core = new Color(255, 255, 255, 0) * (glow * (0.3f + 0.7f * flash));

			const float r = 22f, sweep = 4.1f;
			if (arc > 0f)
			{
				Crescent.Draw(sb, c, r, a0, sweep * arc, 6f, burnt, default);
				Crescent.Draw(sb, c, r, a0, sweep * arc, 3.5f, light, core);
			}

			Vector2 dir = (a0 + 0.9f).ToRotationVector2();
			Vector2 s0 = c - dir * 30f;
			Vector2 s1 = Vector2.Lerp(s0, c + dir * 30f, slash);
			if (slash > 0f)
			{
				Line(sb, s0, s1, burnt, 4f);
				Line(sb, s0, s1, light, 2f);
			}

			// the ember at the pen tip
			if (alpha >= 1f && f < 1f && arc > 0f)
			{
				Vector2 tip = slash > 0f ? s1 : c + (a0 + sweep * arc).ToRotationVector2() * r;
				sb.Draw(ParticleSystem.Flare, tip - Main.screenPosition, null, new Color(255, 200, 120, 0) * 0.6f, 0f,
					ParticleSystem.Flare.Size() * 0.5f, 0.12f, SpriteEffects.None, 0f);
			}
		}

		private static void Line(SpriteBatch sb, Vector2 a, Vector2 b, Color color, float thick)
		{
			Vector2 seg = b - a;
			if (seg.LengthSquared() < 1f)
				return;
			sb.Draw(TextureAssets.MagicPixel.Value, a - Main.screenPosition, new Rectangle(0, 0, 1, 1), color,
				seg.ToRotation(), new Vector2(0f, 0.5f), new Vector2(seg.Length(), thick), SpriteEffects.None, 0f);
		}
	}

	/// <summary>
	/// A straw crow, thrown in a fan at where the player stood. It does not turn - nothing a
	/// patterned boss in this mod fires homes - so the player reads the fan and steps out of it.
	///
	/// It is a night boss, and the first crows were near-black birds drawn in world light with a
	/// dark trail: on a night sky they simply were not there. Now the sprite is straw, it is drawn
	/// mostly unlit, it carries a ghost-green rim and a ghost trail, and it lights the air it
	/// passes through.
	/// </summary>
	public class HarvesterCrow : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			Main.projFrames[Type] = 2;
			ProjectileID.Sets.TrailCacheLength[Type] = 6;
			ProjectileID.Sets.TrailingMode[Type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.width = 18;
			Projectile.height = 18;
			Projectile.hostile = true;
			Projectile.penetrate = 1;
			Projectile.tileCollide = false;
			Projectile.timeLeft = 170;
		}

		public override void AI()
		{
			Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;
			Projectile.rotation = Projectile.velocity.ToRotation() + (Projectile.spriteDirection < 0 ? MathHelper.Pi : 0f);
			if (++Projectile.frameCounter >= 6)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
			}
			Projectile.alpha = Projectile.timeLeft < 20 ? (20 - Projectile.timeLeft) * 12 : 0;
			Lighting.AddLight(Projectile.Center, 0.22f * Projectile.Opacity, 0.32f * Projectile.Opacity, 0.24f * Projectile.Opacity);

			if (Main.dedServ)
				return;
			if (Projectile.localAI[0] == 0f)
			{
				// the caw: a burst of feathers where it leaves the Harvester's cloak
				Projectile.localAI[0] = 1f;
				for (int i = 0; i < 5; i++)
					ParticleSystem.Spawn(new Feather(Projectile.Center, Projectile.velocity * 0.3f + Main.rand.NextVector2Circular(3f, 3f),
						Main.rand.NextFloat(0.8f, 1.2f), Main.rand.Next(50, 80)));
				ParticleSystem.Spawn(new Smoke(Projectile.Center, Projectile.velocity * 0.2f, HarvestPalette.Crow, 0.3f, 24));
			}
			if (Main.rand.NextBool(9) && Projectile.alpha < 120)
				ParticleSystem.Spawn(new Feather(Projectile.Center - Projectile.velocity,
					-Projectile.velocity * 0.15f + Main.rand.NextVector2Circular(0.6f, 0.6f),
					Main.rand.NextFloat(0.6f, 1f), Main.rand.Next(40, 70)));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// a ghost trail behind it, then a ghost-green rim, then the bird itself mostly unlit:
			// readable against the night sky, which a dark shadow trail never was
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Rectangle frame = tex.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
			SpriteEffects fx = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Vector2 half = Projectile.Size * 0.5f;
			Vector2 origin = frame.Size() * 0.5f;
			float op = Projectile.Opacity;
			Color ghost = new(150, 255, 180, 0);
			for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
			{
				if (Projectile.oldPos[i] == Vector2.Zero)
					continue;
				float f = 1f - i / (float)Projectile.oldPos.Length;
				Main.EntitySpriteDraw(tex, Projectile.oldPos[i] + half - Main.screenPosition, frame,
					ghost * (0.3f * f * op), Projectile.oldRot[i], origin, Projectile.scale * (0.8f + 0.2f * f), fx);
			}
			Vector2 at = Projectile.Center - Main.screenPosition;
			for (int k = 0; k < 4; k++)
				Main.EntitySpriteDraw(tex, at + (MathHelper.PiOver2 * k).ToRotationVector2() * 2f, frame, ghost * (0.45f * op),
					Projectile.rotation, origin, Projectile.scale, fx);
			Main.EntitySpriteDraw(tex, at, frame, Color.Lerp(lightColor, Color.White, 0.7f) * op,
				Projectile.rotation, origin, Projectile.scale, fx);
			return false;
		}

		public override void PostDraw(Color lightColor)
		{
			// one ghost-light eye, so the fan stays readable at night
			Vector2 head = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 8f;
			Main.EntitySpriteDraw(ParticleSystem.Flare, head - Main.screenPosition, null,
				new Color(190, 255, 210, 0) * (0.8f * Projectile.Opacity), 0f, ParticleSystem.Flare.Size() * 0.5f, 0.12f,
				SpriteEffects.None);
		}

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 5; i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.Hay, Main.rand.NextVector2Circular(2f, 2f), 80, default, 0.9f);
			for (int i = 0; i < 6; i++)
				ParticleSystem.Spawn(new Feather(Projectile.Center, Main.rand.NextVector2Circular(3f, 3f),
					Main.rand.NextFloat(0.8f, 1.2f), Main.rand.Next(50, 90)));
			for (int i = 0; i < 3; i++)
				ParticleSystem.Spawn(new Straw(Projectile.Center, Main.rand.NextVector2Circular(2.5f, 2.5f),
					HarvestPalette.RandomStraw(), Main.rand.NextFloat(4f, 7f), Main.rand.Next(40, 70)));
		}
	}

	/// <summary>
	/// The scythe, thrown in phase two along the line it drew: out, a turn, and back to the
	/// Harvester's hand - a boomerang, so its whole path is the drawn line twice.
	/// ai[0] is the Harvester's NPC index.
	/// </summary>
	public class HarvesterScythe : ModProjectile
	{
		private const int Out = 30;

		private ref float Timer => ref Projectile.ai[1];

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 8;
			ProjectileID.Sets.TrailingMode[Type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.width = 48;
			Projectile.height = 48;
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.timeLeft = 240;
		}

		public override void AI()
		{
			Timer++;
			NPC owner = Main.npc[(int)Projectile.ai[0]];
			bool ownerAlive = owner.active && owner.type == ModContent.NPCType<HollowHarvester>();
			if (Timer < Out)
				Projectile.velocity *= 0.97f;
			else if (ownerAlive)
			{
				Vector2 home = owner.Center - Projectile.Center;
				float d = home.Length();
				if (d < 40f)
				{
					Projectile.Kill();
					return;
				}
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, home / d * 15f, 0.12f);
			}
			else
				Projectile.Kill();
			Projectile.rotation += 0.35f;
			if (Main.rand.NextBool(3))
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Hay, 0f, 0f, 90, default, 0.9f).noGravity = true;
			Lighting.AddLight(Projectile.Center, 0.45f, 0.62f, 0.48f);

			if (Main.dedServ)
				return;
			// sparks flung off the edge, tangent to the spin and carried by the flight
			if (Main.rand.NextBool(2))
			{
				Vector2 rim = Projectile.rotation.ToRotationVector2();
				ParticleSystem.Spawn(new Spark(Projectile.Center + rim * 30f,
					rim.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(3f, 6f) + Projectile.velocity * 0.4f,
					Main.rand.NextBool(3) ? HarvestPalette.Ember : HarvestPalette.Ghost, 0.8f, Main.rand.Next(12, 20), 0.1f));
			}
			if (Main.rand.NextBool(6))
				ParticleSystem.Spawn(new Chaff(Projectile.Center + Main.rand.NextVector2Circular(20f, 20f),
					Projectile.velocity * 0.2f, HarvestPalette.RandomStraw(), 60));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			SpriteBatch sb = Main.spriteBatch;
			Vector2 c = Projectile.Center;
			Color ghost = new(190, 236, 200, 0);

			// a crescent swept behind the blade: the spin made visible
			Crescent.Draw(sb, c, 31f, Projectile.rotation - 2.4f, 2.4f, 8f, ghost * 0.28f, new Color(255, 255, 255, 0) * 0.35f, 14);
			Crescent.Draw(sb, c, 22f, Projectile.rotation - 1.6f, 1.6f, 4f, ghost * 0.16f, default, 10);

			// afterimages down its path, in ghost-light
			for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
			{
				if (Projectile.oldPos[i] == Vector2.Zero)
					continue;
				float f = 1f - i / (float)Projectile.oldPos.Length;
				Main.EntitySpriteDraw(tex, Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition, null,
					ghost * (0.22f * f), Projectile.oldRot[i], tex.Size() / 2f, 1f, SpriteEffects.None);
			}

			// a ghost rim, then the blade mostly unlit, then ghost-light over it: never a dark shape at night
			for (int k = 0; k < 4; k++)
				Main.EntitySpriteDraw(tex, c - Main.screenPosition + (MathHelper.PiOver2 * k).ToRotationVector2() * 2f, null,
					ghost * 0.4f, Projectile.rotation, tex.Size() / 2f, 1f, SpriteEffects.None);
			Main.EntitySpriteDraw(tex, c - Main.screenPosition, null, Color.Lerp(lightColor, Color.White, 0.6f), Projectile.rotation,
				tex.Size() / 2f, 1f, SpriteEffects.None);
			Main.EntitySpriteDraw(tex, c - Main.screenPosition, null, ghost * 0.3f, Projectile.rotation,
				tex.Size() / 2f, 1f, SpriteEffects.None);
			return false;
		}

		public override void OnKill(int timeLeft)
		{
			ParticleSystem.Spawn(new Bloom(Projectile.Center, HarvestPalette.Ghost, 0.3f, 0.8f, 14));
			FX.Sparks(Projectile.Center, HarvestPalette.Ghost, 8, 5f, 0.9f, 18);
		}
	}
}
