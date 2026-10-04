using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Reaper;

namespace Terrapex.Content.Projectiles.Reaper
{
	/// <summary>
	/// Everything a pre-hardmode scythe throws: a soul, an icicle, a thorn, an ember, a fistful of
	/// sand, a cloud of rot. One projectile with a style table rather than six near-copies - they
	/// differ only in colour, dust, how they move and what they leave on the target, and a table is
	/// where that difference is easiest to read side by side.
	///
	/// ai[0] is the style. Drawn as a trail and a bloom from <see cref="RiftDraw"/>, so none of them
	/// needs a sprite of its own. The flare is 64 px, so a style's size is a fraction of that.
	/// </summary>
	public class ReapBolt : ModProjectile
	{
		public const int Soul = 0, Ice = 1, Thorn = 2, Fire = 3, Sand = 4, Rot = 5;

		/// <summary>ai[1]: thrown by an ordinary swing, so it banks Harvest like a wave. A Reap's bolts do not.</summary>
		public const float FromSwing = 1f;

		private readonly struct Style
		{
			public readonly Color Color;
			public readonly int Dust;
			public readonly int Debuff;
			public readonly int DebuffTicks;
			public readonly int Pierce;
			public readonly int Life;
			public readonly float Gravity;
			public readonly float Drag;
			public readonly float Size;

			public Style(Color color, int dust, int debuff, int debuffTicks, int pierce, int life,
				float gravity, float drag, float size)
			{
				Color = color; Dust = dust; Debuff = debuff; DebuffTicks = debuffTicks; Pierce = pierce;
				Life = life; Gravity = gravity; Drag = drag; Size = size;
			}
		}

		private static readonly Style[] Styles =
		{
			new(new Color(196, 236, 214), DustID.Wraith, 0, 0, 1, 150, 0f, 1f, 0.40f),            // soul: homes
			new(new Color(176, 232, 255), DustID.Ice, BuffID.Frostburn, 180, 2, 70, 0.10f, 1f, 0.32f),
			new(new Color(126, 200, 80), DustID.JungleGrass, BuffID.Poisoned, 240, 2, 50, 0f, 0.96f, 0.32f),
			new(new Color(255, 144, 64), DustID.Torch, BuffID.OnFire, 180, 4, 60, 0f, 0.99f, 0.55f),
			new(new Color(232, 200, 122), DustID.Sand, 0, 0, 2, 70, 0.20f, 1f, 0.36f),
			new(new Color(155, 111, 194), DustID.Demonite, 0, 0, -1, 150, 0f, 0.93f, 0.75f),       // rot: lingers
		};

		private Style S => Styles[(int)Projectile.ai[0] % Styles.Length];
		private ref float Timer => ref Projectile.localAI[0];

		public override string Texture => "Terrapex/Content/Projectiles/RiftFlare";

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 8;
			ProjectileID.Sets.TrailingMode[Type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = ModContent.GetInstance<ReapingDamage>();
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
			Projectile.timeLeft = 600;
		}

		public override void AI()
		{
			Style s = S;
			if (Timer == 0f)
			{
				Projectile.penetrate = s.Pierce;
				Projectile.timeLeft = s.Life;
				Projectile.scale = s.Size;
				if (Projectile.ai[0] == Rot)
				{
					Projectile.tileCollide = false;
					Projectile.localNPCHitCooldown = 30;
				}
			}
			Timer++;

			Projectile.velocity.Y += s.Gravity;
			Projectile.velocity *= s.Drag;

			if (Projectile.ai[0] == Soul && Timer > 12f)
			{
				NPC target = Projectile.FindTargetWithinRange(420f);
				if (target != null)
				{
					Vector2 want = Projectile.DirectionTo(target.Center) * 10f;
					Projectile.velocity = Vector2.Lerp(Projectile.velocity, want, 0.08f);
				}
			}

			Projectile.rotation = Projectile.velocity.ToRotation();
			Lighting.AddLight(Projectile.Center, s.Color.ToVector3() * 0.35f);
			if (Main.rand.NextBool(Projectile.ai[0] == Rot ? 2 : 3))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f) * Projectile.scale,
					s.Dust, Projectile.velocity * 0.1f, 120, default, 0.9f);
				d.noGravity = true;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.ai[1] == FromSwing)
				ReapingSlash.RangedHit(Projectile, target, hit);
			Style s = S;
			if (s.Debuff > 0)
				target.AddBuff(s.Debuff, s.DebuffTicks);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Style s = S;
			float fade = Projectile.ai[0] == Rot ? Projectile.timeLeft / (float)s.Life : 1f;
			RiftDraw.Trail(Projectile,
				f => new Color(s.Color.R, s.Color.G, s.Color.B, 0) * (f * 0.55f * fade),
				f => 0.5f + 0.5f * f);
			RiftDraw.Bloom(Projectile.Center, new Color(s.Color.R, s.Color.G, s.Color.B, 0) * (0.7f * fade),
				Projectile.scale);
			return false;
		}

		public override void OnKill(int timeLeft)
		{
			Style s = S;
			for (int i = 0; i < 8; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, s.Dust, Main.rand.NextVector2Circular(3f, 3f),
					110, default, 0.9f);
				d.noGravity = true;
			}
		}
	}
}
