using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Reaper;
using Terrapex.Content.Projectiles.Scarab;

namespace Terrapex.Content.Projectiles.Reaper
{
	/// <summary>
	/// The Tidewrack set's breaker: the Old Shellback's landing wave, handed to the reaper. A Reap
	/// spends the swells the set has banked and sends one of these along the floor each way from
	/// the reaper's feet. It runs exactly to the rim of the reach circle and breaks there - the
	/// circle on screen is where the wave stops - so the tide throws a crowd out onto the edge band
	/// the next strike wants. ai[0] is the direction (+1/-1), ai[1] the distance it may run.
	///
	/// Floor-finding is the boss wave's: look a few tiles up and down for ground and ride it; a
	/// wall or a cliff breaks it early. Everything is deterministic from its spawn, so every client
	/// runs the same wave and nothing needs syncing past the spawn itself.
	/// </summary>
	public class TideBreaker : ModProjectile
	{
		public const float Speed = 9f;

		private float Dir => Projectile.ai[0] >= 0f ? 1f : -1f;
		private ref float Run => ref Projectile.localAI[0];

		public override string Texture => "Terrapex/Content/Projectiles/Shellback/ShellbackWave";

		public override void SetStaticDefaults() => Main.projFrames[Type] = 3;

		public override void SetDefaults()
		{
			Projectile.width = 30;
			Projectile.height = 40;
			Projectile.friendly = true;
			Projectile.DamageType = ModContent.GetInstance<ReapingDamage>();
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.timeLeft = 120;
		}

