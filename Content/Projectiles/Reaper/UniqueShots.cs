using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;

namespace Terrapex.Content.Projectiles.Reaper
{
	/// <summary>
	/// The Frostbite Sickle's strike: icicles brought down on the clicked point from above. An
	/// icicle does not just hit: it sticks in, and a moment later bursts into three shards around
	/// the thing it is stuck in, so the strike rewards landing in a crowd.
	///
	/// ai[0] is 0 while flying and 1 once stuck; ai[1] is the NPC it is stuck in. The offset from
	/// that NPC lives in velocity, the usual javelin trick, so every client can follow it.
	/// </summary>
	public class FrostIcicle : ModProjectile
	{
		public const float Speed = 15f;
		private const int StuckTicks = 50;
		public const float ShatterShare = 0.45f;

		private bool Stuck => Projectile.ai[0] == 1f;

		public override void SetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = ModContent.GetInstance<ReapingDamage>();
			Projectile.penetrate = -1;
			Projectile.timeLeft = 34;
			Projectile.tileCollide = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void AI()
		{
			if (Stuck)
			{
				NPC npc = Main.npc[(int)Projectile.ai[1]];
				if (!npc.active)
				{
					Projectile.Kill();
					return;
				}
				Projectile.Center = npc.Center + Projectile.velocity;
				return;
			}

			// it falls from above the point, so it passes through any ceiling for its first ticks
			// rather than dying inside the stone it was spawned in
			Projectile.tileCollide = Projectile.timeLeft < 25;
			if (Projectile.timeLeft < 22)
				Projectile.velocity.Y += 0.25f;
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			if (Main.rand.NextBool(3))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Ice, Projectile.velocity * 0.1f, 120, default, 0.8f);
				d.noGravity = true;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			ReapingSlash.RangedHit(Projectile, target, hit);
			target.AddBuff(BuffID.Frostburn, 180);
			if (Stuck)
				return;
			Projectile.ai[0] = 1f;
			Projectile.ai[1] = target.whoAmI;
			Projectile.velocity = (Projectile.Center - target.Center) * 0.75f;
			Projectile.timeLeft = StuckTicks;
			Projectile.friendly = false;
			Projectile.tileCollide = false;
			Projectile.netUpdate = true;
		}

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.6f }, Projectile.Center);
			for (int i = 0; i < 10; i++)
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Ice, Main.rand.NextVector2Circular(3f, 3f), 100, default, 1f);
				d.noGravity = true;
			}
			if (!Stuck || Projectile.owner != Main.myPlayer)
				return;
			for (int i = 0; i < 3; i++)
			{
				Vector2 v = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 3f + Main.rand.NextFloat(0.6f)) * 7f;
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, v,
					ModContent.ProjectileType<ReapBolt>(), (int)(Projectile.damage * ShatterShare), 0f,
					Projectile.owner, ReapBolt.Ice);
			}
		}
	}

	/// <summary>
	/// The Lidcutter's throw: a disc of the lid, eye and all, flung out like a boomerang. It goes
	/// out to the clicked point, turns there, and comes back through whatever it passed, cracking
	/// everything it touches - two chances on one throw, the way the Rift Scythe's arc works a
	/// tier later. ai[1] is the tick it turns on, worked out by the scythe from the distance.
	/// </summary>
	public class LidDisc : ModProjectile
	{
		public const float Speed = 13f;
		private const int DefaultTurn = 20;
		private float TurnAt => Projectile.ai[1] > 0f ? Projectile.ai[1] : DefaultTurn;

		private ref float Timer => ref Projectile.ai[0];

		public override void SetDefaults()
		{
			Projectile.width = 18;
			Projectile.height = 18;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = ModContent.GetInstance<ReapingDamage>();
			Projectile.penetrate = -1;
			Projectile.timeLeft = 180;
			Projectile.tileCollide = false;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 30;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			Timer++;
			if (Timer > TurnAt)
			{
				Vector2 home = owner.MountedCenter - Projectile.Center;
				float d = home.Length();
				if (d < 24f)
				{
					Projectile.Kill();
					return;
				}
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, home / d * 15f, 0.12f);
			}
			Projectile.rotation += 0.4f * (Projectile.velocity.X >= 0f ? 1f : -1f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			ReapingSlash.RangedHit(Projectile, target, hit);
			target.AddBuff(ModContent.BuffType<Buffs.Cracked>(), 180);
		}
	}
}
