using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Weapons.Reaper
{
	/// <summary>
	/// The class's first weapon, from wood and iron before any boss. It has the default Reap on
	/// purpose: the starter is where a player learns what a full Harvest does, so it does the plain
	/// thing every later scythe builds on.
	/// </summary>
	public class RustedSickle : ScytheItem
	{
		public override float Reach => 70f;

		public override float RangeTiles => 9f;

		protected override void SetScytheDefaults()
		{
			Item.width = 30;
			Item.height = 30;
			Item.damage = 9;
			Item.knockBack = 4f;
			Item.useTime = 26;
			Item.useAnimation = 26;
			Item.value = Item.sellPrice(silver: 20);
			Item.rare = ItemRarityID.White;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.Wood, 10)
				.AddRecipeGroup("IronBar", 6)
				.AddTile(TileID.WorkBenches)
				.Register();
		}
	}
}