		public override void AI()
		{
			Projectile.velocity = new Vector2(Dir * Speed, 0f);
			Run += Speed;
			if (Run >= Projectile.ai[1])
			{
				Projectile.Kill();
				return;
			}

			int tx = (int)(Projectile.Center.X / 16f);
			int top = (int)((Projectile.Bottom.Y - 48f) / 16f);
			int ground = -1;
			for (int ty = top; ty < top + 8; ty++)
			{
				if (WorldGen.InWorld(tx, ty) && WorldGen.SolidTile(tx, ty))
				{
					ground = ty;
					break;
				}
			}
			if (ground < 0 || ground == top)
			{
				Projectile.Kill();
				return;
			}
			Projectile.position.Y = ground * 16f - Projectile.height;

			Projectile.spriteDirection = (int)Dir;
			if (++Projectile.frameCounter >= 5)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
			}
			if (Main.rand.NextBool(2))
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Water,
					Dir * 1.5f, -2.5f, 80, default, 1.1f);
			float left = Projectile.ai[1] - Run;
			Projectile.alpha = left < 40f ? (int)((40f - left) / 40f * 200f) : 0;
			Lighting.AddLight(Projectile.Center, 0.08f, 0.22f, 0.26f);
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
			=> modifiers.HitDirectionOverride = (int)Dir;

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			for (int i = 0; i < 6; i++)
				Dust.NewDustPerfect(target.Center, DustID.Water, new Vector2(Dir * Main.rand.NextFloat(1f, 3f), -Main.rand.NextFloat(1f, 4f)),
					60, default, 1.2f);
		}

		public override void OnKill(int timeLeft)
		{
			// the wave breaks at the rim: a burst of spray, so the end of the run is visible too
			SoundEngine.PlaySound(SoundID.Splash with { Volume = 0.5f, Pitch = 0.2f }, Projectile.Center);
			for (int i = 0; i < 14; i++)
				Dust.NewDustPerfect(Projectile.Bottom + new Vector2(0f, -8f), DustID.Water,
					new Vector2(Main.rand.NextFloat(-2f, 2f), -Main.rand.NextFloat(2f, 6f)), 60, default, 1.3f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Rectangle frame = tex.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
			float fade = 1f - Projectile.alpha / 255f;
			SpriteEffects fx = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, Color.Lerp(lightColor, Color.White, 0.4f) * fade,
				0f, frame.Size() * 0.5f, 1f, fx, 0);
			return false;
		}
	}

	/// <summary>
	/// The Sunwrought set's sun: while the reaper's Harvest is full, each strike that lands rolls
	/// one of these out of the struck point, away from the reaper. It obeys the same
	/// <see cref="BallPhysics"/> as the Scarab Sultan's sun - so the dotted path the set draws from
	/// the reticle while the meter is full is where it will actually go - and it gathers as it
	/// rolls, like the ball a scarab pushes: every new enemy it rolls through adds
	/// <see cref="GrowPerHit"/> to its damage and a little to its size. ai[0]/ai[1] are its true
	/// velocity.
	/// </summary>
	public class SunwroughtSun : ModProjectile
	{
		public const int Size = 26;
		public const int Life = 150;
		public const float GrowPerHit = 0.15f;
		public const int MaxGrowth = 4;
		/// <summary>Launch speed along the floor and the little hop it is thrown with.</summary>
		public const float LaunchX = 7f;
		public const float LaunchY = -3f;

		private ref float Grown => ref Projectile.localAI[0];
		private int baseDamage;

		public override string Texture => "Terrapex/Content/Projectiles/Scarab/SunBall";

		public override void SetDefaults()
		{
			Projectile.width = Size;
			Projectile.height = Size;
			Projectile.friendly = true;
			Projectile.DamageType = ModContent.GetInstance<ReapingDamage>();
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.timeLeft = Life;
		}

		public override void AI()
		{
			if (baseDamage == 0)
			{
				baseDamage = Projectile.damage;
				SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = 0.3f }, Projectile.Center);
			}
			Vector2 vel = new(Projectile.ai[0], Projectile.ai[1]);
			Vector2 pos = Projectile.position;
			Vector2 moved = BallPhysics.Step(ref pos, ref vel, Projectile.width, Projectile.height);
			Projectile.position = pos;
			Projectile.velocity = moved;
			Projectile.ai[0] = vel.X;
			Projectile.ai[1] = vel.Y;
			Projectile.rotation += moved.X / (Size * 0.5f);
			Projectile.alpha = Projectile.timeLeft < 20 ? (20 - Projectile.timeLeft) * 12 : 0;
			Lighting.AddLight(Projectile.Center, 0.7f, 0.5f, 0.15f);
			if (Main.rand.NextBool(3))
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, 0f, -1f, 100, default, 0.9f).noGravity = true;
		}

		public override bool? CanDamage() => Projectile.timeLeft > 20 ? null : false;

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			float r = Size * 0.5f * Scale;
			Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
			return Vector2.DistanceSquared(nearest, Projectile.Center) <= r * r;
		}

		private float Scale => 1f + 0.08f * Grown;

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			ReapingSlash.RangedHit(Projectile, target, hit);
			if (Grown < MaxGrowth)
			{
				Grown++;
				Projectile.damage = (int)(baseDamage * (1f + GrowPerHit * Grown));
			}
			for (int i = 0; i < 6; i++)
				Dust.NewDustPerfect(target.Center, DustID.GoldFlame, Main.rand.NextVector2Circular(3f, 3f), 60, default, 1.2f).noGravity = true;
		}

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 10; i++)
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Sand, 0f, -1.5f, 80, default, 1f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			// the Sultan's sheet is three 48x48 balls stacked; the first is the whole sun
			Rectangle frame = tex.Frame(1, 3, 0, 0);
			float fade = 1f - Projectile.alpha / 255f;
			float scale = Size / 48f * Scale * 1.15f;
			RiftDraw.Bloom(Projectile.Center, new Color(255, 190, 90, 0) * (0.4f * fade), 0.55f * Scale);
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, Color.White * fade, Projectile.rotation,
				frame.Size() * 0.5f, scale, SpriteEffects.None, 0);
			return false;
		}
	}
}
