using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles.Reaper;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons.Reaper
{
	/// <summary>
	/// The First Keeper's scythe: white, with one warm eye at the neck, the colour that means being
	/// seen. After the cut the eye opens over the point and looks every way at once: a ring of six
	/// white rays.
	/// </summary>
	public class Lastlight : ScytheItem
	{
		public override float Reach => 124f;

		public override float RangeTiles => 25f;

		public override Color SlashColor => new(255, 215, 154);

		protected override void SetScytheDefaults()
		{
			Item.width = 56;
			Item.height = 56;
			Item.damage = 190;
			Item.knockBack = 6f;
			Item.useTime = 21;
			Item.useAnimation = 21;
			Item.value = Item.sellPrice(gold: 25);
			Item.rare = ItemRarityID.Purple;
		}

		/// <summary>A ring of six prime rays out of the point.</summary>
		public override void AfterStrike(Player player, Projectile slash, Vector2 aim, bool edge)
		{
			for (int i = 0; i < 6; i++)
				Projectile.NewProjectile(slash.GetSource_FromThis(), slash.Center,
					aim.RotatedBy(MathHelper.TwoPi * i / 6f) * 12f, ModContent.ProjectileType<ReapBolt>(),
					(int)(slash.damage * 0.3f), slash.knockBack * 0.3f, player.whoAmI, ReapBolt.Prime, ReapBolt.FromSwing);
		}
	}
}
