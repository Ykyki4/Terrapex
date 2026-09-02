using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Armor
{
	[AutoloadEquip(EquipType.Body)]
	public class DustseekerGarb : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 22;
			Item.value = Item.sellPrice(gold: 1, silver: 40);
			Item.rare = ItemRarityID.Orange;
			Item.defense = 9;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Ranged) += 0.08f;
			player.statLifeMax2 += 20;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Riftglass>(12)
				.AddIngredient<RiftDust>(30)
				.AddIngredient(ItemID.Bone, 25)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
