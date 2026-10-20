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
using Blunderbuss = Terrapex.Content.Items.Weapons.Shellback.BarnacleBlunderbuss;
using Sunwing = Terrapex.Content.Items.Weapons.Scarab.SunwingBow;
using Hoarfrost = Terrapex.Content.Items.Weapons.Rime.HoarfrostBow;
using CleftBow = Terrapex.Content.Items.Weapons.Cleft;
using RibBow = Terrapex.Content.Items.Weapons.Rib;
using WeftGun = Terrapex.Content.Items.Weapons.Weft;

namespace Terrapex.Content.Projectiles.Trees.Ranged
{
	/// <summary>
	/// The Horizon's volley - the tree's Zenith moment. A rose of eight bows opens over the
	/// player, each drawn from the ingredient's own item texture, and in turn every bow looses its
	/// own arrow at the cursor: the four winds from the Wayfinder's four, the Rib's marking arrow,
	/// the Weft's bolt, the Shotbow's chlorophyte arrow and the Phantasm's luminite arrow. Last the
	/// eye at the centre - the Unblinking - lays a <see cref="HorizonGaze"/> down the same line.
	///
	/// ai[0] is the aim angle; the owner keeps it on the cursor and syncs it now and then, so other
	/// clients see the bows turn roughly where they are aimed.
	/// </summary>
	public class HorizonRose : ModProjectile
	{
		public const int Bows = 8;
		public const int OpenTicks = 10;
		public const int Stagger = 3;
		public const int Life = 58;
		public const float Radius = 46f;
		public const float Lift = 78f;
		public const float ArrowDamage = 0.6f;
		public const float GazeDamage = 1.5f;
		public const float ArrowSpeed = 16f;

		public override string Texture => "Terrapex/Content/Projectiles/RiftFlare";

		private ref float Aim => ref Projectile.ai[0];
		private ref float Timer => ref Projectile.localAI[0];

		public static int GazeAt => OpenTicks + Bows * Stagger + 3;

		public static int BowItem(int i) => i switch
		{
			0 => ModContent.ItemType<Blunderbuss>(),
			1 => ModContent.ItemType<Sunwing>(),
			2 => ModContent.ItemType<Hoarfrost>(),
			3 => ModContent.ItemType<CleftBow>(),
			4 => ModContent.ItemType<RibBow>(),
			5 => ModContent.ItemType<WeftGun>(),
			6 => ItemID.ChlorophyteShotbow,
			_ => ItemID.Phantasm,
		};

		private static Color BowColor(int i) => i switch
		{
			< 4 => WindFX.Of(i),
			4 => new Color(210, 120, 235),
			5 => new Color(90, 220, 200),
			6 => new Color(140, 235, 90),
			_ => new Color(120, 220, 255),
		};

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Life;
			Projectile.DamageType = DamageClass.Ranged;
		}

		public override bool? CanDamage() => false;

		private float Open
		{
			get
			{
				float o = Math.Min(1f, Timer / OpenTicks);
				float c = Math.Min(1f, Projectile.timeLeft / 10f);
				float e = 1f - (float)Math.Pow(1f - Math.Min(o, c), 3);
				return e;
			}
		}

		public Vector2 BowAt(int i)
		{
			float a = -MathHelper.PiOver2 + MathHelper.TwoPi * i / Bows + Timer * 0.01f;
			return Projectile.Center + a.ToRotationVector2() * Radius * Open;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead)
			{
				Projectile.Kill();
				return;
			}
			Projectile.Center = owner.MountedCenter - new Vector2(0f, Lift);
			Projectile.velocity = Vector2.Zero;
			Timer++;

			if (Projectile.owner == Main.myPlayer)
			{
				float want = (Main.MouseWorld - Projectile.Center).ToRotation();
				if (Math.Abs(MathHelper.WrapAngle(want - Aim)) > 0.05f && (int)Timer % 6 == 0)
					Projectile.netUpdate = true;
				Aim = want;
			}

