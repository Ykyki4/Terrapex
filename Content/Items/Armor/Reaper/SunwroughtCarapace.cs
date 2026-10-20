using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Armor.Reaper
{
	[AutoloadEquip(EquipType.Body)]
	public class SunwroughtCarapace : ModItem
	{
		public const float ReapingDamageBonus = 0.08f;
		public const float ReapBonus = 0.15f;

		public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs((int)(ReapingDamageBonus * 100f), (int)(ReapBonus * 100f));

		public override void SetDefaults()
		{
			Item.width = 42;
			Item.height = 20;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Green;
			Item.defense = 6;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetDamage<ReapingDamage>() += ReapingDamageBonus;
			player.GetModPlayer<ReaperPlayer>().reapDamage += ReapBonus;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<GildedChitin>(), 10)
				.AddIngredient(ModContent.ItemType<Sunsand>(), 6)
				.AddIngredient(ItemID.AntlionMandible, 3)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
