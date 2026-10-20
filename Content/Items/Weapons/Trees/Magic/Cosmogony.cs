using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Weapons.Trees.Magic
{
	/// <summary>
	/// Node 4, the magic Zenith. The book is open and its four pages have left it: they stand in
	/// a ring around the player, each with its glyph. A cast shuts the book - the pages slam in
	/// and reopen - and every page throws its own cast at the cursor from where it stood. The
	/// page right-click picks is the lit one and hits harder (0.85 of the book against 0.55). A full charge throws all four held variants, the
	/// Equinox lance and the Solstice thread, and opens the Regard's field on the cursor.
	/// </summary>
	public class Cosmogony : AlmanacItem
	{
		public override int Level => 4;

		public override void SetDefaults()
		{
			Item.width = 48;
			Item.height = 44;
			BookDefaults(damage: 175, mana: 20, useTime: 22, knockBack: 4.5f);
			Item.value = Item.sellPrice(gold: 60);
			Item.rare = ItemRarityID.Red;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Solstice>()
				.AddIngredient<Regard>()
				.AddIngredient(ItemID.RazorbladeTyphoon)
				.AddIngredient(ItemID.LunarBar, 10)
				.AddIngredient<PrimordiumBar>(5)
				.AddTile(TileID.LunarCraftingStation)
				.Register();
		}
	}
}
