using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;

namespace Terrapex.Content.Items.Armor
{
	[AutoloadEquip(EquipType.Legs)]
	public class ScarecrowTrousers : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 18;
			Item.value = Item.sellPrice(silver: 20);
			Item.rare = ItemRarityID.White;
			Item.defense = 2;
		}

		public override void UpdateEquip(Player player) => player.moveSpeed += 0.06f;

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.Hay, 15)
				.AddIngredient(ItemID.Silk, 3)
				.AddTile(TileID.Loom)
				.Register();
		}
	}
}
