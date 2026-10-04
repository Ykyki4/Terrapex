using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Reaper;

namespace Terrapex.Content.Items.Weapons.Reaper
{
	/// <summary>
	/// Crimson: crimtane and tissue. The Rotreaper's twin across the evil biomes, and the opposite
	/// answer - it leaves nothing behind, it takes something back. Edge hits drink a little life, inside
	/// vanilla's own life-steal budget so it never outpaces the Vampire Knives.
	/// </summary>
	public class Veinreaper : ScytheItem
	{
		public override float Reach => 90f;

		public override float RangeTiles => 13f;

		public override Color SlashColor => new(220, 70, 70);

		protected override void SetScytheDefaults()
		{
			Item.width = 40;
			Item.height = 40;
			Item.damage = 21;
			Item.knockBack = 5f;
			Item.useTime = 28;
			Item.useAnimation = 28;
			Item.value = Item.sellPrice(silver: 60);
			Item.rare = ItemRarityID.Blue;
		}

		public override void OnScytheHit(Player player, Projectile stroke, NPC target, NPC.HitInfo hit,
			bool edge, bool reap)
		{
			if (!edge || target.lifeMax <= 5 || target.SpawnedFromStatue || player.lifeSteal <= 0f)
				return;
			int heal = reap ? 3 : 1;
			player.lifeSteal -= heal;
			player.Heal(heal);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.CrimtaneBar, 10)
				.AddIngredient(ItemID.TissueSample, 6)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
