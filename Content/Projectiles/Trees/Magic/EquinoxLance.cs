using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Projectiles.Trees.Magic
{
	/// <summary>
	/// The Riftflow inside the Equinox: what a full charge adds on top of the page. A third of a
	/// second of the Riftflow's beam, down the aim, stopped by the first wall - not channelled,
	/// a lance. Built the way <c>RiftflowBeam</c> is, because a beam that is one coloured quad
	/// reads as a stick: haze a hair off axis, a tiled moving body, a flat body, a white core,
	/// a muzzle bloom and an impact bloom with a ring.
	///
	/// It follows the player for its short life but keeps the direction it was fired in, so it is
	/// aimed once, at release, like every other cast of the book.
	/// </summary>
	public class EquinoxLance : ModProjectile
	{
		private const int Life = 22;
		private const float MaxLength = 560f;
		private const float Width = 16f;

		public override string Texture => "Terrapex/Content/Projectiles/RiftLaser";

		private float Length
		{
			get => Projectile.localAI[0];
			set => Projectile.localAI[0] = value;
		}

		private int Age => Life - Projectile.timeLeft;

		public override void SetDefaults()
		{
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Life;
			Projectile.hide = false;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override bool ShouldUpdatePosition() => false;

		/// <summary>0 -> 1 -> 0 over the life: snaps open, holds, thins out.</summary>
		private float Envelope
		{
			get
			{
				float open = Utils.GetLerpValue(0f, 4f, Age, true);
				float close = Utils.GetLerpValue(Life, Life - 10f, Age, true);
				return open * close;
			}
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead)
			{
				Projectile.Kill();
				return;
			}
			Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX);
			Projectile.Center = owner.MountedCenter;
			Projectile.rotation = Projectile.velocity.ToRotation();

			Length = MaxLength;
			for (float d = 24f; d < MaxLength; d += 8f)
			{
				if (!Collision.CanHitLine(owner.MountedCenter, 1, 1, owner.MountedCenter + Projectile.velocity * d, 1, 1))
				{
					Length = d;
					break;
				}
			}

			Vector2 tip = owner.MountedCenter + Projectile.velocity * Length;
			for (float d = 40f; d < Length; d += 90f)
				Lighting.AddLight(owner.MountedCenter + Projectile.velocity * d, 0.34f, 0.12f, 0.46f);
			Lighting.AddLight(tip, 0.9f, 0.35f, 1.1f);

			if (Age < 12)
			{
				for (int i = 0; i < 2; i++)
				{
					Vector2 kick = (-Projectile.velocity).RotatedByRandom(0.9f) * Main.rand.NextFloat(1.5f, 5f);
					Dust d2 = Dust.NewDustPerfect(tip + Main.rand.NextVector2Circular(6f, 6f),
						DustID.PurpleTorch, kick, 90, default, Main.rand.NextFloat(0.9f, 1.4f));
					d2.noGravity = true;
				}
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (Age < 2)
				return false;
			Player owner = Main.player[Projectile.owner];
			float _ = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
				owner.MountedCenter, owner.MountedCenter + Projectile.velocity * Length, Width, ref _);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (Length <= 1f)
				return false;
			Player owner = Main.player[Projectile.owner];
			Texture2D beam = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
			Texture2D flare = ModContent.Request<Texture2D>("Terrapex/Content/Projectiles/RiftFlare").Value;
			Texture2D ring = ModContent.Request<Texture2D>("Terrapex/Content/Projectiles/RiftRing").Value;

			Vector2 dir = Projectile.velocity;
			Vector2 start = owner.MountedCenter - Main.screenPosition;
			Vector2 tip = start + dir * Length;
			float rot = dir.ToRotation() - MathHelper.PiOver2;
			Vector2 origin = new Vector2(beam.Width * 0.5f, 0f);
			float env = Envelope;
			float w = Width * env;
			Color body = Color.Lerp(new Color(168, 62, 240), new Color(226, 108, 214), 0.5f + 0.5f * (float)Math.Sin(Age * 0.4f));

			// inside an explicit Additive batch, so colours are scaled, never A = 0
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			float jitter = 0.012f * (float)Math.Sin(Age * 1.3f);
			Stretch(beam, start, rot + jitter, origin, w * 2.6f, new Color(84, 26, 150) * (0.28f * env));
			Stretch(beam, start, rot - jitter, origin, w * 1.9f, new Color(112, 34, 180) * (0.28f * env));

			int scroll = -(Age * 9) % beam.Height;
			Main.EntitySpriteDraw(beam, start, new Rectangle(0, scroll, beam.Width, (int)Length), body * (0.6f * env),
				rot, origin, new Vector2(w * 1.25f / beam.Width, 1f), SpriteEffects.None, 0);
			Stretch(beam, start, rot, origin, w, body * (0.7f * env));
			Stretch(beam, start, rot, origin, w * 0.34f, Color.White * (0.95f * env));

			float mz = Width / 22f;
			Main.EntitySpriteDraw(flare, start, null, new Color(190, 80, 235) * (0.7f * env), Age * 0.05f,
				flare.Size() * 0.5f, mz * 1.5f, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(flare, tip, null, body * (0.85f * env), -Age * 0.06f,
				flare.Size() * 0.5f, mz * 1.8f, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(flare, tip, null, Color.White * (0.7f * env), Age * 0.11f,
				flare.Size() * 0.5f, mz * 0.85f, SpriteEffects.None, 0);
			float ph = Age / (float)Life;
			Main.EntitySpriteDraw(ring, tip, null, body * ((1f - ph) * 0.6f), 0f, ring.Size() * 0.5f,
				mz * (0.35f + ph * 1.6f), SpriteEffects.None, 0);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
			return false;
		}

		private void Stretch(Texture2D tex, Vector2 at, float rot, Vector2 origin, float width, Color color)
		{
			if (width <= 0.1f)
				return;
			Main.EntitySpriteDraw(tex, at, null, color, rot, origin,
				new Vector2(width / tex.Width, Length / tex.Height), SpriteEffects.None, 0);
		}
	}
}
