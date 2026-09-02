using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Armor
{
	[AutoloadEquip(EquipType.Body)]
	public class GlassblowerRobe : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 20;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Green;
			Item.defense = 6;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Magic) += 0.07f;
			player.statManaMax2 += 20;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Riftglass>(16)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
