using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons.Trees.Magic
{
	/// <summary>
	/// Node 3, after Plantera. A codex open at two pages, the sun on one and the moon on the
	/// other, held apart by the Loom Staff's frame. Every cast is the chosen page and the page
	/// opposite it - Tide with Snow, Sun with Scree - so turning the page now picks a pair.
	/// A full charge keeps the Equinox's lance and strings the Loom's thread across the cursor.
	/// </summary>
	public class Solstice : AlmanacItem
	{
		public override int Level => 3;

		public override void SetDefaults()
		{
			Item.width = 44;
			Item.height = 40;
			BookDefaults(damage: 92, mana: 16, useTime: 24, knockBack: 4f);
			Item.value = Item.sellPrice(gold: 25);
			Item.rare = ItemRarityID.Yellow;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<EquinoxAlmanac>()
				.AddIngredient<LoomStaff>()
				.AddIngredient(ItemID.LeafBlower)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
