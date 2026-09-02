using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>Plan item #85.</summary>
	[AutoloadEquip(EquipType.Legs)]
	public class DarnerBoots : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 22;
			Item.value = Item.sellPrice(gold: 6);
			Item.rare = ItemRarityID.Yellow;
			Item.defense = 14;
		}

		public override void UpdateEquip(Player player)
		{
			player.moveSpeed += 0.12f;
			player.GetCritChance(DamageClass.Generic) += 4;
			player.GetAttackSpeed(DamageClass.Generic) += 0.05f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<VoidCloth>(8)
				.AddIngredient(ItemID.HallowedBar, 8)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
