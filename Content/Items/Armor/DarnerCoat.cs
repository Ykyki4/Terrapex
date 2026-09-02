using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>Plan item #84.</summary>
	[AutoloadEquip(EquipType.Body)]
	public class DarnerCoat : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 24;
			Item.value = Item.sellPrice(gold: 8);
			Item.rare = ItemRarityID.Yellow;
			Item.defense = 18;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Generic) += 0.06f;
			player.endurance += 0.04f;
			player.statLifeMax2 += 20;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<VoidCloth>(12)
				.AddIngredient(ItemID.HallowedBar, 12)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
