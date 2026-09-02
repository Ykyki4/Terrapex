using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// Plan item #60's beam: held while the button is down, stopping at the first wall.
	/// It borrows the boss's own <c>RiftLaser</c> strip and the shared flare and ring, and
	/// stacks them the way vanilla's Last Prism does — haze, a moving body, a white core,
	/// a muzzle bloom and an impact that recycles. A beam that is one stretched quad reads
	/// as a stick no matter what colour it is; the movement is what makes it a beam.
	/// </summary>
	public class RiftflowBeam : ModProjectile
	{
		public override string Texture => "Terrapex/Content/Projectiles/RiftLaser";

		private const float MaxLength = 860f;
		private const float Width = 22f;
		private const int ManaEvery = 18;

		/// <summary>How far the beam actually reaches this tick, after the wall check.</summary>
		private ref float Length => ref Projectile.localAI[0];
		private ref float Charge => ref Projectile.ai[0];

		public override void SetDefaults()
		{
			Projectile.width = 18;
			Projectile.height = 18;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			// NOT hide: a hidden projectile is skipped by the draw loop entirely unless it is
			// pushed into a cache from DrawBehind, and PreDraw then never runs at all
			Projectile.hide = false;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 8;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];

			bool stop = owner.dead || !owner.active || owner.noItems || owner.CCed || !owner.channel;
			if (stop)
			{
				Projectile.Kill();
				return;
			}
			Projectile.timeLeft = 2;
			Charge++;

			if (Projectile.owner == Main.myPlayer)
			{
				Vector2 aim = Vector2.Normalize(Main.MouseWorld - owner.MountedCenter);
				if (aim.HasNaNs())
					aim = Vector2.UnitX * owner.direction;
				Projectile.velocity = aim;
				Projectile.netUpdate = true;

				// mana is spent while channelling, not at the swing
				if (Charge % ManaEvery == 0)
				{
					if (!owner.CheckMana(owner.HeldItem, -1, true))
					{
						Projectile.Kill();
						return;
					}
				}
			}

			Projectile.Center = owner.MountedCenter + Projectile.velocity * 26f;
			Projectile.rotation = Projectile.velocity.ToRotation();
			owner.heldProj = Projectile.whoAmI;
			owner.itemTime = owner.itemAnimation = 2;
			owner.ChangeDir(Projectile.velocity.X > 0f ? 1 : -1);
			owner.itemRotation = MathHelper.WrapAngle(Projectile.velocity.ToRotation() + (owner.direction < 0 ? MathHelper.Pi : 0f));

			// stop at the first wall, sampled coarsely — a per-pixel walk here is what makes
			// homebrew beams cost more than the rest of the fight
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

			// lit down its whole length, not only where it lands
			for (float d = 40f; d < Length; d += 90f)
				Lighting.AddLight(owner.MountedCenter + dir * d, 0.34f, 0.12f, 0.46f);
			Lighting.AddLight(tip, 0.9f, 0.35f, 1.1f);

			// impact spray thrown back along the beam, so the far end reads as landing on
			// something rather than stopping in mid-air
			for (int i = 0; i < 3; i++)
			{
				Vector2 kick = (-dir).RotatedByRandom(0.95f) * Main.rand.NextFloat(1.6f, 5.2f);
				Dust d2 = Dust.NewDustPerfect(tip + Main.rand.NextVector2Circular(6f, 6f),
					DustID.PurpleTorch, kick, 90, default, Main.rand.NextFloat(0.9f, 1.5f));
				d2.noGravity = true;
			}

			// a few motes peeling sideways off the shaft
			if (Main.rand.NextBool(2))
			{
				float at = Main.rand.NextFloat(30f, Length);
				Vector2 side = dir.RotatedBy(MathHelper.PiOver2 * (Main.rand.NextBool() ? 1f : -1f));
				Dust d3 = Dust.NewDustPerfect(owner.MountedCenter + dir * at + Main.rand.NextVector2Circular(7f, 7f),
					DustID.PurpleTorch, side * Main.rand.NextFloat(0.4f, 1.3f), 150, default, 0.8f);
				d3.noGravity = true;
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

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
			=> target.AddBuff(ModContent.BuffType<Cracked>(), 180);

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
			float breathe = 1f + 0.10f * (float)System.Math.Sin(Charge * 0.21f);
			float w = Width * fade * breathe;

			// the body drifts between two rift tones rather than sitting on one flat colour
			float shift = 0.5f + 0.5f * (float)System.Math.Sin(Charge * 0.06f);
			Color body = Color.Lerp(new Color(168, 62, 240), new Color(226, 108, 214), shift);

			// PointWrap is the whole trick: with a source rectangle taller than the texture the
			// strip tiles down the beam, and the taper already baked into RiftLaser then reads
			// as pulses running outward instead of one motionless bar
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			// haze, drawn twice a hair off axis so the edge shimmers instead of standing still
			float jitter = 0.010f * (float)System.Math.Sin(Charge * 0.9f);
			Stretch(beam, start, rot + jitter, origin, w * 2.6f, new Color(84, 26, 150) * (0.26f * fade));
			Stretch(beam, start, rot - jitter, origin, w * 1.9f, new Color(112, 34, 180) * (0.26f * fade));

			// the layer that actually moves
			int scroll = -(int)(Charge * 6f) % beam.Height;
			Rectangle src = new Rectangle(0, scroll, beam.Width, (int)Length);
			Main.EntitySpriteDraw(beam, start, src, body * (0.60f * fade), rot, origin,
				new Vector2(w * 1.25f / beam.Width, 1f), SpriteEffects.None, 0);

			Stretch(beam, start, rot, origin, w, body * (0.70f * fade));
			Stretch(beam, start, rot, origin, w * 0.34f, Color.White * (0.95f * fade));

			float pop = 1f + 0.14f * (float)System.Math.Sin(Charge * 0.31f);
			float mz = w / 22f;

			// muzzle: two blooms turning against each other, so it churns
			Main.EntitySpriteDraw(flare, start, null, new Color(190, 80, 235) * (0.75f * fade),
				Charge * 0.05f, flare.Size() * 0.5f, mz * 1.55f * pop, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(flare, start, null, Color.White * (0.60f * fade),
				-Charge * 0.08f, flare.Size() * 0.5f, mz * 0.75f * pop, SpriteEffects.None, 0);

			// the opening flash, gone by the time the beam reaches full width
			float open = 1f - Utils.GetLerpValue(0f, 12f, Charge, true);
			if (open > 0f)
				Main.EntitySpriteDraw(flare, start, null, Color.White * (0.9f * open), 0f,
					flare.Size() * 0.5f, mz * (0.6f + 2.6f * open), SpriteEffects.None, 0);

			// impact bloom plus two rings on a recycling phase — the rings are what sell the
			// far end as a point of contact
			Main.EntitySpriteDraw(flare, tip, null, body * (0.85f * fade),
				-Charge * 0.06f, flare.Size() * 0.5f, mz * 1.8f * pop, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(flare, tip, null, Color.White * (0.70f * fade),
				Charge * 0.11f, flare.Size() * 0.5f, mz * 0.85f * pop, SpriteEffects.None, 0);
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
