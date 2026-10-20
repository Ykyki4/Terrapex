using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Weapons.Rime;
using Terrapex.Content.Items.Weapons.Scarab;
using Terrapex.Content.Items.Weapons.Shellback;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons.Trees.Magic
{
	/// <summary>
	/// Node 1, the magic Night's Edge. Four tomes from four places bound into one: the cover
	/// carries the conch's shell, the scepter's sun, the staff's snow and the tome's scree, one
	/// in each corner. The book turns its own page every four casts - Tide, Sun, Snow, Scree -
	/// and the glyph over the player's head, with its four pips, says which and for how long.
	/// </summary>
	public class Almanac : AlmanacItem
	{
		public override int Level => 1;

		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 36;
			BookDefaults(damage: 36, mana: 10, useTime: 26, knockBack: 3.5f);
			Item.value = Item.sellPrice(gold: 4);
			Item.rare = ItemRarityID.Orange;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<BrineConch>()
				.AddIngredient<SunScepter>()
				.AddIngredient<SnowcapStaff>()
				.AddIngredient<ScreeTome>()
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
