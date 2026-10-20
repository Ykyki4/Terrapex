using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Particles;
using Terrapex.Content.Items.Weapons.Trees.Melee;

namespace Terrapex.Content.Projectiles.Trees.Melee
{
	/// <summary>
	/// The wave a Confluence-tree swing lets go: a crescent of one current. What it does on a hit
	/// is the current's, not the wave's - the tide wave shoves (double knockback, Wet), the sun
	/// wave sets a flare off on the first thing it touches, the frost wave sheds splinters there,
	/// and the bone wave pierces everything and goes through walls. From node 3 on it also leaves
	/// a <see cref="WorldseamSeam"/> where it lands, at most two per wave.
	///
	/// ai[0] = current, ai[1] = node.
	/// </summary>
	public class ConfluenceWave : ModProjectile
	{
		private const int MaxSeams = 2;

		private Current Cur => (Current)(int)Projectile.ai[0];
		private int Node => Math.Max(1, (int)Projectile.ai[1]);

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 9;
			ProjectileID.Sets.TrailingMode[Type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.width = 36;
			Projectile.height = 36;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = 3;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.timeLeft = 60;
			Projectile.extraUpdates = 1;
		}

		public override void AI()
		{
			if (Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				Projectile.timeLeft = 34 + Node * 8; // updates, two per tick
				if (Cur == Current.Bone)
				{
					Projectile.penetrate = -1;
					Projectile.tileCollide = false;
				}
				Projectile.scale = 0.85f + 0.18f * Node;
			}
			Projectile.velocity *= 0.993f;
			Projectile.rotation = Projectile.velocity.ToRotation();
			Color col = Currents.Of(Cur);
			Lighting.AddLight(Projectile.Center, col.ToVector3() * 0.6f);

			if (Main.dedServ || !Main.rand.NextBool(2))
				return;
			// shed from the two horns of the crescent, the way spray comes off a wave's ends
			Vector2 side = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
			Vector2 horn = Projectile.Center + side * (Main.rand.NextBool() ? 1f : -1f) * 20f * Projectile.scale;
			switch (Cur)
			{
				case Current.Tide:
					ParticleSystem.Spawn(new Spark(horn, -Projectile.velocity * 0.2f + Main.rand.NextVector2Circular(1f, 1f),
						col, 0.8f, 18, 0.12f));
					break;
				case Current.Sun:
					ParticleSystem.Spawn(new Smoke(horn, -Projectile.velocity * 0.1f, new Color(255, 140, 50), 0.25f, 24, true));
					break;
				case Current.Frost:
					ParticleSystem.Spawn(new Chip(horn, -Projectile.velocity * 0.15f, col, 0.8f, 30));
					break;
				default:
					ParticleSystem.Spawn(new Spark(horn, -Projectile.velocity * 0.15f, col, 0.6f, 14));
					break;
			}
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (Cur == Current.Tide)
				modifiers.Knockback *= 2f;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			Player owner = Main.player[Projectile.owner];
			Currents.Debuff(target, Cur, Node);
			Currents.Splash(target.Center, Cur, 0.9f);

			// the sun, frost and tide tricks go off once per wave, on the first thing it hits;
			// bone's trick is the pierce itself
			if (Projectile.localAI[1] == 0f && Cur != Current.Bone)
			{
				Projectile.localAI[1] = 1f;
				Currents.Burst(Projectile.GetSource_FromThis(), owner, target.Center, Projectile.velocity, Cur,
					Projectile.damage);
			}

			if (Node >= 3 && Projectile.owner == Main.myPlayer && Projectile.ai[2] < MaxSeams)
			{
				Projectile.ai[2]++;
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
					ModContent.ProjectileType<WorldseamSeam>(), (int)(Projectile.damage * WorldseamSeam.Share), 2f,
					Projectile.owner, Projectile.rotation + MathHelper.PiOver2 + Main.rand.NextFloat(-0.35f, 0.35f), (float)Cur, Node);
			}
		}

		public override void OnKill(int timeLeft)
		{
			Currents.Splash(Projectile.Center, Cur, 0.6f);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Color col = Currents.Of(Cur);
			Color edge = Currents.Edge(Node);
			float life = Math.Min(1f, Projectile.timeLeft / 14f);
			Vector2 at = Projectile.Center - Main.screenPosition;
			Vector2 origin = tex.Size() * 0.5f;

			// the trail is the current, fading to nothing; the head carries the node's edge
			RiftDraw.Trail(Projectile, f => Glow(col, 0.45f * f * life), f => 0.55f + 0.45f * f);
			RiftDraw.Bloom(Projectile.Center, Glow(col, 0.35f * life), 0.55f * Projectile.scale);
			Main.EntitySpriteDraw(tex, at, null, Glow(col, 0.95f * life), Projectile.rotation, origin,
				new Vector2(1.1f, 1.05f) * Projectile.scale, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(tex, at + Projectile.velocity.SafeNormalize(Vector2.Zero) * 3f, null,
				Glow(edge, 0.8f * life), Projectile.rotation, origin, new Vector2(0.7f, 0.9f) * Projectile.scale,
				SpriteEffects.None, 0);
			Main.EntitySpriteDraw(tex, at + Projectile.velocity.SafeNormalize(Vector2.Zero) * 5f, null,
				Glow(Color.White, 0.6f * life), Projectile.rotation, origin, new Vector2(0.4f, 0.75f) * Projectile.scale,
				SpriteEffects.None, 0);
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
