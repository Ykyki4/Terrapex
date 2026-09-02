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
	/// An eye the First Keeper leaves standing in the arena. It opens, takes aim once, and fires
	/// a single <see cref="GazeRay"/> down the line it settled on — then closes and is gone.
	///
	/// It is a turret rather than a shot on purpose. Six of them placed around the player draw
	/// six lines that all exist before any of them can hurt, so the move is a puzzle with a
	/// timer on it: the gaps are visible from the first frame and the player picks one.
	///
	/// Nothing about it tracks. It reads the player's position once, at the moment it opens,
	/// and commits — the same rule every patterned attack in this mod follows.
	/// </summary>
	public class Watcher : ModProjectile
	{
		public const int Open = 34;
		public const int Aim = 76;
		private const int Close = 40;
		public const int Life = Aim + Close;

		/// <summary>What the ray it fires will carry.</summary>
		private ref float RayDamage => ref Projectile.ai[0];
		private ref float Facing => ref Projectile.ai[1];
		private int Age => Life - Projectile.timeLeft;

		public override void SetStaticDefaults() => Main.projFrames[Type] = 4;

		public override void SetDefaults()
		{
			Projectile.width = 30;
			Projectile.height = 30;
			Projectile.aiStyle = -1;
			Projectile.hostile = false;
			Projectile.friendly = false;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.netImportant = true;
		}

		public override bool ShouldUpdatePosition() => false;

		public static Projectile Spawn(NPC source, Vector2 at, float facing, int rayDamage)
		{
			Projectile p = Projectile.NewProjectileDirect(source.GetSource_FromAI(), at,
				Vector2.Zero, ModContent.ProjectileType<Watcher>(), 0, 0f, Main.myPlayer,
				rayDamage, facing);
			p.Center = at;
			p.netUpdate = true;
			return p;
		}

		public override void AI()
		{
			int t = Age;

			if (t == 1)
				SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.4f, Pitch = 0.8f }, Projectile.Center);

			// it fires once, on the frame its lid finishes opening
			if (t == Aim && Main.netMode != NetmodeID.MultiplayerClient)
				GazeRay.Spawn(Projectile.GetSource_FromAI(), Projectile.Center, Facing, 0f,
					(int)RayDamage, GazeRay.Telegraph + 46);

			Projectile.rotation = Facing;
			Lighting.AddLight(Projectile.Center, 0.4f, 0.4f, 0.45f);

			if (t > Open && t < Aim && Main.rand.NextBool(3))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(14f, 14f),
					DustID.WhiteTorch, Vector2.Zero, 130, default, 0.8f);
				d.noGravity = true;
				d.velocity = (Projectile.Center - d.position) * 0.07f;
			}
		}

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 12; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.WhiteTorch,
					Main.rand.NextVector2Circular(2.5f, 2.5f), 120, default, 1f);
				d.noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int t = Age;
			// 0 shut, 1-2 opening, 3 wide: the frame is the countdown
			Projectile.frame = t < Open / 2 ? 0 : t < Open ? 1 : t < Aim ? 2 : 3;

			float open = MathHelper.Clamp(t / (float)Open, 0f, 1f);
			float wind = t < Aim ? MathHelper.Clamp((t - Open) / (float)(Aim - Open), 0f, 1f) : 1f;

			// the line it has settled on, drawn before it fires — this is the whole tell
			if (t > Open)
			{
				Vector2 to = Projectile.Center + Facing.ToRotationVector2() * 1500f;
				RiftDraw.Line(Projectile.Center, to, RiftDraw.Glow(70, 74, 90, 0.10f + wind * 0.26f),
					1.2f + wind * 2.2f);
				RiftDraw.Line(Projectile.Center, to, RiftDraw.Glow(255, 255, 255, 0.08f + wind * 0.22f), 0.8f);
			}

			RiftDraw.Bloom(Projectile.Center, RiftDraw.Glow(255, 255, 255, 0.30f + wind * 0.5f),
				0.35f + open * 0.35f);
			RiftDraw.Ring(Projectile.Center, RiftDraw.Glow(190, 196, 215, 0.25f + wind * 0.3f),
				0.55f + (1f - wind) * 0.5f, (float)Main.timeForVisualEffects * 0.02f);
			RiftDraw.Head(Projectile, Color.White);
			return false;
		}
	}
}
