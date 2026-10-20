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
	/// The Keeper of the Rift's scythe: a crescent of its shell plates round an eye that is still
	/// open. After the cut, five rift shards burst out of the point in a fan along the stroke - the
	/// Keeper's own volley, turned round.
	/// </summary>
	public class KeepersHarvest : ScytheItem
	{
		public override float Reach => 104f;

		public override float RangeTiles => 18f;

		public override Color SlashColor => new(255, 154, 217);

		protected override void SetScytheDefaults()
		{
			Item.width = 52;
			Item.height = 52;
			Item.damage = 52;
			Item.knockBack = 5.5f;
			Item.useTime = 25;
			Item.useAnimation = 25;
			Item.value = Item.sellPrice(gold: 4);
			Item.rare = ItemRarityID.Pink;
		}

		/// <summary>Five rift shards out of the point, fanned along the stroke.</summary>
		public override void AfterStrike(Player player, Projectile slash, Vector2 aim, bool edge)
		{
			for (int i = 0; i < 5; i++)
				Projectile.NewProjectile(slash.GetSource_FromThis(), slash.Center,
					aim.RotatedBy((i - 2) * 0.28f) * 9f, ModContent.ProjectileType<ReapBolt>(),
					(int)(slash.damage * 0.3f), slash.knockBack * 0.3f, player.whoAmI, ReapBolt.Rift, ReapBolt.FromSwing);
		}
	}
}
