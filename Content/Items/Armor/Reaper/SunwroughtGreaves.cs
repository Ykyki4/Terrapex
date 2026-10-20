using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Armor.Reaper
{
	[AutoloadEquip(EquipType.Legs)]
	public class SunwroughtGreaves : ModItem
	{
		public const float ReapingSpeed = 0.08f;
		/// <summary>The meter holds half again as long before it drains - the set lives on a full one.</summary>
		public const float HoldBonus = 1.5f;

		public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs((int)(ReapingSpeed * 100f), (int)((HoldBonus - 1f) * 100f));

		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 16;
			Item.value = Item.sellPrice(silver: 80);
			Item.rare = ItemRarityID.Green;
			Item.defense = 5;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetAttackSpeed<ReapingDamage>() += ReapingSpeed;
			player.GetModPlayer<ReaperPlayer>().holdMult *= HoldBonus;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<GildedChitin>(), 8)
				.AddIngredient(ModContent.ItemType<Sunsand>(), 5)
				.AddIngredient(ItemID.AntlionMandible, 2)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
