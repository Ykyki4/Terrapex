using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// A lantern of souls of light and night on a hallowed frame. A Reap burns brighter and leaves a
	/// quarter of the meter standing, so the next one is closer.
	/// </summary>
	public class SoulLantern : ModItem
	{
		public const float ReapBonus = 0.25f;
		public const float Refund = 0.25f;

		public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs((int)(ReapBonus * 100f), (int)(Refund * 100f));

		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 30;
			Item.accessory = true;
			Item.value = Item.sellPrice(gold: 5);
			Item.rare = ItemRarityID.Pink;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			player.GetModPlayer<ReaperPlayer>().reapDamage += SoulLantern.ReapBonus;
			player.GetModPlayer<ReaperPlayer>().reapRefund += SoulLantern.Refund;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.SoulofLight, 10)
				.AddIngredient(ItemID.SoulofNight, 10)
				.AddIngredient(ItemID.HallowedBar, 5)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}
}
