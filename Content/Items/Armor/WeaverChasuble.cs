using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>Plan item #103.</summary>
	[AutoloadEquip(EquipType.Body)]
	public class WeaverChasuble : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 24;
			Item.value = Item.sellPrice(gold: 16);
			Item.rare = ItemRarityID.Lime;
			Item.defense = 28;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Generic) += 0.08f;
			player.endurance += 0.07f;
			player.statLifeMax2 += 60;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<EchoAlloy>(16)
				.AddIngredient(ItemID.ChlorophyteBar, 10)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
