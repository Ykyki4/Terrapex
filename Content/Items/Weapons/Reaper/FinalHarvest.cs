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
	/// Luminite, at the Ancient Manipulator: the last scythe a reaper forges. The four pillars'
	/// light banded down one long blade.
	/// </summary>
	public class FinalHarvest : ScytheItem
	{
		public override float Reach => 130f;

		public override float RangeTiles => 24f;

		public override Color SlashColor => new(180, 255, 230);

		protected override void SetScytheDefaults()
		{
			Item.width = 60;
			Item.height = 60;
			Item.damage = 165;
			Item.knockBack = 6f;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.value = Item.sellPrice(gold: 20);
			Item.rare = ItemRarityID.Red;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.LunarBar, 14)
				.AddTile(TileID.LunarCraftingStation)
				.Register();
		}
	}
}
