using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Accessories.Reaper
{
	/// <summary>
	/// The Rime's reaper accessory: a length of haft bound in the mountain's red cloth, frost grown
	/// over the wraps. It gives the haft - the inner, weak part of every stroke - a job. A haft hit
	/// throws the target back <see cref="KnockbackMult"/>x as hard (out toward the edge, where the
	/// reaper wants it; bosses are not thrown) and frostbites it for <see cref="FrostTicks"/> ticks.
	/// The next edge or strike hit on a frostbitten target shatters the frost for
	/// <see cref="ShatterBonus"/> more damage. Close in, push out, cut at the rim.
	/// </summary>
	public class RimeboundWrap : ModItem
	{
		public const float KnockbackMult = 2.5f;
		public const int FrostTicks = 180;
		public const float ShatterBonus = 0.35f;

		public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs((int)(ShatterBonus * 100f));

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 16;
			Item.height = 32;
			Item.accessory = true;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Orange;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
			=> player.GetModPlayer<BiomeRelicPlayer>().wrap = true;
	}
}
