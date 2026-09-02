using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// Plan item #115's beam. The boss's own unblinking stare handed over, and it keeps the
	/// property that made it frightening: it is worth more the longer it stays on one thing.
	///
	/// Damage climbs from 1x to <see cref="MaxRamp"/>x over four seconds of contact and drops
	/// the instant the beam leaves — so the weapon is priced in *holding an aim*, not in
	/// clicking, and it is deliberately poor at sweeping a crowd. Everything else about the
	/// stack is <see cref="RiftflowBeam"/>'s, which is the mod's reference build for a beam:
	/// haze, a tiled body that scrolls, a flat body, a white core, a churning muzzle.
	/// </summary>
	public class UnblinkingBeam : ModProjectile
	{
		public override string Texture => "Terrapex/Content/Projectiles/RiftLaser";

		private const float MaxLength = 1000f;
		private const float Width = 20f;
		public const float MaxRamp = 2.5f;
		private const int RampTime = 60 * 4;

		private ref float Length => ref Projectile.localAI[0];
		private ref float Charge => ref Projectile.ai[0];
		/// <summary>Ticks of unbroken contact. This is the whole weapon.</summary>
		private ref float Held => ref Projectile.ai[1];

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.hide = false;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 6;
		}

		private float Ramp => 1f + (MaxRamp - 1f) * MathHelper.Clamp(Held / RampTime, 0f, 1f);

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (owner.dead || !owner.active || owner.noItems || owner.CCed || !owner.channel)
			{
				Projectile.Kill();
				return;
			}
			Projectile.timeLeft = 2;
			Charge++;

			// the ramp bleeds three times faster than a hit fills it, so letting go is cheap and
			// keeping the aim is the expensive thing - which is the only way round that makes it
			// a skill rather than a free multiplier for turning the weapon on
			Held = Math.Max(0f, Held - 3f);

			if (Projectile.owner == Main.myPlayer)
			{
				Vector2 aim = Vector2.Normalize(Main.MouseWorld - owner.MountedCenter);
				if (aim.HasNaNs())
					aim = Vector2.UnitX * owner.direction;
				Projectile.velocity = aim;
				Projectile.netUpdate = true;
			}

			Projectile.Center = owner.MountedCenter + Projectile.velocity * 26f;
			Projectile.rotation = Projectile.velocity.ToRotation();
			owner.heldProj = Projectile.whoAmI;
			owner.itemTime = owner.itemAnimation = 2;
			owner.ChangeDir(Projectile.velocity.X > 0f ? 1 : -1);
			owner.itemRotation = MathHelper.WrapAngle(Projectile.velocity.ToRotation()
				+ (owner.direction < 0 ? MathHelper.Pi : 0f));

			Length = MaxLength;
			for (float d = 26f; d < MaxLength; d += 8f)
			{
				Vector2 at = owner.MountedCenter + Projectile.velocity * d;
				if (!Collision.CanHitLine(owner.MountedCenter, 1, 1, at, 1, 1))
				{
					Length = d;
					break;
				}
			}

			Vector2 dir = Projectile.velocity;
			Vector2 tip = owner.MountedCenter + dir * Length;
			for (float d = 40f; d < Length; d += 100f)
				Lighting.AddLight(owner.MountedCenter + dir * d, 0.5f, 0.5f, 0.55f);
			Lighting.AddLight(tip, 1.0f, 1.0f, 1.1f);

			for (int i = 0; i < 2; i++)
			{
				Vector2 kick = (-dir).RotatedByRandom(0.95f) * Main.rand.NextFloat(1.4f, 4.8f);
				Dust d2 = Dust.NewDustPerfect(tip + Main.rand.NextVector2Circular(6f, 6f),
					DustID.WhiteTorch, kick, 90, default, Main.rand.NextFloat(0.8f, 1.4f));
				d2.noGravity = true;
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Player owner = Main.player[Projectile.owner];
			float _ = 0f;
			Vector2 start = owner.MountedCenter;
			Vector2 end = start + Projectile.velocity * Length;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
				start, end, Width, ref _);
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
			=> modifiers.FinalDamage *= Ramp;

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
			=> Held = Math.Min(RampTime, Held + 24f);

		public override bool PreDraw(ref Color lightColor)
		{
			if (Length <= 1f)
				return false;

			Player owner = Main.player[Projectile.owner];
			Texture2D beam = TextureAssets.Projectile[Type].Value;
			Texture2D flare = ModContent.Request<Texture2D>("Terrapex/Content/Projectiles/RiftFlare").Value;
			Texture2D ring = ModContent.Request<Texture2D>("Terrapex/Content/Projectiles/RiftRing").Value;

			Vector2 dir = Projectile.velocity;
			Vector2 start = owner.MountedCenter - Main.screenPosition;
			Vector2 tip = start + dir * Length;
			float rot = dir.ToRotation() - MathHelper.PiOver2;
			Vector2 origin = new Vector2(beam.Width * 0.5f, 0f);

			float fade = Utils.GetLerpValue(0f, 10f, Charge, true);
			float heat = MathHelper.Clamp(Held / RampTime, 0f, 1f);
			// the beam visibly fattens and whitens as the ramp fills: the number is on screen
			// without a number being on screen
			float w = Width * fade * (0.8f + heat * 0.55f) * (1f + 0.08f * (float)Math.Sin(Charge * 0.22f));
			Color body = Color.Lerp(new Color(150, 158, 185), new Color(245, 248, 255), heat);

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			float jitter = 0.009f * (float)Math.Sin(Charge * 0.9f);
			Stretch(beam, start, rot + jitter, origin, w * 2.5f, new Color(28, 30, 44) * (0.30f * fade));
			Stretch(beam, start, rot - jitter, origin, w * 1.8f, new Color(64, 68, 92) * (0.30f * fade));

			int scroll = -(int)(Charge * (5f + heat * 6f)) % beam.Height;
			Rectangle src = new Rectangle(0, scroll, beam.Width, (int)Length);
			Main.EntitySpriteDraw(beam, start, src, body * (0.60f * fade), rot, origin,
				new Vector2(w * 1.25f / beam.Width, 1f), SpriteEffects.None, 0);

			Stretch(beam, start, rot, origin, w, body * (0.70f * fade));
			Stretch(beam, start, rot, origin, w * 0.30f, Color.White * (0.95f * fade));

			float mz = w / 20f;
			float pop = 1f + 0.14f * (float)Math.Sin(Charge * 0.31f);
			Main.EntitySpriteDraw(flare, start, null, body * (0.8f * fade), Charge * 0.05f,
				flare.Size() * 0.5f, mz * 1.5f * pop, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(flare, start, null, Color.White * (0.6f * fade), -Charge * 0.08f,
				flare.Size() * 0.5f, mz * 0.7f * pop, SpriteEffects.None, 0);

			Main.EntitySpriteDraw(flare, tip, null, body * (0.85f * fade), -Charge * 0.06f,
				flare.Size() * 0.5f, mz * 1.8f * pop, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(flare, tip, null, Color.White * (0.7f * fade), Charge * 0.11f,
				flare.Size() * 0.5f, mz * 0.85f * pop, SpriteEffects.None, 0);
			for (int i = 0; i < 2; i++)
			{
				float ph = ((Charge + i * 14f) % 28f) / 28f;
				Main.EntitySpriteDraw(ring, tip, null, body * ((1f - ph) * 0.55f * fade),
					Charge * 0.02f, ring.Size() * 0.5f, mz * (0.35f + ph * 1.5f), SpriteEffects.None, 0);
			}

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
