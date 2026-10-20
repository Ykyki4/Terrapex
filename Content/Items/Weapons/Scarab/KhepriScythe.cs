using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Weapons.Reaper;
using Terrapex.Content.Projectiles.Scarab;

namespace Terrapex.Content.Items.Weapons.Scarab
{
	/// <summary>
	/// The reaper's drop from the Scarab Sultan: a gold crescent with a sun-disc set in its neck,
	/// named for the scarab who rolls the sun. After the cut, a sun pebble drops out of the point
	/// and rolls on along the stroke by the Sultan's own ball rules; a cut on the edge drops two,
	/// one each way.
	/// </summary>
	public class KhepriScythe : ScytheItem
	{
		public const float PebbleShare = 0.5f;

		public override float Reach => 96f;

		public override float RangeTiles => 14f;

		public override Color SlashColor => new(255, 200, 90);

		public override void AfterStrike(Player player, Projectile slash, Vector2 aim, bool edge)
		{
			int side = aim.X >= 0f ? 1 : -1;
			int damage = (int)(slash.damage * PebbleShare);
			Drop(player, slash, side, damage);
			if (edge)
				Drop(player, slash, -side, damage);
		}

		private static void Drop(Player player, Projectile slash, int side, int damage)
		{
			Vector2 v = new(side * 6.5f, -3f);
			Projectile.NewProjectile(slash.GetSource_FromThis(), slash.Center, Vector2.Zero,
				ModContent.ProjectileType<SunPebble>(), damage, slash.knockBack * 0.4f, player.whoAmI, v.X, v.Y);
		}

		protected override void SetScytheDefaults()
		{
			Item.width = 48;
			Item.height = 48;
			Item.damage = 25;
			Item.knockBack = 5f;
			Item.useTime = 24;
			Item.useAnimation = 24;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Green;
		}
	}
}
