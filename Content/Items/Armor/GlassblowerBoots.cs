using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Armor
{
	[AutoloadEquip(EquipType.Legs)]
	public class GlassblowerBoots : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 18;
			Item.value = Item.sellPrice(silver: 85);
			Item.rare = ItemRarityID.Green;
			Item.defense = 3;
		}

		public override void UpdateEquip(Player player)
		{
			player.manaCost -= 0.08f;
			player.moveSpeed += 0.08f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Riftglass>(12)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
