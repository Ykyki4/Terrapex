using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Particles;
using Terrapex.Common.Reaper;
using Terrapex.Content.Items.Weapons.Trees.Reaper;
using Terrapex.Content.Projectiles.Reaper;

namespace Terrapex.Content.Projectiles.Trees.Reaper
{
	/// <summary>
	/// The Event Horizon's vortex: opened on the point at the end of every cut. For a little under
	/// a second it drags small enemies - anything that is not a boss and takes knockback - toward
	/// its middle, wears at whatever its core holds, and then collapses into all five rites of the
	/// tree at once. The Undertow's whirlpool at the bottom of the tree, grown into a hole.
	///
	/// Drawn back to front the way a black hole has to be: a lensing haze, the far half of two
	/// tilted accretion rings, the black core with its photon ring, then the near half of the rings
	/// over the core. The rings run hot on the approaching side and cold on the receding one, the
	/// outer ring carries Final Harvest's four pillar colours and the hot spot Lastlight's warm
	/// regard - the two scythes it was forged from, still visible in it.
	///
	/// ai[0]: the cut's damage, which the collapse's rites are shares of. ai[1]: scale, 0 meaning 1.
	/// </summary>
	public class EventHorizonVortex : ModProjectile
	{
		/// <summary>The vortex's own tick hits, as a share of the cut.</summary>
		public const float TickShare = 0.2f;
		public const int MaxOpen = 3;
		public const float ReapScale = 1.6f;
		public const int Life = 54;
		public const float PullRadius = 230f;
		public const float HitRadius = 64f;
		public const float Core = 18f;

		private const int OpenTicks = 10;
		private const int CloseTicks = 12;

		public override string Texture => "Terrapex/Content/Projectiles/RiftFlare";

		private ref float Timer => ref Projectile.localAI[0];
		private float Size => Projectile.ai[1] > 0f ? Projectile.ai[1] : 1f;

		public override void SetDefaults()
		{
			Projectile.width = 40;
			Projectile.height = 40;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = ModContent.GetInstance<ReapingDamage>();
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Life;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
		}

		/// <summary>0 to 1 over the opening, 1 while it holds, back to 0 over the collapse.</summary>
		private float Openness
		{
			get
			{
				float open = Math.Min(1f, Timer / OpenTicks);
				float close = Math.Min(1f, Projectile.timeLeft / (float)CloseTicks);
				return Math.Min(1f - (1f - open) * (1f - open), close);
			}
		}

		public override void AI()
		{
			if (Timer == 0f)
				SoundEngine.PlaySound(SoundID.Item117 with { Pitch = -0.5f, Volume = 0.7f }, Projectile.Center);
			Timer++;
			Projectile.velocity = Vector2.Zero;
			float s = Size;
			float open = Openness;
			Lighting.AddLight(Projectile.Center, 0.45f * open, 0.4f * open, 0.7f * open);

			// matter falling in: sparks spiralling to the core, dying before they reach it so they
			// never draw over the black
			if (!Main.dedServ && Projectile.timeLeft > CloseTicks)
			{
				for (int i = 0; i < 2; i++)
				{
					float dist = Main.rand.NextFloat(0.5f, 0.85f) * PullRadius * s;
					Vector2 from = Main.rand.NextVector2Unit() * dist;
					Vector2 v = (-from).SafeNormalize(Vector2.Zero).RotatedBy(0.7f) * Main.rand.NextFloat(4f, 6.5f) * s;
					Color c = Main.rand.NextBool(3) ? MoonDraw.Prime : MoonDraw.Pillars[Main.rand.Next(MoonDraw.Pillars.Length)];
					int life = (int)((dist - Core * s * 2f) / v.Length() * 0.9f);
					ParticleSystem.Spawn(new Spark(Projectile.Center + from, v, c, 0.8f * s, Math.Max(6, life)) { Drag = 1f });
				}
			}

			// the pull is an NPC position change, so the server does it and the NPC syncs
			if (Main.netMode == NetmodeID.MultiplayerClient || Projectile.timeLeft <= CloseTicks)
				return;
			foreach (NPC n in Main.ActiveNPCs)
			{
				if (n.boss || n.friendly || n.townNPC || n.dontTakeDamage || n.knockBackResist <= 0f
					|| NPCID.Sets.ShouldBeCountedAsBoss[n.type])
					continue;
				Vector2 to = Projectile.Center - n.Center;
				float d = to.Length();
				if (d > PullRadius * s)
					continue;
				if (d < Core * s)
					n.velocity *= 0.6f; // held at the middle, not flung through it
				else
					n.velocity = Vector2.Lerp(n.velocity, to / d * 7f * n.knockBackResist * open, 0.2f);
				if (Projectile.timeLeft % 10 == 0)
					n.netUpdate = true;
			}
		}

