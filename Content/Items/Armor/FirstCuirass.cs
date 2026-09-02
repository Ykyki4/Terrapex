using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>Plan item #123's body. Thirty of the set's seventy-eight defence lives here.</summary>
	[AutoloadEquip(EquipType.Body)]
	public class FirstCuirass : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 34;
			Item.height = 26;
			Item.value = Item.sellPrice(gold: 30);
			Item.rare = ItemRarityID.Red;
			Item.defense = 30;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Generic) += 0.10f;
			player.GetArmorPenetration(DamageClass.Generic) += 8f;
			player.statLifeMax2 += 100;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<PrimordiumBar>(22)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
