using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Particles;
using Terrapex.Content.Projectiles.Trees.Ranged;

namespace Terrapex.Content.Items.Weapons.Trees.Ranged
{
	/// <summary>
	/// The ranged tree of CRAFTING_TREES.md - Wayfinder, Compass Rose, Trade Wind, Horizon. One
	/// bow growing, so one base class: every node keeps what the node below it does and switches
	/// on one thing more. What each node adds is a property here, read in <see cref="Shoot"/>.
	///
	/// Wooden arrows leave every bow of the tree as the four winds in turn - spray, sun, frost,
	/// cleft. Any other arrow flies as itself; the fan and the rose are always the winds, because
	/// the compass does not care what is in the quiver.
	/// </summary>
	public abstract class WindBow : ModItem
	{
		/// <summary>Every Nth shot is a fan of all four winds (0 = never). Compass Rose on.</summary>
		protected virtual int FanEvery => 0;

		/// <summary>Every Nth shot opens the rose of eight bows over the player (0 = never). Horizon.</summary>
		protected virtual int RoseEvery => 0;

		/// <summary>The wind flags every arrow this bow fires carries.</summary>
		protected virtual int ArrowFlags => 0;

		/// <summary>The Chlorophyte Shotbow's half: a second, lighter arrow of the opposite wind.</summary>
		protected virtual bool SecondArrow => false;

		public const float FanSpread = 0.16f;
		public const float SecondDamage = 0.5f;

		private int shot;
		private int wind;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			shot++;
			Vector2 muzzle = Vector2.Normalize(velocity) * 22f;
			if (Collision.CanHit(position, 0, 0, position + muzzle, 0, 0))
				position += muzzle;

			int arrow = ModContent.ProjectileType<WindArrow>();

			if (RoseEvery > 0 && shot % RoseEvery == 0)
			{
				Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero,
					ModContent.ProjectileType<HorizonRose>(), damage, knockback, player.whoAmI,
					velocity.ToRotation());
			}
			else if (FanEvery > 0 && shot % FanEvery == 0)
			{
				// the needle of the compass: all four winds at once, and they carry the Rib's mark
				for (int i = 0; i < 4; i++)
				{
					float t = i / 3f - 0.5f;
					Projectile.NewProjectile(source, position, velocity.RotatedBy(t * FanSpread * 3f), arrow,
						damage, knockback, player.whoAmI, i, ArrowFlags | WindArrow.FlagMark);
				}
				SoundEngine.PlaySound(SoundID.Item102 with { Volume = 0.7f }, position);
				if (!Main.dedServ)
					WindFX.CompassFlash(position, velocity.ToRotation());
				return false;
			}

			int current = wind;
			wind = (wind + 1) % 4;
			bool wooden = type == ProjectileID.WoodenArrowFriendly;

			Projectile.NewProjectile(source, position, velocity, wooden ? arrow : type, damage, knockback,
				player.whoAmI, wooden ? current : 0f, wooden ? ArrowFlags : 0f);

			if (SecondArrow)
			{
				Vector2 v2 = velocity.RotatedBy(0.07f * player.direction) * 0.94f;
				Projectile.NewProjectile(source, position, v2, wooden ? arrow : type, (int)(damage * SecondDamage),
					knockback * 0.5f, player.whoAmI, wooden ? (current + 2) % 4 : 0f, wooden ? ArrowFlags : 0f);
			}
			return false;
		}
	}
}
