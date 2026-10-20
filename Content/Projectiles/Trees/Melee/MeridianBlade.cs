using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Particles;
using Terrapex.Content.Buffs;
using Terrapex.Content.Items.Weapons;
using Terrapex.Content.Items.Weapons.Rime;
using Terrapex.Content.Items.Weapons.Scarab;
using Terrapex.Content.Items.Weapons.Shellback;
using Terrapex.Content.Items.Weapons.Trees.Melee;
using Terrapex.Content.Projectiles.Rime;
using Terrapex.Content.Projectiles.Scarab;
using Terrapex.Content.Projectiles.Shellback;

namespace Terrapex.Content.Projectiles.Trees.Melee
{
	/// <summary>
	/// Meridian's spectral copies - the Zenith bow of the tree. Every swing opens five of them in
	/// a ring round the player: the Wrecker's Cutlass, the Sultan's Khopesh, the Glacierbreaker,
	/// Bonefissure and the Riftshard Cleaver, each drawn from that item's own texture so no art is
	/// duplicated. They hang for <see cref="Hover"/> ticks turning to face the point that was
	/// clicked, then dash straight through it - never homing, the line is fixed at the swing -
	/// and where they reach it each one lets go of its own sword's trick.
	///
	/// ai[0] = blade (0..4), ai[1]/ai[2] = target point.
	/// </summary>
	public class MeridianBlade : ModProjectile
	{
		public const float Share = 0.3f;
		public const int Blades = 5;
		private const int Hover = 14;
		private const float Speed = 24f;
		private const float Ring = 62f;

		public override string Texture => "Terrapex/Content/Projectiles/RiftFlare";

		private int Blade => Math.Clamp((int)Projectile.ai[0], 0, Blades - 1);
		private Vector2 Target => new(Projectile.ai[1], Projectile.ai[2]);
		private int Age => (int)Projectile.localAI[0];

		/// <summary>The item each copy is a ghost of, in the order the tree was built.</summary>
		public static int ItemOf(int blade) => blade switch
		{
			0 => ModContent.ItemType<WreckersCutlass>(),
			1 => ModContent.ItemType<SultansKhopesh>(),
			2 => ModContent.ItemType<Glacierbreaker>(),
			3 => ModContent.ItemType<Bonefissure>(),
			_ => ModContent.ItemType<RiftshardCleaver>(),
		};

		private static Color Tint(int blade) => blade switch
		{
			0 => new Color(70, 214, 214),
			1 => new Color(255, 186, 64),
			2 => new Color(150, 228, 255),
			3 => new Color(255, 232, 186),
			_ => new Color(255, 120, 220),
		};

		/// <summary>Opens all five round the player toward <paramref name="target"/>.</summary>
		public static void Unfold(IEntitySource src, Player player, Vector2 target, int damage, float knockback)
		{
			if (player.whoAmI != Main.myPlayer)
				return;
			Vector2 to = target - player.MountedCenter;
			float max = 640f;
			if (to.Length() > max)
				target = player.MountedCenter + to.SafeNormalize(Vector2.UnitX) * max;
			for (int i = 0; i < Blades; i++)
				Projectile.NewProjectile(src, player.MountedCenter, Vector2.Zero, ModContent.ProjectileType<MeridianBlade>(),
					damage, knockback * 0.5f, player.whoAmI, i, target.X, target.Y);
		}

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 10;
			ProjectileID.Sets.TrailingMode[Type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.width = 34;
			Projectile.height = 34;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.timeLeft = Hover + 60;
		}

		public override void AI()
		{
			Player p = Main.player[Projectile.owner];
			Projectile.localAI[0]++;
			if (Age <= Hover)
			{
				// open out of the player into a ring - a fan above the aim, so it reads as a spread hand
				Vector2 aim = (Target - p.MountedCenter).SafeNormalize(Vector2.UnitX * p.direction);
				float a = aim.ToRotation() + MathHelper.Pi + (Blade - 2) * 0.62f;
				float open = (float)Math.Sin(Age / (float)Hover * MathHelper.PiOver2);
				Projectile.Center = p.MountedCenter + a.ToRotationVector2() * Ring * open;
				float face = (Target - Projectile.Center).ToRotation();
				Projectile.rotation = Utils.AngleLerp(a, face, open);
				if (Age == Hover)
				{
					Projectile.velocity = (Target - Projectile.Center).SafeNormalize(Vector2.UnitX) * Speed;
					Projectile.localAI[1] = (Target - Projectile.Center).Length() / Speed; // ticks to the point
					if (!Main.dedServ && Blade == 0)
						SoundEngine.PlaySound(SoundID.Item169 with { Volume = 0.5f }, Projectile.Center);
				}
				return;
			}

			Projectile.rotation = Projectile.velocity.ToRotation();
			int dash = Age - Hover;
			if (dash >= (int)Projectile.localAI[1] && Projectile.localAI[2] == 0f)
			{
				Projectile.localAI[2] = 1f;
				Strike(p);
			}
			if (Projectile.localAI[2] != 0f)
			{
				Projectile.velocity *= 0.9f;
				if (dash > Projectile.localAI[1] + 14)
					Projectile.Kill();
			}
			Lighting.AddLight(Projectile.Center, Tint(Blade).ToVector3() * 0.5f);
			if (!Main.dedServ && Main.rand.NextBool(2))
				ParticleSystem.Spawn(new Spark(Projectile.Center, -Projectile.velocity * 0.15f + Main.rand.NextVector2Circular(1f, 1f),
					Tint(Blade), 0.8f, 16));
		}

