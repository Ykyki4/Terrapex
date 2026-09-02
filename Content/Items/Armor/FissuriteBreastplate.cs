using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Armor
{
	[AutoloadEquip(EquipType.Body)]
	public class FissuriteBreastplate : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 20;
			Item.value = Item.sellPrice(silver: 45);
			Item.rare = ItemRarityID.Blue;
			Item.defense = 4;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Melee) += 0.05f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<FissuriteBar>(), 20)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
