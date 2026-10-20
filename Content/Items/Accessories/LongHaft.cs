using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// A longer handle for whatever scythe you fit it to. It does not change the blade - it changes how
	/// far the reach goes, which is the thing the class's damage grows with.
	/// </summary>
	public class LongHaft : ModItem
	{
		public const float RangeBonus = 0.20f;

		public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs((int)(RangeBonus * 100f));

		public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 30;
			Item.accessory = true;
			Item.value = Item.sellPrice(silver: 50);
			Item.rare = ItemRarityID.Blue;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			player.GetModPlayer<ReaperPlayer>().rangeMult += LongHaft.RangeBonus;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.Wood, 25)
				.AddRecipeGroup("IronBar", 6)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
