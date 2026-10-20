using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Particles;
using Terrapex.Content.Buffs;

namespace Terrapex.Content.Projectiles.Trees.Ranged
{
	/// <summary>
	/// The four winds of the ranged tree, one projectile with a frame per wind (ai[0]):
	///
	/// 0 spray (the Blunderbuss) - bursts into three pellets after 16 ticks, or on the first hit;
	/// 1 sun (the Sunwing Bow) - glances off up to two walls, penetrates once;
	/// 2 frost (the Hoarfrost Bow) - wherever it breaks, an icicle drops onto that spot from above;
	/// 3 cleft (the Cleft) - goes through four enemies, a crack running out of each.
	///
	/// ai[1] carries the bow's flags: the Rib's mark (fan arrows), the trade wind (Trade Wind on),
	/// and the Weft's stitch. ai[2] is the distance flown, carried over by the spray's pellets.
	///
	/// Each wind has its own trail, because glow is a progression signal and four winds that drew
	/// the same tail would be one arrow with four sprites: spray widens and stays dim like foam, sun
	/// is long and white-hot at the head, frost leaves solid frozen afterimages, cleft is a short
	/// hard f-squared tail with the seam it cut drawn straight behind it.
	/// </summary>
	public class WindArrow : ModProjectile
	{
		public const int Spray = 0, Sun = 1, Frost = 2, Cleft = 3;

		public const int FlagMark = 1;
		public const int FlagTailwind = 2;
		public const int FlagStitch = 4;

		/// <summary>25 tiles.</summary>
		public const float TailwindAt = 400f;
		public const float TailwindBoost = 1.4f;

		public const int SprayBurstAt = 16;
		public const float PelletDamage = 0.45f;
		public const float IcicleDamage = 0.6f;
		public const float StitchDamage = 0.5f;
		public const int SunBounces = 2;
		public const int CleftPierce = 4;

		private int Wind => (int)Projectile.ai[0];
		private int Flags => (int)Projectile.ai[1];
		private ref float Travelled => ref Projectile.ai[2];
		private ref float Flight => ref Projectile.localAI[0];
		private ref float Bounces => ref Projectile.localAI[1];
		private ref float Stitched => ref Projectile.localAI[2];

		public static bool Caught(int flags, float travelled) => (flags & FlagTailwind) != 0 && travelled >= TailwindAt;
		private bool IsCaught => Caught(Flags, Travelled);

		public override void SetStaticDefaults()
		{
			Main.projFrames[Type] = 4;
			ProjectileID.Sets.TrailCacheLength[Type] = 12;
			ProjectileID.Sets.TrailingMode[Type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.arrow = true;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 600;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void AI()
		{
			if (Flight == 0f)
			{
				if (Wind == Cleft)
					Projectile.penetrate = CleftPierce + 1;
				else if (Wind == Sun)
					Projectile.penetrate = 2;
			}
			Projectile.frame = Math.Clamp(Wind, 0, 3);
			Flight++;

			bool was = IsCaught;
			Travelled += Projectile.velocity.Length();
			if (!was && IsCaught)
			{
				SoundEngine.PlaySound(SoundID.Item32 with { Volume = 0.45f, Pitch = 0.3f }, Projectile.Center);
				if (!Main.dedServ)
					WindFX.Catch(Projectile.Center, Vector2.Normalize(Projectile.velocity));
			}

			// gravity per wind; an arrow carried by the trade wind stops falling - the wind holds it
			if (!IsCaught)
			{
				switch (Wind)
				{
					case Sun when Flight > 15f:
						Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.1f, 16f);
						break;
					case Frost when Flight > 14f:
						Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.14f, 16f);
						break;
					case Cleft:
						Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.06f, 16f);
						break;
				}
			}
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

			if (Wind == Spray && Flight >= SprayBurstAt)
			{
				Burst();
				Projectile.Kill();
				return;
			}

