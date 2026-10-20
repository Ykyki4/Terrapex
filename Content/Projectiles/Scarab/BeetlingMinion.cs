using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;

namespace Terrapex.Content.Projectiles.Scarab
{
	/// <summary>
	/// The Beetling Staff's minion: a gilded beetling that fights the way the Sultan does. It flies
	/// to a point off to the side of its target, holds there for <see cref="Hold"/> ticks while it
	/// lines up, then dives straight through for <see cref="Dash"/> ticks and swings round for the
	/// next pass. Two frames of wings, facing right.
	/// </summary>
	public class BeetlingMinion : ModProjectile
	{
		private const float Range = 700f;
		private const int Hold = 10;
		private const int Dash = 16;
		private const float DashSpeed = 15f;
		private const float Standoff = 130f;

		private const int Approach = 0, Lining = 1, Diving = 2;

		private ref float State => ref Projectile.ai[0];
		private ref float Timer => ref Projectile.ai[1];

		public override void SetStaticDefaults()
		{
			Main.projFrames[Type] = 2;
			Main.projPet[Type] = true;
			ProjectileID.Sets.MinionSacrificable[Type] = true;
			ProjectileID.Sets.MinionTargettingFeature[Type] = true;
			ProjectileID.Sets.CultistIsResistantTo[Type] = true;
		}

		public override void SetDefaults()
		{
			Projectile.width = 22;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.minion = true;
			Projectile.minionSlots = 1f;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 18000;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.netImportant = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
		}

		public override bool MinionContactDamage() => true;

		// it only hurts while it dives - drifting into an enemy is not an attack
		public override bool? CanDamage() => State == Diving;

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (owner.dead || !owner.active)
			{
				owner.ClearBuff(ModContent.BuffType<BeetlingBuff>());
				Projectile.Kill();
				return;
			}
			if (owner.HasBuff(ModContent.BuffType<BeetlingBuff>()))
				Projectile.timeLeft = 2;

			if (Projectile.Distance(owner.Center) > 1600f)
			{
				Projectile.Center = owner.Center;
				Projectile.velocity = Vector2.Zero;
				State = Approach;
			}

			NPC target = FindTarget(owner);
			Timer++;

			if (State == Diving)
			{
				if (Timer >= Dash)
				{
					State = Approach;
					Timer = 0f;
				}
			}
			else if (target == null)
			{
				State = Approach;
				Idle(owner);
			}
			else if (State == Approach)
			{
				// a point to the side of the target, on the far side from where it is now
				int side = Projectile.Center.X < target.Center.X ? -1 : 1;
				Vector2 spot = target.Center + new Vector2(side * Standoff, -50f);
				MoveTo(spot, 11f, 0.12f);
				if (Projectile.Distance(spot) < 40f)
				{
					State = Lining;
					Timer = 0f;
				}
			}
			else
			{
				Projectile.velocity *= 0.8f;
				if (Timer >= Hold)
				{
					State = Diving;
					Timer = 0f;
					Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * DashSpeed;
					Projectile.netUpdate = true;
				}
			}

			Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;
			Projectile.rotation = State == Diving
				? Projectile.velocity.ToRotation() + (Projectile.spriteDirection < 0 ? MathHelper.Pi : 0f)
				: Projectile.velocity.X * 0.04f;

			if (++Projectile.frameCounter >= 3)
			{
				Projectile.frameCounter = 0;
				Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
			}
			Lighting.AddLight(Projectile.Center, 0.3f, 0.22f, 0.06f);
			if (State == Diving && Main.rand.NextBool(2))
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, 0f, 0f, 100, default, 0.9f).noGravity = true;
		}

		private void Idle(Player owner)
		{
			int index = 0;
			foreach (Projectile p in Main.ActiveProjectiles)
			{
				if (p.owner == Projectile.owner && p.type == Type && p.whoAmI < Projectile.whoAmI)
					index++;
			}
			Vector2 spot = owner.Center + new Vector2(-owner.direction * (40f + index * 24f),
				-50f + MathF.Sin(Main.GlobalTimeWrappedHourly * 3f + index) * 8f);
			MoveTo(spot, 9f, 0.08f);
		}

		private void MoveTo(Vector2 spot, float speed, float pull)
		{
			Vector2 to = spot - Projectile.Center;
			float d = to.Length();
			Vector2 want = d > 1f ? to / d * Math.Min(speed, d * 0.15f + 1f) : Vector2.Zero;
			Projectile.velocity = Vector2.Lerp(Projectile.velocity, want, pull);
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
				float d = owner.Distance(n.Center);
				if (d < bestDist)
				{
					bestDist = d;
					best = n;
				}
			}
			return best;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Rectangle frame = tex.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
			SpriteEffects fx = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, frame, lightColor, Projectile.rotation,
				frame.Size() * 0.5f, 1f, fx, 0);
			return false;
		}
	}
}
