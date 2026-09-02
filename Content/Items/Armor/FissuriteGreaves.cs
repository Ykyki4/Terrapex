using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Armor
{
	[AutoloadEquip(EquipType.Legs)]
	public class FissuriteGreaves : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 18;
			Item.value = Item.sellPrice(silver: 38);
			Item.rare = ItemRarityID.Blue;
			Item.defense = 3;
		}

		public override void UpdateEquip(Player player)
		{
			player.moveSpeed += 0.06f;
			player.GetAttackSpeed(DamageClass.Melee) += 0.05f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<FissuriteBar>(), 15)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
