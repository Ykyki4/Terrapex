using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Weapons.Trees.Magic
{
	/// <summary>
	/// Node 2, after the mechanical bosses. The Almanac clasped to a second book by the
	/// Riftflow's crystal. The pages stop turning on their own: right-click turns them, so the
	/// player picks the page. And every page now has a held variant - hold until the ring
	/// closes on the book and it lets go by itself, the page's bigger cast plus a short
	/// Riftflow lance down the aim. A tap is still the plain page.
	/// </summary>
	public class EquinoxAlmanac : AlmanacItem
	{
		public override int Level => 2;

		public override void SetDefaults()
		{
			Item.width = 40;
			Item.height = 36;
			BookDefaults(damage: 64, mana: 14, useTime: 24, knockBack: 4f);
			Item.value = Item.sellPrice(gold: 10);
			Item.rare = ItemRarityID.LightPurple;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Almanac>()
				.AddIngredient<Riftflow>()
				.AddIngredient(ItemID.SoulofMight, 5)
				.AddIngredient(ItemID.SoulofSight, 5)
				.AddIngredient(ItemID.SoulofFright, 5)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}
}
