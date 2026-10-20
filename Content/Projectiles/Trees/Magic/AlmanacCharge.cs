using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Particles;
using Terrapex.Content.Items.Weapons.Trees.Magic;

namespace Terrapex.Content.Projectiles.Trees.Magic
{
	/// <summary>
	/// The held half of every book from the Equinox up. It lives while the button is down, sits
	/// on the book, and owns the cast: let go early and the page casts plain; hold until the ring
	/// closes onto the book and it lets go by itself with the held variant. The auto-release is
	/// deliberate - a magic weapon is held, and a charge that waited for release forever would
	/// turn holding the button into not casting at all.
	///
	/// The tell is a ring in the page's colour shrinking onto the book: it is the shape the mod
	/// already uses for "this is about to happen", and it says how long is left without a number.
	/// </summary>
	public class AlmanacCharge : ModProjectile
	{
		public const int ChargeTicks = 48;

		public override string Texture => "Terrapex/Content/Projectiles/Trees/Magic/AlmanacGlyphs";

		private ref float Timer => ref Projectile.ai[0];
		private int Level => (int)Projectile.ai[1];

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = false;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 2;
		}

		public override bool? CanDamage() => false;

		// the centre is set from the player every tick; velocity only carries the aim
		public override bool ShouldUpdatePosition() => false;

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.ModItem is not AlmanacItem)
			{
				Projectile.Kill();
				return;
			}

			if (Projectile.owner == Main.myPlayer)
			{
				Vector2 aim = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(Vector2.UnitX * owner.direction);
				if (Vector2.DistanceSquared(aim, Projectile.velocity) > 0.0004f)
					Projectile.netUpdate = true;
				Projectile.velocity = aim;
			}

			Projectile.Center = owner.MountedCenter + Projectile.velocity * 18f;
			owner.heldProj = Projectile.whoAmI;
			owner.itemTime = owner.itemAnimation = 2;
			owner.ChangeDir(Projectile.velocity.X > 0f ? 1 : -1);
			owner.itemRotation = MathHelper.WrapAngle(Projectile.velocity.ToRotation()
				+ (owner.direction < 0 ? MathHelper.Pi : 0f));

			AlmanacPlayer book = owner.GetModPlayer<AlmanacPlayer>();
			book.charge = Math.Min(1f, Timer / ChargeTicks);

			Color tint = AlmanacItem.PageColor[book.page];
			Lighting.AddLight(Projectile.Center, tint.ToVector3() * (0.25f + 0.45f * book.charge));

			bool held = owner.channel;
			if (held && Timer < ChargeTicks)
			{
				Timer++;
				Projectile.timeLeft = 2;
				// motes drawn in from a ring onto the book, thickening as it fills
				if (Main.rand.NextFloat() < 0.25f + 0.6f * book.charge)
				{
					Vector2 off = Main.rand.NextVector2CircularEdge(1f, 1f) * MathHelper.Lerp(60f, 22f, book.charge);
					ParticleSystem.Spawn(new Spark(Projectile.Center + off, -off * 0.07f,
						tint, 0.7f, 14));
				}
				return;
			}

			if (Projectile.owner == Main.myPlayer)
			{
				bool charged = Timer >= ChargeTicks && owner.CheckMana(owner.HeldItem, -1, true);
				AlmanacItem.Release(Projectile.GetSource_FromThis(), owner, Level, charged,
					Projectile.damage, Projectile.knockBack, Main.MouseWorld);
				if (charged)
					FX.Impact(Projectile.Center, new Color(255, 240, 250), tint, 0.8f);
			}
			Projectile.Kill();
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Player owner = Main.player[Projectile.owner];
			AlmanacPlayer book = owner.GetModPlayer<AlmanacPlayer>();
			float c = Math.Min(1f, Timer / ChargeTicks);
			Color tint = AlmanacItem.PageColor[book.page];

			// the closing ring: wide and faint at first, tight and bright when it lands
			float s = MathHelper.Lerp(1.5f, 0.32f, c);
			RiftDraw.Ring(Projectile.Center, new Color(tint.R, tint.G, tint.B, 0) * (0.25f + 0.6f * c), s,
				Main.GlobalTimeWrappedHourly * 2f);
			RiftDraw.Bloom(Projectile.Center, new Color(tint.R, tint.G, tint.B, 0) * (0.15f + 0.45f * c),
				0.25f + 0.35f * c);
			return false;
		}

		public override void OnKill(int timeLeft)
		{
			if (Timer >= ChargeTicks)
				SoundEngine.PlaySound(SoundID.MaxMana with { Volume = 0.5f }, Projectile.Center);
		}
	}
}
