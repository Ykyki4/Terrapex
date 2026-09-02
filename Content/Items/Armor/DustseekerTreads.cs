using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Armor
{
	[AutoloadEquip(EquipType.Legs)]
	public class DustseekerTreads : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 20;
			Item.value = Item.sellPrice(gold: 1, silver: 10);
			Item.rare = ItemRarityID.Orange;
			Item.defense = 5;
		}

		public override void UpdateEquip(Player player)
		{
			player.moveSpeed += 0.12f;
			player.GetCritChance(DamageClass.Ranged) += 3;
			player.pickSpeed -= 0.10f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Riftglass>(10)
				.AddIngredient<RiftDust>(24)
				.AddIngredient(ItemID.Bone, 20)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
