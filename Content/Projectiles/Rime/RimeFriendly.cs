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
using Terrapex.Content.Buffs;
using Terrapex.Content.Projectiles.Reaper;

namespace Terrapex.Content.Projectiles.Rime
{
	/// <summary>
	/// A splinter of glacier ice, the player's half of the Rime's shard. The Glacierbreaker, the
	/// Hoarfrost Bow's arrows and the Permafrost Heart all burst into these. ai[0] picks the class
	/// it counts as: 0 generic, 1 melee, 2 ranged.
	/// </summary>
	public class RimeSplinter : ModProjectile
	{
		public override string Texture => "Terrapex/Content/Projectiles/Rime/RimeShard";

		public override void SetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Generic;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 45;
			Projectile.tileCollide = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.scale = 0.8f;
		}

		public override void AI()
		{
			Projectile.DamageType = Projectile.ai[0] switch
			{
				1f => DamageClass.Melee,
				2f => DamageClass.Ranged,
				_ => DamageClass.Generic,
			};
			Projectile.velocity.Y += 0.12f;
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			if (Main.rand.NextBool(4))
				Dust.NewDustPerfect(Projectile.Center, DustID.Ice, Vector2.Zero, 120, default, 0.7f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.Frostburn, 120);

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 3; i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.Ice, Main.rand.NextVector2Circular(1.5f, 1.5f), 90, default, 0.8f);
		}
	}

	/// <summary>
	/// The Hoarfrost Bow's icicle: it flies flat for a moment, then drops, and wherever it breaks
	/// it throws three splinters up in a fan - the Rime's block in miniature.
	/// </summary>
	public class RimeArrow : ModProjectile
	{
		public override void SetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 300;
			Projectile.arrow = true;
		}

		public override void AI()
		{
			if (++Projectile.ai[1] > 14f)
				Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.14f, 16f);
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			Lighting.AddLight(Projectile.Center, 0.1f, 0.25f, 0.35f);
			if (Main.rand.NextBool(3))
				Dust.NewDustPerfect(Projectile.Center, DustID.Ice, Projectile.velocity * 0.1f, 120, default, 0.7f).noGravity = true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.Frostburn, 150);

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.45f }, Projectile.Center);
			if (Projectile.owner != Main.myPlayer)
				return;
			for (int i = -1; i <= 1; i++)
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center,
					new Vector2(i * 2.6f, -5f), ModContent.ProjectileType<RimeSplinter>(),
					(int)(Projectile.damage * 0.35f), 1f, Projectile.owner, 2f);
		}
	}

	/// <summary>
	/// The Snowcap Staff's snowball. It falls, lands and rolls on along the floor, and it grows
	/// as it rolls - wider, heavier, harder - until a wall stops it.
	/// </summary>
	public class RollingSnowball : ModProjectile
	{
		public const int GrowTicks = 120;
		public const float MaxGrowth = 0.9f;

		private float Growth => 1f + MaxGrowth * Math.Min(1f, Projectile.ai[0] / GrowTicks);

		public override void SetDefaults()
		{
			Projectile.width = 22;
			Projectile.height = 22;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = 6;
			Projectile.timeLeft = 260;
			Projectile.tileCollide = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
		}

		public override void AI()
		{
			bool rolling = Projectile.velocity.Y == 0f;
			if (rolling)
				Projectile.ai[0]++;
			Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.3f, 12f);
			Projectile.scale = Growth;
			Projectile.rotation += Projectile.velocity.X / (11f * Projectile.scale);
			if (rolling && Main.rand.NextBool(2))
				Dust.NewDustPerfect(Projectile.Bottom, DustID.Snow,
					new Vector2(-Projectile.velocity.X * 0.2f, -Main.rand.NextFloat(0.5f, 2f)), 100, default, 1f);
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			if (Math.Abs(oldVelocity.X - Projectile.velocity.X) > 0.1f)
				return true;           // a wall stops it
			if (oldVelocity.Y > 0f)
			{
				Projectile.velocity.Y = 0f;
				Projectile.velocity.X = oldVelocity.X;
			}
			return false;
		}

		// the hitbox grows downward-anchored with the ball, so a big one still rolls on the floor
		public override void ModifyDamageHitbox(ref Rectangle hitbox)
		{
			int grow = (int)(Projectile.width * (Projectile.scale - 1f));
			hitbox.Inflate(grow / 2, grow / 2);
			hitbox.Y -= grow / 2;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
			=> modifiers.SourceDamage *= Growth;

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.Frostburn, 120);

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Item51, Projectile.Center);
			for (int i = 0; i < (int)(12 * Projectile.scale); i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.Snow, Main.rand.NextVector2Circular(4f, 4f), 80, default, 1.3f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			// drawn standing on the floor rather than round the hitbox centre, as it grows
			Vector2 at = Projectile.Bottom - Main.screenPosition - new Vector2(0f, tex.Height * Projectile.scale / 2f);
			Main.EntitySpriteDraw(tex, at, null, lightColor, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
			return false;
		}
	}

	/// <summary>
	/// The Glacier Bell's spirit: a little rime-spirit, a chip off the mountain. It rests over the
	/// player's shoulder and darts into whatever they are fighting, frostburning it, then comes
	/// back. A contact darter, so it keeps <c>MinionContactDamage</c> true.
	/// </summary>
	public class RimeSprite : ModProjectile
	{
		private const float Range = 640f;
		private const int DartTime = 32;

		private ref float Dart => ref Projectile.ai[0];

		public override void SetStaticDefaults()
		{
			Main.projPet[Type] = true;
			Main.projFrames[Type] = 4;
			ProjectileID.Sets.MinionSacrificable[Type] = true;
			ProjectileID.Sets.MinionTargettingFeature[Type] = true;
		}

		public override void SetDefaults()
		{
			Projectile.width = 22;
			Projectile.height = 22;
			Projectile.friendly = true;
			Projectile.minion = true;
			Projectile.minionSlots = 1f;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 18000;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.netImportant = true;
			Projectile.aiStyle = -1;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
		}

		public override bool MinionContactDamage() => true;

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (owner.dead || !owner.active)
			{
				owner.ClearBuff(ModContent.BuffType<RimeSpriteBuff>());
				Projectile.Kill();
				return;
			}
			if (owner.HasBuff(ModContent.BuffType<RimeSpriteBuff>()))
				Projectile.timeLeft = 2;

			NPC target = FindTarget(owner);
			if (Dart > 0f)
			{
				Dart--;
				if (target != null)
					Projectile.velocity = Vector2.Lerp(Projectile.velocity,
						Vector2.Normalize(target.Center - Projectile.Center) * 12f, 0.15f);
				Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;
			}
			else
			{
				int slot = 0;
				for (int i = 0; i < Projectile.whoAmI; i++)
				{
					Projectile p = Main.projectile[i];
					if (p.active && p.owner == Projectile.owner && p.type == Type)
						slot++;
				}
				Vector2 rest = owner.Center + new Vector2(-owner.direction * (40f + slot * 24f),
					-56f + (float)Math.Sin(Main.GameUpdateCount * 0.05f + slot) * 7f);
				Vector2 to = rest - Projectile.Center;
				if (to.Length() > 900f)
				{
					Projectile.Center = owner.Center;
					Projectile.velocity *= 0.2f;
				}
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, to * 0.1f, 0.2f);
				Projectile.spriteDirection = owner.direction;
				if (target != null)
				{
					Dart = DartTime;
					SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.35f, Pitch = 0.5f }, Projectile.Center);
				}
			}

			if (++Projectile.frameCounter >= 7)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
			}
			Lighting.AddLight(Projectile.Center, 0.15f, 0.3f, 0.45f);
			if (Main.rand.NextBool(8))
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), DustID.Snow,
					new Vector2(0f, 0.5f), 140, default, 0.8f).noGravity = true;
		}

		private NPC FindTarget(Player owner)
		{
			if (owner.HasMinionAttackTargetNPC)
			{
				NPC forced = Main.npc[owner.MinionAttackTargetNPC];
				if (forced.CanBeChasedBy(Projectile) && Projectile.Distance(forced.Center) < Range * 1.5f)
					return forced;
			}
			NPC best = null;
			float bestDist = Range;
			foreach (NPC n in Main.ActiveNPCs)
			{
				if (!n.CanBeChasedBy(Projectile))
					continue;
				float d = Projectile.Distance(n.Center);
				if (d < bestDist)
				{
					bestDist = d;
					best = n;
				}
			}
			return best;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Frostburn, 150);
			Dart = 0f;
			Projectile.velocity *= -0.5f;
		}
	}

	/// <summary>
	/// The Frostfell Scythe's icicle: it hangs over the point the scythe struck, glints for a
	/// moment and drops through it - the Rime's icefall, on the reaper's side now. ai[1] is the Y
	/// it falls to.
	/// </summary>
	public class FrostfellIcicle : ModProjectile
	{
		public const int Tell = 12;

		public override string Texture => "Terrapex/Content/Projectiles/Rime/RimeIcicle";

		private ref float Timer => ref Projectile.localAI[0];

		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 28;
			Projectile.friendly = true;
			Projectile.DamageType = ModContent.GetInstance<ReapingDamage>();
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 120;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.scale = 0.65f;
		}

		public override void AI()
		{
			Timer++;
			if (Timer <= Tell)
			{
				Projectile.velocity = Vector2.Zero;
				return;
			}
			Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 1.4f, 24f);
			if (Projectile.Center.Y >= Projectile.ai[1] + 16f)
				Projectile.Kill();
		}

		public override bool? CanDamage() => Timer > Tell ? null : (bool?)false;

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			ReapingSlash.RangedHit(Projectile, target, hit);
			target.AddBuff(BuffID.Frostburn, 180);
		}

		public override void OnKill(int timeLeft)
		{
			for (int i = 0; i < 6; i++)
				Dust.NewDustPerfect(Projectile.Center, DustID.Ice, Main.rand.NextVector2Circular(2.5f, 2.5f), 90, default, 0.9f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			if (Timer <= Tell)
				RiftDraw.Bloom(Projectile.Bottom, RiftDraw.Glow(200, 245, 255, 0.6f * Timer / Tell), 0.25f);
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.Lerp(lightColor, Color.White, 0.4f),
				0f, tex.Size() / 2f, Projectile.scale, SpriteEffects.None);
			return false;
		}
	}
}
