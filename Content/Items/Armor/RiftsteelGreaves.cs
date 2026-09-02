using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Armor
{
	[AutoloadEquip(EquipType.Legs)]
	public class RiftsteelGreaves : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 22;
			Item.value = Item.sellPrice(gold: 4);
			Item.rare = ItemRarityID.Pink;
			Item.defense = 9;
		}

		public override void UpdateEquip(Player player)
		{
			player.moveSpeed += 0.14f;
			player.GetAttackSpeed(DamageClass.Generic) += 0.05f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<RiftsteelBar>(12)
				.AddIngredient<RiftEssence>(4)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}
}
