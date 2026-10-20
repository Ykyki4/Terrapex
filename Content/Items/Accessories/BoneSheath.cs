using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// A sheath of dungeon bone, off the Angry Bones. The blade comes out of it quicker: faster strokes,
	/// faster strikes, and more of both before the Harvest drains.
	/// </summary>
	public class BoneSheath : ModItem
	{
		public const float SpeedBonus = 0.12f;

		public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs((int)(SpeedBonus * 100f));

		public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 30;
			Item.accessory = true;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Green;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			player.GetAttackSpeed<ReapingDamage>() += BoneSheath.SpeedBonus;
		}
	}
}