			if (Timer == 1f)
			{
				SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.55f, Pitch = 0.2f }, Projectile.Center);
				if (!Main.dedServ)
				{
					ParticleSystem.Spawn(new Shockwave(Projectile.Center, WindFX.Regard, 0.1f, 1.3f, 24));
					ParticleSystem.Spawn(new Bloom(Projectile.Center, WindFX.Breeze, 0.5f, 1.1f, 18));
				}
			}

			for (int i = 0; i < Bows; i++)
				if (Timer == OpenTicks + i * Stagger)
					Fire(i);

			if (Timer == GazeAt)
			{
				SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.7f }, Projectile.Center);
				if (Projectile.owner == Main.myPlayer)
					Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
						ModContent.ProjectileType<HorizonGaze>(), (int)(Projectile.damage * GazeDamage),
						Projectile.knockBack, Projectile.owner, Aim);
			}

			Lighting.AddLight(Projectile.Center, 0.6f * Open, 0.5f * Open, 0.35f * Open);
		}

		private void Fire(int i)
		{
			Vector2 at = BowAt(i);
			SoundEngine.PlaySound((i == 0 ? SoundID.Item36 : SoundID.Item5) with { Volume = 0.45f }, at);
			if (!Main.dedServ)
			{
				ParticleSystem.Spawn(new Bloom(at, BowColor(i), 0.25f, 0.6f, 12));
				FX.Sparks(at, BowColor(i), 4, 4f, 0.8f, 14, 0f, 0.8f, Aim);
			}
			if (Projectile.owner != Main.myPlayer)
				return;

			Vector2 v = Vector2.Normalize(Main.MouseWorld - at) * ArrowSpeed;
			if (v.HasNaNs())
				v = Aim.ToRotationVector2() * ArrowSpeed;
			int damage = (int)(Projectile.damage * ArrowDamage);
			var src = Projectile.GetSource_FromThis();
			int flags = WindArrow.FlagTailwind | WindArrow.FlagStitch;
			switch (i)
			{
				case < 4:
					Projectile.NewProjectile(src, at, v, ModContent.ProjectileType<WindArrow>(), damage,
						Projectile.knockBack, Projectile.owner, i, flags);
					break;
				case 4:
					Projectile.NewProjectile(src, at, v, ModContent.ProjectileType<RibArrow>(), damage, Projectile.knockBack, Projectile.owner);
					break;
				case 5:
					Projectile.NewProjectile(src, at, v * 0.75f, ModContent.ProjectileType<WeftBolt>(), damage, Projectile.knockBack, Projectile.owner);
					break;
				case 6:
					Projectile.NewProjectile(src, at, v, ProjectileID.ChlorophyteArrow, damage, Projectile.knockBack, Projectile.owner);
					break;
				default:
					Projectile.NewProjectile(src, at, v, ProjectileID.MoonlordArrow, damage, Projectile.knockBack, Projectile.owner);
					break;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float open = Open;
			if (open <= 0.01f)
				return false;
			Vector2 c = Projectile.Center;

			// the compass behind it: eight spokes out to the bows, and the ring they sit on
			for (int i = 0; i < Bows; i++)
			{
				Vector2 b = BowAt(i);
				RiftDraw.Line(c, b, RiftDraw.Glow(255, 215, 160, 0.22f * open), i % 2 == 0 ? 2f : 1.2f);
			}
			RiftDraw.Ring(c, RiftDraw.Glow(235, 240, 255, 0.35f * open), 0.42f * open, Timer * 0.02f);
			RiftDraw.Bloom(c, RiftDraw.Glow(255, 200, 130, 0.35f * open), 0.6f * open);

			for (int i = 0; i < Bows; i++)
				DrawBow(i, open);

			DrawEye(c, open);
			return false;
		}

		private void DrawBow(int i, float open)
		{
			int type = BowItem(i);
			Main.instance.LoadItem(type);
			Texture2D tex = TextureAssets.Item[type].Value;
			Vector2 at = BowAt(i);
			Vector2 toward = (Projectile.owner == Main.myPlayer ? Main.MouseWorld : Projectile.Center + Aim.ToRotationVector2() * 400f) - at;
			float rot = toward.ToRotation();
			bool left = Math.Cos(rot) < 0;
			float fit = Math.Min(1f, 40f / Math.Max(tex.Width, tex.Height));

			// a bow that has already loosed dims a little - the volley reads as a sweep round the rose
			bool fired = Timer >= OpenTicks + i * Stagger;
			float bright = open * (fired ? 0.75f : 1f);

			RiftDraw.Bloom(at, RiftDraw.Glow(BowColor(i).R, BowColor(i).G, BowColor(i).B, 0.3f * bright), 0.32f);
			Main.EntitySpriteDraw(tex, at - Main.screenPosition, null, Color.White * bright, rot, tex.Size() * 0.5f,
				fit * (0.6f + 0.4f * open), left ? SpriteEffects.FlipVertically : SpriteEffects.None, 0);
		}

		/// <summary>The Unblinking at the centre: pale shell, warm iris, opening wider as the gaze comes.</summary>
		private void DrawEye(Vector2 c, float open)
		{
			float charge = MathHelper.Clamp(Timer / GazeAt, 0f, 1f);
			Texture2D px = TextureAssets.MagicPixel.Value;
			Rectangle src = new(0, 0, 1, 1);
			Vector2 s = c - Main.screenPosition;
			float w = 22f * open, h = (4f + 9f * charge) * open;
			Main.EntitySpriteDraw(px, s, src, new Color(238, 241, 248) * open, Aim, new Vector2(0.5f), new Vector2(w, h), SpriteEffects.None, 0);
			Main.EntitySpriteDraw(px, s, src, new Color(255, 159, 74) * open, Aim, new Vector2(0.5f), new Vector2(h * 0.9f, h * 0.9f), SpriteEffects.None, 0);
			Main.EntitySpriteDraw(px, s, src, new Color(34, 38, 60) * open, Aim, new Vector2(0.5f), new Vector2(h * 0.35f, h * 0.35f), SpriteEffects.None, 0);
			RiftDraw.Bloom(c, RiftDraw.Glow(255, 215, 154, 0.4f * charge * open), 0.25f + 0.25f * charge);
		}
	}

	/// <summary>
	/// The Unblinking's stare, fired from the centre of the Horizon's rose: one straight line,
	/// lethal for 22 ticks, hitting each enemy on it once. A player's weapon, so it needs no tell -
	/// the rose opening over the player is the tell for everyone watching.
	/// </summary>
	public class HorizonGaze : ModProjectile
	{
		public const float Length = 1100f;
		public const int Life = 22;

		public override string Texture => "Terrapex/Content/Projectiles/RiftFlare";

		private float Angle => Projectile.ai[0];
		private Vector2 Start => Projectile.Center;
		private Vector2 End => Projectile.Center + Angle.ToRotationVector2() * Length;

		public override void SetDefaults()
		{
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Life;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.hide = false;
		}

		public override void AI()
		{
			Projectile.velocity = Vector2.Zero;
			if (Projectile.localAI[0]++ == 0f && !Main.dedServ)
			{
				ParticleSystem.Spawn(new Bloom(Start, WindFX.Regard, 0.5f, 1.2f, 16));
				for (int i = 1; i <= 10; i++)
					FX.Sparks(Vector2.Lerp(Start, End, i / 10f), WindFX.Regard, 2, 3f, 0.7f, 16);
			}
			DelegateMethods.v3_1 = new Vector3(0.9f, 0.7f, 0.4f) * (Projectile.timeLeft / (float)Life);
			Utils.PlotTileLine(Start, End, 12f, DelegateMethods.CastLight);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			float _ = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Start, End, 18f, ref _);
		}

		public override void CutTiles()
		{
			DelegateMethods.tilecut_0 = Terraria.Enums.TileCuttingContext.AttackProjectile;
			Utils.PlotTileLine(Start, End, 16f, DelegateMethods.CutTiles);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			float life = Projectile.timeLeft / (float)Life;
			float swell = (float)Math.Sin(Math.Min(1f, (1f - life) * 4f) * MathHelper.PiOver2);
			float k = swell * life;
			RiftDraw.Line(Start, End, RiftDraw.Glow(255, 159, 74, 0.35f * k), 18f * k);
			RiftDraw.Line(Start, End, RiftDraw.Glow(255, 215, 154, 0.6f * k), 8f * k);
			RiftDraw.Line(Start, End, RiftDraw.Glow(255, 255, 255, 0.9f * k), 3f * k);
			RiftDraw.Bloom(Start, RiftDraw.Glow(255, 215, 154, 0.6f * k), 0.5f);
			return false;
		}
	}
}