		// the sword's own trick, at the point the copy was sent to
		private void Strike(Player p)
		{
			Vector2 at = Projectile.Center;
			Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
			if (!Main.dedServ)
				FX.Impact(at, Color.White, Tint(Blade), 0.8f);
			if (Projectile.owner != Main.myPlayer)
				return;
			IEntitySource src = Projectile.GetSource_FromThis();
			int dmg = Projectile.damage;
			switch (Blade)
			{
				case 0:
					for (int i = 0; i < 3; i++)
						Projectile.NewProjectile(src, at, new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-7f, -4f)),
							ModContent.ProjectileType<BarnacleShrapnel>(), (int)(dmg * 0.5f), 1f, p.whoAmI);
					break;
				case 1:
					Projectile.NewProjectile(src, at, Vector2.Zero, ModContent.ProjectileType<KhopeshFlare>(), dmg, 3f, p.whoAmI);
					break;
				case 2:
					for (int i = 0; i < 3; i++)
						Projectile.NewProjectile(src, at, dir.RotatedBy((i - 1) * 0.5f) * 8f - Vector2.UnitY * 2f,
							ModContent.ProjectileType<RimeSplinter>(), (int)(dmg * 0.5f), 2f, p.whoAmI, 1f);
					break;
				case 3:
					for (int i = -1; i <= 1; i += 2)
						Projectile.NewProjectile(src, at, dir.RotatedBy(i * 0.13f) * 11f, ModContent.ProjectileType<BoneSplinter>(),
							(int)(dmg * 0.5f), 2f, p.whoAmI);
					break;
				default:
					Projectile.NewProjectile(src, at, dir * 13f, ModContent.ProjectileType<RiftWave>(), (int)(dmg * 0.8f), 3f, p.whoAmI);
					break;
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
			=> Age > Hover ? null : false;

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			switch (Blade)
			{
				case 0: target.AddBuff(BuffID.Wet, 240); break;
				case 1: target.AddBuff(BuffID.OnFire3, 180); break;
				case 2: target.AddBuff(BuffID.Frostburn2, 180); break;
				case 3: target.AddBuff(ModContent.BuffType<Cracked>(), 180); break;
				default: target.AddBuff(ModContent.BuffType<Cracked>(), 120); break;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int type = ItemOf(Blade);
			Main.instance.LoadItem(type);
			Texture2D tex = TextureAssets.Item[type].Value;
			Vector2 origin = tex.Size() * 0.5f;
			// the item sprites point up-right, -45 degrees; turn that onto the flight line
			float rot = Projectile.rotation + MathHelper.PiOver4;
			float appear = Math.Min(1f, Age / 6f);
			float gone = Projectile.localAI[2] != 0f ? Math.Max(0f, 1f - (Age - Hover - Projectile.localAI[1]) / 14f) : 1f;
			float a = appear * gone;
			Color tint = Tint(Blade);
			Vector2 offset = Projectile.Size * 0.5f - Main.screenPosition;

			// afterimages only once it is moving: the dash is the stroke
			if (Age > Hover)
			{
				for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
				{
					if (Projectile.oldPos[i] == Vector2.Zero)
						continue;
					float f = 1f - i / (float)Projectile.oldPos.Length;
					Main.EntitySpriteDraw(tex, Projectile.oldPos[i] + offset, null, Glow(tint, 0.35f * f * a),
						Projectile.oldRot[i] + MathHelper.PiOver4, origin, 0.9f + 0.1f * f, SpriteEffects.None, 0);
				}
			}
			RiftDraw.Bloom(Projectile.Center, Glow(tint, 0.4f * a), 0.7f);
			// the ghost itself: its own colours, pale, half light and half paint
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Glow(tint, 0.55f * a), rot, origin,
				1.12f, SpriteEffects.None, 0);
			Color ghost = new Color(235, 240, 255, 150) * (0.85f * a);
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, ghost, rot, origin, 1f, SpriteEffects.None, 0);
			return false;
		}

		private static Color Glow(Color c, float s)
		{
			Color o = c * s;
			o.A = 0;
			return o;
		}
	}
}
