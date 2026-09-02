using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// The Weaver's thread. It is not a projectile that travels — it is a line strung between
	/// two points, and the whole fight is about where those lines are.
	///
	/// It spends its first 40 ticks thin and completely harmless while it draws tight. That
	/// telegraph is the reason the fight is readable at all: a line hazard that appeared at
	/// full strength would kill people who were already standing in it through no fault of
	/// their own. It also fades out over its last 24 ticks, so a strand never simply blinks
	/// off the screen — you can see the web coming apart in time to move into it.
	///
	/// Either end may be an NPC, a player, or a fixed world point. Both ends fixed is what
	/// makes an actual web possible: the rings of the lattice hang between points in the air
	/// with nothing standing at the corners.
	/// </summary>
	public class RiftThread : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_1";

		public const int Telegraph = 40;
		private const int FadeOut = 24;
		private const float Width = 11f;
		private const int Segments = 14;

		/// <summary>Endpoint handles: >= 0 is an NPC index, negative is -(player + 1).</summary>
		private ref float EndA => ref Projectile.ai[0];
		private ref float EndB => ref Projectile.ai[1];
		private ref float Age => ref Projectile.localAI[0];

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.aiStyle = -1;
			Projectile.friendly = false;
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 60 * 60;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.netImportant = true;
		}

		/// <summary>Encodes a player as an endpoint handle.</summary>
		public static float Player(int who) => -(who + 1);

		/// <summary>
		/// Endpoint sentinel: the end is a fixed world point rather than something that moves.
		/// End B's point lives in <c>Projectile.velocity</c>, end A's in <c>Projectile.Center</c>
		/// — both of which are already synced, which <c>localAI</c> is not.
		///
		/// Phase three needs threads pinned to where the player *was*; anchoring one to the
		/// player themselves would simply sit on them and tick damage forever.
		/// </summary>
		public const float Pinned = -9999f;

		private static bool IsPinned(float handle) => handle <= Pinned + 1f;

		public override bool ShouldUpdatePosition() => false;

		/// <summary>Strings a strand, in the one place that knows how the handles are packed.</summary>
		public static Projectile Spawn(NPC source, Vector2 a, Vector2 b, float endA, float endB,
			int damage, int life)
		{
			Projectile p = Projectile.NewProjectileDirect(source.GetSource_FromAI(), a, b,
				ModContent.ProjectileType<RiftThread>(), damage, 0f, Main.myPlayer, endA, endB);
			p.Center = a;
			p.velocity = b;
			p.timeLeft = life;
			p.netUpdate = true;
			return p;
		}

		private static bool Resolve(float handle, out Vector2 at)
		{
			at = Vector2.Zero;
			if (handle >= 0f)
			{
				int i = (int)handle;
				if (i < 0 || i >= Main.maxNPCs || !Main.npc[i].active)
					return false;
				at = Main.npc[i].Center;
				return true;
			}

			int p = (int)(-handle) - 1;
			if (p < 0 || p >= Main.maxPlayers || !Main.player[p].active || Main.player[p].dead)
				return false;
			at = Main.player[p].MountedCenter;
			return true;
		}

		private bool Ends(out Vector2 a, out Vector2 b)
		{
			a = Projectile.Center;
			b = Projectile.velocity;
			bool okA = IsPinned(EndA) || Resolve(EndA, out a);
			bool okB = IsPinned(EndB) || Resolve(EndB, out b);
			return okA && okB;
		}

		/// <summary>1 while taut, ramping in over the telegraph and out over the last ticks.</summary>
		private float Strength
		{
			get
			{
				if (Age < Telegraph)
					return 0f;
				return Projectile.timeLeft < FadeOut ? Projectile.timeLeft / (float)FadeOut : 1f;
			}
		}

		public override void AI()
		{
			Age++;
			if (!Ends(out Vector2 a, out Vector2 b))
			{
				Snap(Projectile.Center);
				Projectile.Kill();
				return;
			}

			if (Age == Telegraph)
			{
				// the moment it goes live gets its own sound and its own flash at both ends
				SoundEngine.PlaySound(SoundID.Item112 with { Volume = 0.45f, Pitch = 0.4f },
					Vector2.Lerp(a, b, 0.5f));
				Snap(a, 6);
				Snap(b, 6);
			}

			if (Age < Telegraph)
				return;

			Lighting.AddLight(Vector2.Lerp(a, b, 0.5f), 0.24f, 0.55f, 0.5f);
			if (Main.rand.NextBool(3))
			{
				Vector2 at = Vector2.Lerp(a, b, Main.rand.NextFloat());
				Dust d = Dust.NewDustPerfect(at, DustID.Vortex, Vector2.Zero, 120, default, 0.9f);
				d.noGravity = true;
				d.velocity *= 0.2f;
			}
		}

		private static void Snap(Vector2 at, int count = 10)
		{
			for (int i = 0; i < count; i++)
			{
				Dust d = Dust.NewDustPerfect(at, DustID.Vortex,
					Main.rand.NextVector2Circular(3f, 3f), 110, default, 1f);
				d.noGravity = true;
			}
		}

		/// <summary>Harmless until it is taut. Everything else in the fight depends on this.</summary>
		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (Strength < 0.35f || !Ends(out Vector2 a, out Vector2 b))
				return false;

			float point = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
				a, b, Width, ref point);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (!Ends(out Vector2 a, out Vector2 b))
				return false;
			if (Vector2.DistanceSquared(a, b) < 16f)
				return false;

			float draw = MathHelper.Clamp(Age / Telegraph, 0f, 1f);
			float live = Strength;
			float len = Vector2.Distance(a, b);

			// it visibly tightens: the belly closes and the strand thickens as the telegraph runs
			float sag = MathHelper.Clamp(len * 0.16f, 4f, 46f) * (1f - draw * 0.86f);

			if (live <= 0f)
			{
				// drawing tight — thin, dim, and beaded, so it reads as a threat that has not
				// landed yet rather than as a weak version of the real thing
				Color faint = RiftDraw.Glow(50, 150, 150, 0.30f + draw * 0.45f);
				RiftDraw.Silk(a, b, faint, default, 1.6f, sag, -1f, Segments);
				for (int k = 0; k < 2; k++)
				{
					float t = k == 0 ? draw * 0.5f : 1f - draw * 0.5f;
					RiftDraw.Bloom(Vector2.Lerp(a, b, t), RiftDraw.Glow(140, 255, 240, 0.5f), 0.18f);
				}
				return false;
			}

			float thick = (5.5f + (float)Math.Sin(Age * 0.17f) * 1.4f) * live;
			Color hot = RiftDraw.Glow(150, 255, 240, live);
			Color haze = RiftDraw.Glow(24, 150, 145, live * 0.7f);
			RiftDraw.Silk(a, b, hot, haze, thick, sag, Age * 0.008f % 1f, Segments);

			// the knots at either end: a strand under tension is anchored to something
			RiftDraw.Bloom(a, RiftDraw.Glow(120, 255, 235, live * 0.7f), 0.42f);
			RiftDraw.Bloom(b, RiftDraw.Glow(120, 255, 235, live * 0.7f), 0.42f);
			return false;
		}
	}
}
