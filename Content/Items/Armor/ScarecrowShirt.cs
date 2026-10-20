using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;

namespace Terrapex.Content.Items.Armor
{
	[AutoloadEquip(EquipType.Body)]
	public class ScarecrowShirt : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 20;
			Item.value = Item.sellPrice(silver: 25);
			Item.rare = ItemRarityID.White;
			Item.defense = 3;
		}

		public override void UpdateEquip(Player player) => player.GetDamage<ReapingDamage>() += 0.05f;

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.Hay, 20)
				.AddIngredient(ItemID.Silk, 4)
				.AddIngredient(ItemID.Wood, 6)
				.AddTile(TileID.Loom)
				.Register();
		}
	}
}
