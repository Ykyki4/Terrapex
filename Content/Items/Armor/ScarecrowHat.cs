using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>
	/// The Scarecrow set: the reaper's first armour, from hay, silk and wood at the loom before any
	/// boss - the class's answer to the wizard hat and the fossil set. 7 defence. Its bonus is
	/// about the meter: Harvest fills a quarter faster and holds twice as long before it drains.
	/// </summary>
	[AutoloadEquip(EquipType.Head)]
	public class ScarecrowHat : ModItem
	{
		public const float SetGain = 0.25f;
		public const float SetHold = 2f;

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 22;
			Item.value = Item.sellPrice(silver: 20);
			Item.rare = ItemRarityID.White;
			Item.defense = 2;
		}

		public override void UpdateEquip(Player player) => player.GetCritChance<ReapingDamage>() += 4;

		public override bool IsArmorSet(Item head, Item body, Item legs)
			=> body.type == ModContent.ItemType<ScarecrowShirt>() && legs.type == ModContent.ItemType<ScarecrowTrousers>();

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.Terrapex.SetBonus.Scarecrow", (int)(SetGain * 100f));
			ReaperPlayer reaper = player.GetModPlayer<ReaperPlayer>();
			reaper.scarecrowSet = true;
			reaper.harvestGain += SetGain;
			reaper.holdMult *= SetHold;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.Hay, 15)
				.AddIngredient(ItemID.Silk, 2)
				.AddTile(TileID.Loom)
				.Register();
		}
	}
}
