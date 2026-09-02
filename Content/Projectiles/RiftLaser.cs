using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// A beam anchored to the Keeper. Spends its first second as a thin harmless line so the
	/// sweep can be read, then widens and starts cutting.
	///
	/// Several of these fire at once (four in a cross, three or four as rotors), so the beam is
	/// deliberately built to survive overlap: it starts a full core-radius away from the Keeper
	/// and its texture fades in over the first stretch, which keeps the shared origin from
	/// stacking into a white blob. Drawing is additive in three layers — wide haze, body, hot
	/// core — so crossings read as light rather than as flat quads on top of each other.
	/// </summary>
	public class RiftLaser : ModProjectile
	{
		public const int TelegraphTime = 46;
		private const int WidenTime = 10;
		private const float Length = 1500f;
		private const float FullWidth = 26f;
		private const float StartOffset = 42f;   // clear of the 96 px core sprite

		private ref float ParentIndex => ref Projectile.ai[0];
		private ref float AngularVelocity => ref Projectile.ai[1];
		private ref float Timer => ref Projectile.localAI[0];

		private bool Firing => Timer >= TelegraphTime;

		// Projectile.scale is a width/offset multiplier: 1 for the Keeper's 96 px core,
		// smaller for anything with a smaller body firing the same beam.
		private Vector2 Origin => Projectile.Center + Projectile.rotation.ToRotationVector2() * StartOffset * Projectile.scale;

		/// <summary>1 while the beam is alive, ramping down over its last frames.</summary>
		private float FadeOut => MathHelper.Clamp((Projectile.timeLeft - 8f) / 14f, 0f, 1f);

		private float CurrentWidth
		{
			get
			{
				if (!Firing)
					return 2.5f * Projectile.scale;
				float t = MathHelper.Clamp((Timer - TelegraphTime) / WidenTime, 0f, 1f);
				// a slow breathing pulse so a long sweep never looks like a static bar
				float pulse = 1f + 0.06f * (float)System.Math.Sin(Timer * 0.18f);
				return FullWidth * t * FadeOut * pulse * Projectile.scale;
			}
		}

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 240;
			Projectile.aiStyle = -1;
			Projectile.penetrate = -1;
			Projectile.hide = false;
		}

		public override void AI()
		{
			int p = (int)ParentIndex;
			if (p < 0 || p >= Main.maxNPCs || !Main.npc[p].active)
			{
				Projectile.Kill();
				return;
			}

			NPC parent = Main.npc[p];
			Projectile.Center = parent.Center;

			if (Timer == 0f)
				Projectile.rotation = Projectile.velocity.ToRotation();

			Projectile.velocity = Vector2.Zero;
			Projectile.rotation += AngularVelocity;
			Timer++;

			if (Timer == TelegraphTime)
			{
				SoundEngine.PlaySound(SoundID.Item122 with { Pitch = -0.4f }, Projectile.Center);
				SoundEngine.PlaySound(SoundID.Item14 with { Pitch = 0.6f, Volume = 0.5f }, Projectile.Center);
			}

			Vector2 dir = Projectile.rotation.ToRotationVector2();
			Vector2 side = dir.RotatedBy(MathHelper.PiOver2);
			Vector2 origin = Origin;

			if (!Firing)
			{
				// charge tell: motes rush down the aiming line into the muzzle
				if (Main.rand.NextBool(2))
				{
					float dist = Main.rand.NextFloat(90f, 520f);
					Dust d = Dust.NewDustPerfect(origin + dir * dist + side * Main.rand.NextFloat(-14f, 14f),
						DustID.PurpleTorch, -dir * Main.rand.NextFloat(6f, 11f), 90, default, 1.1f);
					d.noGravity = true;
				}
				Lighting.AddLight(origin, 0.35f, 0.1f, 0.5f);
				return;
			}

			// sparks kicked sideways off the beam, plus light so it reads against bright tiles
			for (int i = 0; i < 4; i++)
			{
				float dist = Main.rand.NextFloat(StartOffset, Length);
				Vector2 spot = origin + dir * dist;
				Dust d = Dust.NewDustPerfect(spot + side * Main.rand.NextFloat(-5f, 5f),
					Main.rand.NextBool(3) ? DustID.PinkTorch : DustID.PurpleTorch,
					side * Main.rand.NextFloat(-3.5f, 3.5f) + dir * Main.rand.NextFloat(-1f, 1f),
					100, default, Main.rand.NextFloat(0.9f, 1.5f));
				d.noGravity = true;
			}

			// a denser burst right at the muzzle sells the beam as coming out of the Keeper
			if (Main.rand.NextBool(2))
			{
				Dust d = Dust.NewDustPerfect(origin + Main.rand.NextVector2Circular(10f, 10f),
					DustID.PinkTorch, dir * Main.rand.NextFloat(2f, 6f), 80, default, 1.6f);
				d.noGravity = true;
			}

			for (float d2 = 0f; d2 < Length; d2 += 55f)
				Lighting.AddLight(origin + dir * d2, 0.75f, 0.22f, 0.95f);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (!Firing || CurrentWidth < 6f)
				return false;

			float point = 0f;
			Vector2 start = Origin;
			Vector2 end = start + Projectile.rotation.ToRotationVector2() * Length;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
				start, end, CurrentWidth * 0.75f, ref point);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			var tex = TextureAssets.Projectile[Type].Value;
			var flare = ModContent.Request<Texture2D>("Terrapex/Content/Projectiles/RiftFlare").Value;

			Vector2 draw = Origin - Main.screenPosition;
			float rot = Projectile.rotation - MathHelper.PiOver2;
			Vector2 texOrigin = new Vector2(tex.Width * 0.5f, 0f);
			float width = CurrentWidth;
			float fade = FadeOut;

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			if (!Firing)
			{
				// telegraph: a thin flickering thread plus a charging bead at the muzzle
				float t = Timer / TelegraphTime;
				float flick = 0.45f + 0.25f * (float)System.Math.Sin(Timer * 0.55f) + 0.35f * t;
				DrawLayer(tex, draw, rot, texOrigin, 3.2f, new Color(180, 90, 255) * (flick * 0.55f));
				DrawLayer(tex, draw, rot, texOrigin, 1.4f, Color.White * (flick * 0.7f));

				float bead = MathHelper.Lerp(0.15f, 0.75f, t * t);
				Main.EntitySpriteDraw(flare, draw, null, new Color(210, 120, 255) * (0.35f + 0.55f * t),
					0f, flare.Size() * 0.5f, bead, SpriteEffects.None, 0);
			}
			else
			{
				// three stacked layers: wide haze, coloured body, white-hot core
				DrawLayer(tex, draw, rot, texOrigin, width * 2.3f, new Color(96, 30, 170) * (0.30f * fade));
				DrawLayer(tex, draw, rot, texOrigin, width * 1.35f, new Color(168, 62, 240) * (0.55f * fade));
				DrawLayer(tex, draw, rot, texOrigin, width, new Color(226, 152, 255) * (0.95f * fade));
				DrawLayer(tex, draw, rot, texOrigin, width * 0.38f, Color.White * fade);

				float pop = 1f + 0.10f * (float)System.Math.Sin(Timer * 0.3f);
				Main.EntitySpriteDraw(flare, draw, null, new Color(240, 180, 255) * (0.85f * fade),
					0f, flare.Size() * 0.5f, width / 26f * 1.5f * pop, SpriteEffects.None, 0);
				Main.EntitySpriteDraw(flare, draw, null, Color.White * (0.55f * fade),
					0f, flare.Size() * 0.5f, width / 26f * 0.7f * pop, SpriteEffects.None, 0);
			}

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
				DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			return false;
		}

		private static void DrawLayer(Texture2D tex, Vector2 draw, float rot, Vector2 origin, float width, Color color)
		{
			if (width <= 0.1f)
				return;
			Vector2 scale = new Vector2(width / tex.Width, Length / tex.Height);
			Main.EntitySpriteDraw(tex, draw, null, color, rot, origin, scale, SpriteEffects.None, 0);
		}

		public override bool ShouldUpdatePosition() => false;
	}
}
