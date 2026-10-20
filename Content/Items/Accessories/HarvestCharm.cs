using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// A sheaf of straw tied with a red thread, sold by the Ronin once the Eye of Cthulhu is dead. The
	/// meter fills faster, so the Reap comes round more often.
	/// </summary>
	public class HarvestCharm : ModItem
	{
		public const float GainBonus = 0.25f;

		public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs((int)(GainBonus * 100f));

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 26;
			Item.accessory = true;
			Item.value = Item.buyPrice(gold: 3);
			Item.rare = ItemRarityID.Green;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			player.GetModPlayer<ReaperPlayer>().harvestGain += HarvestCharm.GainBonus;
		}
	}
}
