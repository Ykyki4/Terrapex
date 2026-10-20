using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Particles;

namespace Terrapex.Content.Projectiles.Scarab
{
	/// <summary>
	/// A gilded beetling, thrown in a fan at where the player stood when the Sultan's jewel flashed.
	/// It launches slow and speeds up along a straight line - it never turns, so the fan is read
	/// once and stepped out of. Two frames of wings, facing right.
	///
	/// Drawn with a short golden trail of afterimages that lengthens as it speeds up, a wing blur
	/// (the other wing frame ghosted over the current one, lit), and a glint of gold now and then.
	/// </summary>
	public class Beetling : ModProjectile
	{
		public const float MaxSpeed = 10f;

		public override void SetStaticDefaults()
		{
			Main.projFrames[Type] = 2;
			ProjectileID.Sets.TrailCacheLength[Type] = 6;
			ProjectileID.Sets.TrailingMode[Type] = 0;
		}

		public override void SetDefaults()
		{
			Projectile.width = 18;
			Projectile.height = 18;
			Projectile.hostile = true;
			Projectile.penetrate = 1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 180;
		}

		public override void AI()
		{
			if (Projectile.velocity.Length() < MaxSpeed)
				Projectile.velocity *= 1.03f;
			Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;
			Projectile.rotation = Projectile.velocity.ToRotation() + (Projectile.spriteDirection < 0 ? MathHelper.Pi : 0f);
			if (++Projectile.frameCounter >= 3)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
			}
			Projectile.alpha = Projectile.timeLeft < 20 ? (20 - Projectile.timeLeft) * 12 : 0;
			Lighting.AddLight(Projectile.Center, 0.3f, 0.22f, 0.05f);

			// gold motes shed off the wings - one every few ticks, so a fan of thirteen stays cheap
			if (!Main.dedServ && Projectile.alpha < 160 && (Projectile.timeLeft + Projectile.identity) % 5 == 0)
			{
				Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
				ParticleSystem.Spawn(new Spark(Projectile.Center + back * 6f + Main.rand.NextVector2Circular(4f, 4f),
					back * Main.rand.NextFloat(0.6f, 1.4f), SunPalette.Gold, 0.7f, Main.rand.Next(10, 16)));
			}
		}

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 3; i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.GoldCoin, Main.rand.NextVector2Circular(2f, 2f), 80, default, 0.9f);
			FX.Sparks(Projectile.Center, SunPalette.Gold, 6, 3.5f, 0.8f, 16);
			for (int i = 0; i < 4; i++)
				ParticleSystem.Spawn(new Chip(Projectile.Center, Main.rand.NextVector2Circular(3f, 3f) - new Vector2(0f, 1.5f),
					i % 2 == 0 ? new Color(255, 216, 96) : new Color(42, 79, 176), 0.9f, 40));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Rectangle frame = tex.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
			Rectangle other = tex.Frame(1, Main.projFrames[Type], 0, (Projectile.frame + 1) % Main.projFrames[Type]);
			Vector2 origin = frame.Size() * 0.5f;
			SpriteEffects fx = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			float fade = 1f - Projectile.alpha / 255f;
			Vector2 half = new Vector2(Projectile.width, Projectile.height) * 0.5f;
			float speed = MathHelper.Clamp(Projectile.velocity.Length() / MaxSpeed, 0f, 1f);

			// the trail: golden afterimages, reaching further back the faster it goes
			int len = Projectile.oldPos.Length;
			int reach = 2 + (int)(speed * (len - 2));
			for (int i = reach - 1; i >= 1; i--)
			{
				if (Projectile.oldPos[i] == Vector2.Zero)
					continue;
				float f = 1f - i / (float)len;
				Main.EntitySpriteDraw(tex, Projectile.oldPos[i] + half - Main.screenPosition, frame,
					SunPalette.Glow(SunPalette.Gold, 0.45f * f * fade), Projectile.rotation, origin, 1f - 0.07f * i, fx, 0);
			}

			Vector2 at = Projectile.Center - Main.screenPosition;
			RiftDraw.Bloom(Projectile.Center, SunPalette.Glow(SunPalette.Gold, 0.22f * fade), 0.4f);
			// the wing blur: the other wing frame ghosted, a touch larger, lit
			Main.EntitySpriteDraw(tex, at, other, SunPalette.Glow(SunPalette.Pale, 0.28f * fade), Projectile.rotation, origin, 1.06f, fx, 0);
			Main.EntitySpriteDraw(tex, at, frame, Color.Lerp(lightColor, Color.White, 0.4f) * fade,
				Projectile.rotation, origin, 1f, fx, 0);
			return false;
		}
	}
}
