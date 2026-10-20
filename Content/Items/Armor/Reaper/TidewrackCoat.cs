using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Armor.Reaper
{
	[AutoloadEquip(EquipType.Body)]
	public class TidewrackCoat : ModItem
	{
		public const float ReapingDamageBonus = 0.06f;
		public const float EdgeBonus = 0.05f;

		public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs((int)(ReapingDamageBonus * 100f), (int)(EdgeBonus * 100f));

		public override void SetDefaults()
		{
			Item.width = 36;
			Item.height = 20;
			Item.value = Item.sellPrice(silver: 60);
			Item.rare = ItemRarityID.Blue;
			Item.defense = 5;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetDamage<ReapingDamage>() += ReapingDamageBonus;
			player.GetModPlayer<ReaperPlayer>().edgeShare += EdgeBonus;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<BarnacleCluster>(), 10)
				.AddRecipeGroup("IronBar", 8)
				.AddIngredient(ItemID.Rope, 10)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
