using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Armor
{
	[AutoloadEquip(EquipType.Body)]
	public class RiftsteelBreastplate : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 24;
			Item.value = Item.sellPrice(gold: 5);
			Item.rare = ItemRarityID.Pink;
			Item.defense = 12;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Generic) += 0.06f;
			player.statLifeMax2 += 30;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<RiftsteelBar>(16)
				.AddIngredient<RiftEssence>(6)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}
}
