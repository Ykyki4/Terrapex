using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Accessories.Reaper
{
	/// <summary>
	/// The Scarab Sultan's reaper accessory: the scarab that pushes the sun up out of the earth.
	/// Every Reap is followed by a sunrise - for <see cref="SunriseTicks"/> the reach is
	/// <see cref="RangeBonus"/> wider and the blade's edge <see cref="EdgeBonus"/> longer. The
	/// reach circle on screen grows with it, and a ring of gold runs out to the new rim when it
	/// starts, so the window is drawn rather than remembered. The Reap stops being the end of a
	/// cycle and becomes the start of one.
	/// </summary>
	public class ScarabAmulet : ModItem
	{
		public const int SunriseTicks = 240;
		public const float RangeBonus = 0.25f;
		public const float EdgeBonus = 0.10f;

		public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SunriseTicks / 60,
			(int)(RangeBonus * 100f), (int)(EdgeBonus * 100f));

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 28;
			Item.accessory = true;
			Item.value = Item.sellPrice(gold: 1, silver: 50);
			Item.rare = ItemRarityID.Green;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
			=> player.GetModPlayer<BiomeRelicPlayer>().scarab = true;
	}
}