			Color c = WindFX.Of(Wind);
			Lighting.AddLight(Projectile.Center, c.ToVector3() * (Wind == Spray ? 0.12f : 0.3f));
			if (Main.dedServ)
				return;

			Vector2 back = -Projectile.velocity * 0.15f;
			switch (Wind)
			{
				case Spray when Main.rand.NextBool(2):
					ParticleSystem.Spawn(new Droplet(Projectile.Center, back + Main.rand.NextVector2Circular(1.2f, 1.2f),
						WindFX.SprayTeal, Main.rand.NextFloat(0.7f, 1.1f), 22));
					break;
				case Sun when Main.rand.NextBool(3):
					ParticleSystem.Spawn(new Spark(Projectile.Center, back + Main.rand.NextVector2Circular(0.8f, 0.8f),
						WindFX.SunGold, 0.7f, 14));
					break;
				case Frost when Main.rand.NextBool(5):
					ParticleSystem.Spawn(new Chip(Projectile.Center, back * 0.5f, WindFX.FrostIce, 0.7f, 34));
					break;
				case Cleft when Main.rand.NextBool(4):
					Dust.NewDustPerfect(Projectile.Center, DustID.PinkTorch, back, 120, default, 0.8f).noGravity = true;
					break;
			}
			if (IsCaught && (int)Flight % 4 == 0)
			{
				Vector2 side = Vector2.Normalize(Projectile.velocity).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-6f, 6f);
				ParticleSystem.Spawn(new Gust(Projectile.Center + side, Projectile.velocity * 0.25f, WindFX.Breeze * 0.8f,
					Main.rand.NextBool() ? 0.09f : -0.09f, 22, 1.2f));
			}
		}

		/// <summary>The spray's three pellets, flying on from where it burst with the distance it had.</summary>
		private void Burst()
		{
			if (!Main.dedServ)
			{
				ParticleSystem.Spawn(new Bloom(Projectile.Center, WindFX.SprayTeal, 0.2f, 0.45f, 10));
				for (int i = 0; i < 8; i++)
					ParticleSystem.Spawn(new Droplet(Projectile.Center,
						Projectile.velocity.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.2f, 0.5f), WindFX.SprayTeal, 1f, 26));
			}
			SoundEngine.PlaySound(SoundID.Item85 with { Volume = 0.4f, Pitch = 0.4f }, Projectile.Center);
			if (Projectile.owner != Main.myPlayer)
				return;
			for (int i = -1; i <= 1; i++)
			{
				Vector2 v = Projectile.velocity.RotatedBy(i * 0.2f + Main.rand.NextFloat(-0.03f, 0.03f)) * Main.rand.NextFloat(0.92f, 1.04f);
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, v,
					ModContent.ProjectileType<WindPellet>(), (int)(Projectile.damage * PelletDamage),
					Projectile.knockBack * 0.6f, Projectile.owner, Flags, Travelled);
			}
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (IsCaught)
				modifiers.SourceDamage *= TailwindBoost;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if ((Flags & FlagMark) != 0)
				target.AddBuff(ModContent.BuffType<RiftMark>(), 240);

			switch (Wind)
			{
				case Spray:
					Burst();
					Projectile.Kill();
					break;
				case Frost:
					target.AddBuff(BuffID.Frostburn, 150);
					break;
				case Cleft:
					if (!Main.dedServ)
						FX.Crack(target.Center, Projectile.velocity, 70f, WindFX.CleftPink with { A = 0 }, 36, 2f, 2);
					break;
				case Sun:
					target.AddBuff(BuffID.OnFire, 120);
					break;
			}

			if (IsCaught && (Flags & FlagStitch) != 0 && Stitched == 0f)
			{
				Stitched = 1f;
				Stitch(Projectile, target, (int)(Projectile.damage * StitchDamage));
			}
		}

		/// <summary>
		/// The Weft's thread, handed to a tailwind arrow: one Weft bolt from the target to the nearest
		/// other body. Its chain counter is set past the Weft's own limit so it jumps exactly once.
		/// </summary>
		public static void Stitch(Projectile from, NPC target, int damage)
		{
			if (from.owner != Main.myPlayer)
				return;
			NPC next = null;
			float best = 320f;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.CanBeChasedBy(from) || n.whoAmI == target.whoAmI)
					continue;
				float d = Vector2.Distance(n.Center, target.Center);
				if (d < best)
				{
					best = d;
					next = n;
				}
			}
			if (next == null)
				return;
			Vector2 aim = Vector2.Normalize(next.Center - target.Center) * 12f;
			Projectile.NewProjectile(from.GetSource_FromThis(), target.Center, aim, ModContent.ProjectileType<WeftBolt>(),
				damage, 1f, from.owner, WeftBolt.MaxChain + 10, target.whoAmI);
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			if (Wind == Sun && Bounces < SunBounces)
			{
				Bounces++;
				if (Projectile.velocity.X != oldVelocity.X)
					Projectile.velocity.X = -oldVelocity.X * 0.85f;
				if (Projectile.velocity.Y != oldVelocity.Y)
					Projectile.velocity.Y = -oldVelocity.Y * 0.85f;
				Flight = 1f;
				SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.5f, Pitch = 0.5f }, Projectile.Center);
				if (!Main.dedServ)
				{
					ParticleSystem.Spawn(new Bloom(Projectile.Center, WindFX.SunGold, 0.25f, 0.6f, 12));
					ParticleSystem.Spawn(new Shockwave(Projectile.Center, WindFX.SunGold, 0.05f, 0.4f, 14));
					FX.Sparks(Projectile.Center, WindFX.SunGold, 6, 4f, 0.8f, 16);
				}
				return false;
			}
			if (Wind == Spray)
				Burst();
			return true;
		}

		public override void OnKill(int timeLeft)
		{
			Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
			if (Wind == Frost && Projectile.owner == Main.myPlayer)
			{
				// the icicle hangs 15 tiles up and drops straight onto where the arrow broke
				Vector2 top = Projectile.Center - new Vector2(0f, 240f);
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), top, new Vector2(0f, 4f),
					ModContent.ProjectileType<WindIcicle>(), (int)(Projectile.damage * IcicleDamage), 2f,
					Projectile.owner, Projectile.Center.Y, Flags, Travelled);
			}
			if (Main.dedServ)
				return;
			Color c = WindFX.Of(Wind);
			switch (Wind)
			{
				case Sun:
					FX.Impact(Projectile.Center, WindFX.SunGold, WindFX.SunGold, 0.5f);
					break;
				case Frost:
					for (int i = 0; i < 6; i++)
						ParticleSystem.Spawn(new Chip(Projectile.Center, Main.rand.NextVector2Circular(2.5f, 2.5f) - new Vector2(0f, 1.5f), c, 0.9f, 40));
					break;
				case Cleft:
					ParticleSystem.Spawn(new Bloom(Projectile.Center, c, 0.2f, 0.5f, 12));
					FX.Sparks(Projectile.Center, c, 5, 3.5f, 0.7f, 14);
					break;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Color lit = lightColor;
			switch (Wind)
			{
				case Spray:
					// foam: dim, short, and widening toward the tail like spray off a bow wave
					RiftDraw.Trail(Projectile, f => RiftDraw.Glow(120, 225, 230, 0.28f * f),
						f => 0.55f + 0.75f * (1f - f), 0.55f);
					break;
				case Sun:
					// long and white-hot at the head, gold down the length
					RiftDraw.Trail(Projectile, f => Color.Lerp(new Color(255, 150, 60, 0), new Color(255, 250, 225, 0), f * f) * (0.55f * f),
						f => 0.75f + 0.25f * f);
					RiftDraw.Bloom(Projectile.Center, RiftDraw.Glow(255, 196, 90, 0.45f), 0.22f);
					break;
				case Frost:
					DrawFrozen(lit);
					break;
				case Cleft:
					DrawSeam();
					RiftDraw.Trail(Projectile, f => RiftDraw.Glow(255, 110, 210, f * f * 0.55f),
						f => 0.45f + 0.5f * f, 0.45f);
					break;
			}
			if (IsCaught)
				DrawRibbons(Projectile, Travelled);

			Color head = Wind == Sun || Wind == Cleft ? Color.Lerp(lit, Color.White, 0.6f) : lit;
			RiftDraw.Head(Projectile, Projectile.GetAlpha(head));
			return false;
		}

		/// <summary>Frost leaves solid afterimages every third step, not light - ice is a thing, not a glow.</summary>
		private void DrawFrozen(Color lit)
		{
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Rectangle frame = tex.Frame(1, 4, 0, Projectile.frame);
			Vector2 off = Projectile.Size * 0.5f - Main.screenPosition;
			Color ice = lit.MultiplyRGB(new Color(190, 235, 255));
			for (int i = 3; i < Projectile.oldPos.Length; i += 3)
			{
				if (Projectile.oldPos[i] == Vector2.Zero)
					continue;
				float f = 1f - i / (float)Projectile.oldPos.Length;
				Main.EntitySpriteDraw(tex, Projectile.oldPos[i] + off, frame, ice * (0.55f * f), Projectile.oldRot[i],
					frame.Size() * 0.5f, Projectile.scale * (0.6f + 0.35f * f), SpriteEffects.None, 0);
			}
		}

		/// <summary>The cleft draws the cut it made: one straight seam from the oldest point to the head.</summary>
		private void DrawSeam()
		{
			Vector2 half = Projectile.Size * 0.5f;
			for (int i = Projectile.oldPos.Length - 1; i >= 4; i--)
			{
				if (Projectile.oldPos[i] == Vector2.Zero)
					continue;
				RiftDraw.Line(Projectile.oldPos[i] + half, Projectile.Center, RiftDraw.Glow(255, 120, 215, 0.35f), 2f);
				RiftDraw.Line(Projectile.oldPos[i] + half, Projectile.Center, RiftDraw.Glow(255, 255, 255, 0.35f), 0.8f);
				break;
			}
		}

		/// <summary>
		/// The trade wind's own mark, on top of whichever wind is riding it: two white ribbons
		/// weaving either side of the path. Nothing else in the tree draws a ribbon, so it reads
		/// as "this one is carried" at any distance.
		/// </summary>
		public static void DrawRibbons(Projectile p, float phase)
		{
			Vector2 half = p.Size * 0.5f;
			int n = p.oldPos.Length;
			for (int side = -1; side <= 1; side += 2)
			{
				Vector2 prev = Vector2.Zero;
				for (int i = 0; i < n; i++)
				{
					Vector2 at = i == 0 ? p.Center : p.oldPos[i] + half;
					if (i > 0 && p.oldPos[i] == Vector2.Zero)
						break;
					Vector2 dir = i == 0 ? p.velocity : (i == 1 ? p.Center - at : p.oldPos[i - 1] - p.oldPos[i]);
					Vector2 normal = dir.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
					float f = 1f - i / (float)n;
					Vector2 q = at + normal * side * (float)Math.Sin(i * 0.8f + phase * 0.05f) * 5f * (1f - f * 0.6f);
					if (i > 0)
						RiftDraw.Line(prev, q, RiftDraw.Glow(235, 248, 255, 0.5f * f), 1.6f * f + 0.4f);
					prev = q;
				}
			}
		}
	}

	/// <summary>The spray's pellet: a shard of shell and sea water. Falls quickly, hits once.</summary>
	public class WindPellet : ModProjectile
	{
		private int Flags => (int)Projectile.ai[0];
		private ref float Travelled => ref Projectile.ai[1];

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 6;
			ProjectileID.Sets.TrailingMode[Type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 70;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void AI()
		{
			bool was = WindArrow.Caught(Flags, Travelled);
			Travelled += Projectile.velocity.Length();
			if (!was && WindArrow.Caught(Flags, Travelled) && !Main.dedServ)
				WindFX.Catch(Projectile.Center, Vector2.Normalize(Projectile.velocity));
			if (!WindArrow.Caught(Flags, Travelled))
				Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.13f, 14f);
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			if (!Main.dedServ && Main.rand.NextBool(3))
				ParticleSystem.Spawn(new Droplet(Projectile.Center, -Projectile.velocity * 0.1f, WindFX.SprayTeal, 0.7f, 18));
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (WindArrow.Caught(Flags, Travelled))
				modifiers.SourceDamage *= WindArrow.TailwindBoost;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if ((Flags & WindArrow.FlagMark) != 0)
				target.AddBuff(ModContent.BuffType<RiftMark>(), 240);
		}

		public override void OnKill(int timeLeft)
		{
			if (Main.dedServ)
				return;
			for (int i = 0; i < 4; i++)
				ParticleSystem.Spawn(new Droplet(Projectile.Center, Main.rand.NextVector2Circular(2f, 2f) - new Vector2(0f, 1f),
					WindFX.SprayTeal, 0.9f, 24));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			RiftDraw.Trail(Projectile, f => RiftDraw.Glow(120, 225, 230, 0.22f * f), f => 0.5f + 0.5f * (1f - f), 0.7f);
			if (WindArrow.Caught(Flags, Travelled))
				WindArrow.DrawRibbons(Projectile, Travelled);
			RiftDraw.Head(Projectile, Projectile.GetAlpha(lightColor));
			return false;
		}
	}

	/// <summary>
	/// The frost wind's icicle. It hangs above the spot the arrow broke and drops straight down;
	/// it passes through the ceiling it started in and only meets tiles once it is level with the
	/// point it is falling onto (ai[0]), so a frost arrow under a roof still lands its icicle.
	/// </summary>
	public class WindIcicle : ModProjectile
	{
		private float TargetY => Projectile.ai[0];

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 6;
			ProjectileID.Sets.TrailingMode[Type] = 2;
		}

		public override void SetDefaults()
		{
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 90;
			Projectile.tileCollide = false;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.alpha = 255;
		}

		public override void AI()
		{
			Projectile.alpha = Math.Max(0, Projectile.alpha - 40);
			Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.6f, 18f);
			Projectile.velocity.X = 0f;
			Projectile.rotation = 0f;
			if (Projectile.Center.Y > TargetY - 16f)
				Projectile.tileCollide = true;
			Lighting.AddLight(Projectile.Center, 0.15f, 0.3f, 0.4f);
			if (!Main.dedServ && Main.rand.NextBool(3))
				ParticleSystem.Spawn(new Chip(Projectile.Center, new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), -1f), WindFX.FrostIce, 0.6f, 24));
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (WindArrow.Caught((int)Projectile.ai[1], Projectile.ai[2]))
				modifiers.SourceDamage *= WindArrow.TailwindBoost;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.Frostburn, 180);

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.5f }, Projectile.Center);
			if (Main.dedServ)
				return;
			ParticleSystem.Spawn(new Shockwave(Projectile.Bottom, WindFX.FrostIce, 0.05f, 0.6f, 18, 0.35f));
			for (int i = 0; i < 9; i++)
				ParticleSystem.Spawn(new Chip(Projectile.Bottom, new Vector2(Main.rand.NextFloat(-3f, 3f), -Main.rand.NextFloat(1f, 4f)),
					WindFX.FrostIce, 1f, 44));
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// a falling icicle smears straight up: a cold streak, not a glow, under a lit body
			RiftDraw.Trail(Projectile, f => new Color(170, 230, 255) * (0.35f * f * Projectile.Opacity), f => 0.8f + 0.2f * f, 0.7f);
			RiftDraw.Head(Projectile, Projectile.GetAlpha(Color.Lerp(lightColor, Color.White, 0.4f)));
			return false;
		}
	}
}
