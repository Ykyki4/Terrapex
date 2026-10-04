using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// The reaper's first accessory, from the Ronin. It does not add damage: it moves the line
	/// where the edge begins, so more of every stroke crits. That keeps the class's one skill -
	/// distance - the thing the gear improves.
	/// </summary>
	public class Whetstone : ModItem
	{
		public const float EdgeBonus = 0.15f;

		public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs((int)(EdgeBonus * 100f));

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 18;
			Item.accessory = true;
			Item.value = Item.buyPrice(gold: 2);
			Item.rare = ItemRarityID.Blue;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
			=> player.GetModPlayer<ReaperPlayer>().edgeShare += EdgeBonus;
	}
}