		public override bool? CanDamage() => Timer >= OpenTicks / 2 ? null : (bool?)false;

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			float r = HitRadius * Size * Openness;
			Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
			return Vector2.DistanceSquared(nearest, Projectile.Center) <= r * r;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => ReapingSlash.RangedHit(Projectile, target, hit);

		public override void OnKill(int timeLeft)
		{
			float s = Size;
			ParticleSystem.Spawn(new Bloom(Projectile.Center, MoonDraw.Light(Color.White, 1f), 0.4f * s, 1.6f * s, 18));
			ParticleSystem.Spawn(new Shockwave(Projectile.Center, MoonDraw.Light(MoonDraw.Regard, 0.9f), 0.1f * s, 2.0f * s, 28));
			ParticleSystem.Spawn(new Shockwave(Projectile.Center, MoonDraw.Light(new Color(150, 120, 255), 0.7f), 0.1f * s, 2.8f * s, 36));
			for (int i = 0; i < MoonDraw.Pillars.Length; i++)
				FX.Sparks(Projectile.Center, MoonDraw.Pillars[i], 5, 9f * s, s, 26);
			SoundEngine.PlaySound(SoundID.Item122 with { Pitch = 0.2f, Volume = 0.8f }, Projectile.Center);

			if (Projectile.owner != Main.myPlayer)
				return;
			Player owner = Main.player[Projectile.owner];
			Vector2 aim = (Projectile.Center - owner.MountedCenter).SafeNormalize(Vector2.UnitX * owner.direction);
			MoonRites.CastAll(5, owner, Projectile.GetSource_FromThis(), Projectile.Center, aim,
				(int)Projectile.ai[0], 5f, false, false);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			var emit = MoonDraw.Direct;
			float s = Size;
			float open = Openness;
			// the collapse spins the rings up as the core shrinks
			float closing = Projectile.timeLeft < CloseTicks ? 1f - Projectile.timeLeft / (float)CloseTicks : 0f;
			Vector2 c = Projectile.Center - Main.screenPosition;
			float core = Core * s * open;
			float tilt = -0.32f;
			float phase = Timer * (0.11f + closing * 0.25f);
			Color violet = new(120, 90, 220);
			Color cold = new(70, 60, 160);

			// lensing haze, two layers a hair apart, and the faint prime halo of light bent round
			MoonDraw.Glow(emit, c, MoonDraw.Light(violet, 0.35f * open), 3.2f * s * open);
			MoonDraw.Glow(emit, c + new Vector2(1f, -1f), MoonDraw.Light(MoonDraw.Prime, 0.16f * open), 2.2f * s * open);

			// far halves of the rings
			MoonDraw.Accretion(emit, c, core * 3.1f, 0.34f, tilt, 56, phase * 0.6f, false,
				MoonDraw.Prime, cold, 0.6f * open, MoonDraw.Pillars, 7);
			MoonDraw.Accretion(emit, c, core * 2.0f, 0.34f, tilt, 40, phase, false,
				Color.White, violet, 0.9f * open);

			// the core: black, a hair of deep violet at its limb, and the photon ring round it
			MoonDraw.Disc(emit, c, core + 2f, new Color(40, 24, 70) * open);
			MoonDraw.Disc(emit, c, core, MoonDraw.Void * Math.Min(1f, open * 1.5f));
			MoonDraw.Beads(emit, c, core + 3f, Math.Max(10, (int)(core * 1.4f)), -phase * 1.5f,
				i => MoonDraw.Light(i % 4 == 0 ? MoonDraw.Regard : Color.White, 0.8f * open));

			// near halves, over the core
			MoonDraw.Accretion(emit, c, core * 2.0f, 0.34f, tilt, 40, phase, true,
				Color.White, violet, 0.9f * open);
			MoonDraw.Accretion(emit, c, core * 3.1f, 0.34f, tilt, 56, phase * 0.6f, true,
				MoonDraw.Prime, cold, 0.6f * open, MoonDraw.Pillars, 7);

			// the hot spot on the approaching limb: Lastlight's warm, a few pixels of it
			Vector2 hot = c + new Vector2(core * 2.0f, 0f).RotatedBy(tilt);
			MoonDraw.Glow(emit, hot, MoonDraw.Light(MoonDraw.Regard, 0.55f * open), 0.35f * s);
			if (closing > 0f)
				MoonDraw.Glow(emit, c, MoonDraw.Light(Color.White, closing * 0.9f), (0.3f + closing * 0.6f) * s);
			return false;
		}
	}
}
