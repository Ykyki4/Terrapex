using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Particles;

namespace Terrapex.Content.Projectiles.Trees.Summon
{
	/// <summary>
	/// The hatchling's spit, and the Ark's volley bolt. <c>ai[0]</c> is the kind (0 a spit that
	/// arcs under its own weight, 1 a volley bolt that flies flat and bends gently toward the
	/// nearest enemy - a minion's shot, not a boss's, so the no-homing rule does not apply), and
	/// <c>ai[1]</c> is the form of the beast that fired it, which picks the colour.
	/// </summary>
	public class MenagerieSpit : ModProjectile
	{
		private bool Volley => Projectile.ai[0] == 1f;
		private Color Tint => MenagerieBeast.FormColor[(int)Projectile.ai[1] & 3];

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.MinionShot[Type] = true;
			ProjectileID.Sets.TrailCacheLength[Type] = 7;
			ProjectileID.Sets.TrailingMode[Type] = 0;
		}

		public override void SetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 100;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
		}

		public override void AI()
		{
			if (Volley)
			{
				NPC best = null;
				float bd = 420f;
				foreach (NPC n in Main.ActiveNPCs)
				{
					if (!n.CanBeChasedBy(Projectile))
						continue;
					float d = Projectile.Distance(n.Center);
					if (d < bd)
					{
						bd = d;
						best = n;
					}
				}
				if (best != null)
				{
					float speed = Projectile.velocity.Length();
					Vector2 want = (best.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * speed;
					Projectile.velocity = Vector2.Lerp(Projectile.velocity, want, 0.06f).SafeNormalize(Vector2.UnitX) * speed;
				}
			}
			else if (Projectile.timeLeft < 88)
				Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.12f, 10f);

			Projectile.rotation = Projectile.velocity.ToRotation();
			Lighting.AddLight(Projectile.Center, Tint.ToVector3() * 0.35f);
			if (Main.rand.NextBool(4))
				ParticleSystem.Spawn(new Spark(Projectile.Center, -Projectile.velocity * 0.08f, new Color(Tint.R, Tint.G, Tint.B, 0), 0.6f, 10));
		}

		public override void OnKill(int timeLeft)
		{
			FX.Sparks(Projectile.Center, new Color(Tint.R, Tint.G, Tint.B, 0), 6, 3f, 0.7f, 14);
			ParticleSystem.Spawn(new Bloom(Projectile.Center, new Color(Tint.R, Tint.G, Tint.B, 0) * 0.8f, 0.2f, 0.6f, 10));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Color t = Tint;
			float scale = Volley ? 1.15f : 1f;
			RiftDraw.Trail(Projectile, f => new Color(t.R, t.G, t.B, 0) * (0.55f * f), f => scale * (0.45f + 0.55f * f));
			RiftDraw.Bloom(Projectile.Center, new Color(t.R, t.G, t.B, 0) * 0.35f, 0.18f * scale);
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			// the spit keeps its own pink; a volley bolt is washed in its beast's colour
			Color body = Volley ? Color.Lerp(Color.White, t, 0.5f) : Color.Lerp(lightColor, Color.White, 0.6f);
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, body, Projectile.rotation,
				tex.Size() * 0.5f, scale, SpriteEffects.None, 0);
			return false;
		}
	}

	/// <summary>
	/// The Grove Conclave's simultaneous strike: one hit on each beast's own target, in the tick
	/// the pack trades places. A flash and a ring in the beast's colour, so four of them going off
	/// together read as one beat. <c>ai[0]</c> is the form.
	/// </summary>
	public class ConclaveStrike : ModProjectile
	{
		private const int Life = 14;

		public override string Texture => "Terrapex/Content/Projectiles/RiftFlare";

		private Color Tint => MenagerieBeast.FormColor[(int)Projectile.ai[0] & 3];

		public override void SetStaticDefaults() => ProjectileID.Sets.MinionShot[Type] = true;

		public override void SetDefaults()
		{
			Projectile.width = 44;
			Projectile.height = 44;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		// one beat, not a lingering field: it can only land in its first few ticks
		public override bool? CanDamage() => Projectile.timeLeft > Life - 4 ? null : false;

		public override void AI()
		{
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				Color t = Tint;
				FX.Impact(Projectile.Center, new Color(t.R, t.G, t.B, 0), new Color(255, 215, 154, 0), 0.8f);
			}
			Lighting.AddLight(Projectile.Center, Tint.ToVector3() * 0.6f * Projectile.timeLeft / Life);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float f = 1f - Projectile.timeLeft / (float)Life;
			Color t = Tint;
			RiftDraw.Bloom(Projectile.Center, new Color(t.R, t.G, t.B, 0) * (1f - f), 0.5f + 0.4f * f);
			RiftDraw.Ring(Projectile.Center, new Color(t.R, t.G, t.B, 0) * (0.8f * (1f - f)), 0.15f + 0.45f * f);
			return false;
		}
	}
}
