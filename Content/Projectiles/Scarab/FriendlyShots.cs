using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Reaper;

namespace Terrapex.Content.Projectiles.Scarab
{
	/// <summary>
	/// The Sultan's Khopesh's sun flare: one burst on the struck target, catching what stands round
	/// it. Lives a few ticks, hits each enemy once, and is all light - it borrows the shard texture
	/// only so the loader has one.
	/// </summary>
	public class KhopeshFlare : ModProjectile
	{
		public const int Radius = 56;
		private const int Life = 12;

		public override string Texture => "Terrapex/Content/Projectiles/Scarab/SunShard";

		public override void SetDefaults()
		{
			Projectile.width = Radius * 2;
			Projectile.height = Radius * 2;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.timeLeft = Life;
		}

		public override void AI()
		{
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.6f, Pitch = 0.3f }, Projectile.Center);
				for (int i = 0; i < 20; i++)
				{
					Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, Main.rand.NextVector2Circular(6f, 6f), 60, default, 1.4f);
					d.noGravity = true;
				}
			}
			Lighting.AddLight(Projectile.Center, 0.9f, 0.6f, 0.2f);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
			return Projectile.timeLeft > Life - 3 && Vector2.DistanceSquared(nearest, Projectile.Center) <= Radius * Radius;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float t = 1f - Projectile.timeLeft / (float)Life;
			RiftDraw.Bloom(Projectile.Center, new Color(255, 190, 90, 0) * (1f - t), 0.8f + t);
			RiftDraw.Ring(Projectile.Center, new Color(255, 220, 140, 0) * (1f - t), 0.6f + 1.4f * t);
			return false;
		}
	}

	/// <summary>
	/// The Sunwing Bow's arrow. Flies like a wooden arrow, and the first wall it meets it glances off
	/// instead of sticking - once. Pierces two.
	/// </summary>
	public class ScarabArrow : ModProjectile
	{
		private ref float Bounced => ref Projectile.ai[0];
		private ref float Flight => ref Projectile.ai[1];

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 6;
			ProjectileID.Sets.TrailingMode[Type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 2;
			Projectile.arrow = true;
			Projectile.timeLeft = 600;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void AI()
		{
			// a wooden arrow's flight: straight for a beat, then gravity
			if (++Flight > 15f)
				Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.1f, 16f);
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			if (Main.rand.NextBool(4))
				Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.1f, 100, default, 0.7f).noGravity = true;
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			if (Bounced > 0f)
				return true;
			Bounced = 1f;
			if (Projectile.velocity.X != oldVelocity.X)
				Projectile.velocity.X = -oldVelocity.X * 0.8f;
			if (Projectile.velocity.Y != oldVelocity.Y)
				Projectile.velocity.Y = -oldVelocity.Y * 0.8f;
			Flight = 0f;
			SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.5f, Pitch = 0.5f }, Projectile.Center);
			return false;
		}

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 5; i++)
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, 0f, 0f, 100, default, 0.9f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Color lit = lightColor;
			RiftDraw.Trail(Projectile, f => new Color(255, 190, 90, 0) * (0.35f * f), f => 0.7f + 0.3f * f);
			RiftDraw.Head(Projectile, lit);
			return false;
		}
	}

	/// <summary>
	/// The Sun Scepter's bolt: lobbed, falling in an arc. Where it lands - on the floor or on an
	/// enemy - a <see cref="ScepterPillar"/> goes up out of the ground beneath.
	/// </summary>
	public class ScepterBolt : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 8;
			ProjectileID.Sets.TrailingMode[Type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 240;
		}

		public override void AI()
		{
			Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.22f, 14f);
			Projectile.rotation += 0.25f * Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X);
			Lighting.AddLight(Projectile.Center, 0.6f, 0.45f, 0.15f);
			if (Main.rand.NextBool(2))
				Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -Projectile.velocity * 0.15f, 100, default, 0.9f).noGravity = true;
		}

		private void RaiseAt(Vector2 point)
		{
			if (Projectile.owner != Main.myPlayer)
				return;
			Vector2 floor = SandPillar.FindGround(point, 20);
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), floor, Vector2.Zero,
				ModContent.ProjectileType<ScepterPillar>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			RaiseAt(Projectile.Center);
			return true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => RaiseAt(target.Bottom - new Vector2(0f, 8f));

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 10; i++)
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Sand, 0f, -2f, 80, default, 1.1f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			RiftDraw.Trail(Projectile, f => new Color(255, 180, 80, 0) * (0.45f * f), f => 0.5f + 0.5f * f);
			RiftDraw.Bloom(Projectile.Center, new Color(255, 190, 90, 0) * 0.45f, 0.4f);
			RiftDraw.Head(Projectile, Color.White);
			return false;
		}
	}

	/// <summary>
	/// The Khepri Scythe's sun pebble: dropped out of the cut, it rolls by the same
	/// <see cref="BallPhysics"/> as the Sultan's sun, through up to three enemies. ai[0]/ai[1] are
	/// its true velocity.
	/// </summary>
	public class SunPebble : ModProjectile
	{
		public const int Size = 20;
		private const int Life = 120;

		public override void SetDefaults()
		{
			Projectile.width = Size;
			Projectile.height = Size;
			Projectile.friendly = true;
			Projectile.DamageType = ModContent.GetInstance<ReapingDamage>();
			Projectile.penetrate = 3;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
			Projectile.timeLeft = Life;
		}

		public override void AI()
		{
			Vector2 vel = new(Projectile.ai[0], Projectile.ai[1]);
			Vector2 pos = Projectile.position;
			Vector2 moved = BallPhysics.Step(ref pos, ref vel, Projectile.width, Projectile.height);
			Projectile.position = pos;
			Projectile.velocity = moved;
			Projectile.ai[0] = vel.X;
			Projectile.ai[1] = vel.Y;
			Projectile.rotation += moved.X / (Size * 0.5f);
			Projectile.alpha = Projectile.timeLeft < 15 ? (15 - Projectile.timeLeft) * 17 : 0;
			Lighting.AddLight(Projectile.Center, 0.5f, 0.35f, 0.1f);
			if (Main.rand.NextBool(4))
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, 0f, -1f, 100, default, 0.8f).noGravity = true;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float fade = 1f - Projectile.alpha / 255f;
			RiftDraw.Bloom(Projectile.Center, new Color(255, 190, 90, 0) * (0.35f * fade), 0.45f);
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White * fade, Projectile.rotation,
				tex.Size() * 0.5f, 1f, SpriteEffects.None, 0);
			return false;
		}
	}

	/// <summary>
	/// The Sultan's Carapace's sun mote: one of eight thrown out in a ring when the wearer is hit.
	/// Straight, speeding up, gone in a second.
	/// </summary>
	public class SunMote : ModProjectile
	{
		public override string Texture => "Terrapex/Content/Projectiles/Scarab/SunShard";

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 5;
			ProjectileID.Sets.TrailingMode[Type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Generic;
			Projectile.penetrate = 1;
			Projectile.tileCollide = true;
			Projectile.timeLeft = 60;
		}

		public override void AI()
		{
			if (Projectile.velocity.Length() < 11f)
				Projectile.velocity *= 1.06f;
			Projectile.rotation = Projectile.velocity.ToRotation();
			Lighting.AddLight(Projectile.Center, 0.5f, 0.35f, 0.1f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			RiftDraw.Trail(Projectile, f => new Color(255, 170, 70, 0) * (0.5f * f), f => 0.6f + 0.4f * f);
			RiftDraw.Head(Projectile, Color.White);
			return false;
		}
	}
}
