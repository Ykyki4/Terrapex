using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// The end of the line: the Reaper Emblem, the Reaper's Kit and the Soul Lantern in one, finished
	/// with ectoplasm after Plantera - the reaper's Destroyer Emblem.
	/// </summary>
	public class GrimInsignia : ModItem
	{
		public const int CritBonus = 8;

		public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs((int)(ReaperEmblem.DamageBonus * 100f),
			CritBonus, (int)(Whetstone.EdgeBonus * 100f), (int)(HarvestCharm.GainBonus * 100f),
			(int)(BoneSheath.SpeedBonus * 100f), (int)(SoulLantern.ReapBonus * 100f), (int)(SoulLantern.Refund * 100f));

		public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 30;
			Item.accessory = true;
			Item.value = Item.sellPrice(gold: 12);
			Item.rare = ItemRarityID.Yellow;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			player.GetDamage<ReapingDamage>() += ReaperEmblem.DamageBonus;
			player.GetModPlayer<ReaperPlayer>().edgeShare += Whetstone.EdgeBonus;
			player.GetModPlayer<ReaperPlayer>().harvestGain += HarvestCharm.GainBonus;
			player.GetAttackSpeed<ReapingDamage>() += BoneSheath.SpeedBonus;
			player.GetModPlayer<ReaperPlayer>().reapDamage += SoulLantern.ReapBonus;
			player.GetModPlayer<ReaperPlayer>().reapRefund += SoulLantern.Refund;
			player.GetCritChance<ReapingDamage>() += GrimInsignia.CritBonus;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<ReaperEmblem>()
				.AddIngredient<ReapersKit>()
				.AddIngredient<SoulLantern>()
				.AddIngredient(ItemID.Ectoplasm, 10)
				.AddTile(TileID.TinkerersWorkbench)
				.Register();
		}
	}
}
