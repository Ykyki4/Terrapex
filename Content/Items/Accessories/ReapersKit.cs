using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// Whetstone, Harvest Charm and Bone Sheath tied into one roll at the Tinkerer's Workshop - the
	/// pre-hardmode end of the class's accessory line, the way the Warrior Emblem's line ends in one
	/// slot.
	/// </summary>
	public class ReapersKit : ModItem
	{
		// the parts' own numbers, so the tooltip lists what the kit gives and never drifts from them
		public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs((int)(Whetstone.EdgeBonus * 100f),
			(int)(HarvestCharm.GainBonus * 100f), (int)(BoneSheath.SpeedBonus * 100f));

		public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 30;
			Item.accessory = true;
			Item.value = Item.sellPrice(gold: 4);
			Item.rare = ItemRarityID.LightRed;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			player.GetModPlayer<ReaperPlayer>().edgeShare += Whetstone.EdgeBonus;
			player.GetModPlayer<ReaperPlayer>().harvestGain += HarvestCharm.GainBonus;
			player.GetAttackSpeed<ReapingDamage>() += BoneSheath.SpeedBonus;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Whetstone>()
				.AddIngredient<HarvestCharm>()
				.AddIngredient<BoneSheath>()
				.AddTile(TileID.TinkerersWorkbench)
				.Register();
		}
	}
}
