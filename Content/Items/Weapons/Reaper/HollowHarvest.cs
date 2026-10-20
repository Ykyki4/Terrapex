using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Reaper;

namespace Terrapex.Content.Items.Weapons.Reaper
{
	/// <summary>
	/// The Hollow Harvester's own scythe, its guaranteed drop. T2. After the cut, three straw crows
	/// burst out of the point in a fan along the stroke - the crows it threw at you, flying for
	/// you now.
	/// </summary>
	public class HollowHarvest : ScytheItem
	{
		public override float Reach => 98f;

		public override float RangeTiles => 15f;

		public override Color SlashColor => new(190, 236, 200);

		/// <summary>Three crows out of the point, fanned along the stroke.</summary>
		public override void AfterStrike(Player player, Projectile slash, Vector2 aim, bool edge)
		{
			for (int i = -1; i <= 1; i++)
				Projectile.NewProjectile(slash.GetSource_FromThis(), slash.Center, aim.RotatedBy(i * 0.35f) * 9f,
					ModContent.ProjectileType<ReapCrow>(), (int)(slash.damage * 0.35f), slash.knockBack * 0.3f, player.whoAmI);
		}

		protected override void SetScytheDefaults()
		{
			Item.width = 48;
			Item.height = 48;
			Item.damage = 30;
			Item.knockBack = 5f;
			Item.useTime = 25;
			Item.useAnimation = 25;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Orange;
		}
	}
}
