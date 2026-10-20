using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Particles;
using Terrapex.Common.Players;

namespace Terrapex.Content.Projectiles.Trees.Accessories
{
	/// <summary>
	/// The hitbox of a Rift / Weaver / Regard dash: rides on the owner for as long as the dash
	/// is held and hits each NPC it passes through once. The player is the one passing through
	/// (<see cref="TreeAccessoryPlayer.CanBeHitByNPC"/>); this is what the pack feels.
	/// ai[0] = dash tier.
	/// </summary>
	public class AegisStrike : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_1";

		public override void SetDefaults()
		{
			Projectile.width = 56;
			Projectile.height = 56;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 14;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1; // once per NPC per dash
			Projectile.hide = true;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead)
			{
				Projectile.Kill();
				return;
			}
			Projectile.Center = owner.Center;
			Projectile.velocity = Vector2.Zero;
			if (Projectile.owner == Main.myPlayer && !owner.GetModPlayer<TreeAccessoryPlayer>().Dashing
				&& Projectile.timeLeft < 12)
				Projectile.Kill();
		}

		public override bool? CanCutTiles() => false;

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			// runs on the owner: the client that computed the hit is the one that marks it
			var m = Main.player[Projectile.owner].GetModPlayer<TreeAccessoryPlayer>();
			if ((int)Projectile.ai[0] == 5)
				m.regardMarked.Add(target.whoAmI);

			if (!Main.dedServ)
			{
				Color c = TreeAccessoryPlayer.DashColour((int)Projectile.ai[0]);
				FX.Sparks(target.Center, c, 8, 6f, 0.9f, 18);
			}
		}
	}

	/// <summary>
	/// A shove with no damage. ai[0] = 0: the gust a dash pushes ahead of itself, moving;
	/// ai[0] = 1: the Riftstride's landing, a ring of push around where the owner came down.
	/// ai[1] = colour tier. The push itself runs where NPCs are owned - the server, or the only
	/// machine in single player - and marks the NPC for a net update.
	/// </summary>
	public class AegisGust : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_1";

		private readonly bool[] pushed = new bool[Main.maxNPCs];

		private bool Landing => Projectile.ai[0] == 1f;

		public override void SetDefaults()
		{
			Projectile.width = 64;
			Projectile.height = 72;
			Projectile.aiStyle = -1;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 16;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.hide = true;
		}

		public override void AI()
		{
			Color c = TreeAccessoryPlayer.DashColour((int)Projectile.ai[1]);

			if (Landing)
			{
				// OnSpawn only runs on the spawning client, so the landing settles itself here
				Projectile.velocity = Vector2.Zero;
				if (Projectile.timeLeft > 4)
					Projectile.timeLeft = 4;
				if (Projectile.localAI[0]++ == 0)
				{
					if (!Main.dedServ)
					{
						FX.GroundSlam(Projectile.Center, new Color(255, 154, 217), new Color(147, 132, 159), 0.9f);
						SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.5f, Pitch = -0.2f }, Projectile.Center);
					}
					if (Main.netMode != NetmodeID.MultiplayerClient)
						PushRadial(Projectile.Center, 160f);
				}
				return;
			}

			if (!Main.dedServ && Main.rand.NextBool())
			{
				Vector2 at = Projectile.Center + Main.rand.NextVector2Circular(20f, 30f);
				ParticleSystem.Spawn(new Smoke(at, Projectile.velocity * 0.35f + Main.rand.NextVector2Circular(0.5f, 0.5f),
					new Color(226, 234, 242), Main.rand.NextFloat(0.22f, 0.36f), Main.rand.Next(16, 24)));
				if (Projectile.ai[1] >= 3)
					ParticleSystem.Spawn(new Spark(at, Projectile.velocity * 0.5f, c, 0.8f, 14));
			}

			Projectile.velocity *= 0.9f;
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			float dir = Math.Sign(Projectile.velocity.X);
			if (dir == 0f)
				dir = 1f;
			foreach (NPC n in Main.ActiveNPCs)
			{
				if (pushed[n.whoAmI] || n.friendly || n.knockBackResist <= 0f || n.dontTakeDamage
					|| !n.Hitbox.Intersects(Projectile.Hitbox))
					continue;
				pushed[n.whoAmI] = true;
				n.velocity.X = dir * 10f * n.knockBackResist + Projectile.velocity.X * 0.2f;
				n.velocity.Y -= 3f * n.knockBackResist;
				n.netUpdate = true;
			}
		}

		private static void PushRadial(Vector2 at, float radius)
		{
			foreach (NPC n in Main.ActiveNPCs)
			{
				if (n.friendly || n.knockBackResist <= 0f || n.dontTakeDamage)
					continue;
				float d = n.Distance(at);
				if (d > radius)
					continue;
				Vector2 away = (n.Center - at).SafeNormalize(Vector2.UnitY * -1f);
				float strength = 11f * (1f - d / radius * 0.6f) * n.knockBackResist;
				n.velocity = new Vector2(away.X * strength, -Math.Abs(strength) * 0.6f - 2f * n.knockBackResist);
				n.netUpdate = true;
			}
		}
	}

	/// <summary>
	/// Threadstep's thread: from the edge the player ran off to the player's feet, drawn with
	/// the Weaver's own silk. Pure visuals - the standing is done in TreeAccessoryPlayer. Follows
	/// the owner until ai[2] is set, then hangs where it was and fades.
	/// ai[0], ai[1] = the edge (world).
	/// </summary>
	public class StepThread : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_1";

		private const int Fade = 30;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.aiStyle = -1;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 120;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (Projectile.ai[2] == 0f)
			{
				if (!owner.active || owner.dead)
				{
					Projectile.ai[2] = 1f;
					return;
				}
				Projectile.Center = new Vector2(owner.Bottom.X, Projectile.ai[1]);
				Projectile.timeLeft = Math.Max(Projectile.timeLeft, Fade + 2);
				if (!Main.dedServ && Main.rand.NextBool(3))
					ParticleSystem.Spawn(new Spark(Projectile.Center, new Vector2(-owner.velocity.X * 0.15f, 0.4f),
						new Color(127, 242, 224), 0.6f, 16));
			}
			else if (Projectile.timeLeft > Fade)
				Projectile.timeLeft = Fade;

			Lighting.AddLight(Projectile.Center, 0.12f, 0.36f, 0.34f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float fade = Projectile.ai[2] == 0f ? 1f : Projectile.timeLeft / (float)Fade;
			Vector2 a = new Vector2(Projectile.ai[0], Projectile.ai[1]);
			Vector2 b = Projectile.Center;
			float sag = Math.Min(10f, Math.Abs(b.X - a.X) * 0.03f) * (1f - fade * 0.6f);
			RiftDraw.Silk(a, b, new Color(127, 242, 224, 0) * fade, new Color(28, 125, 120, 0) * (0.5f * fade),
				1.8f, sag, (float)(Main.GameUpdateCount * 0.02 % 1.0));
			return false;
		}
	}
}
