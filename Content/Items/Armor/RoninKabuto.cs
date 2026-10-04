using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>
	/// The Ronin set: red lacquer, the reaper's first armour. T2, 20 defence like the tier's other
	/// set, sold by the Ronin after Skeletron rather than crafted - it arrives with the class's NPC.
	///
	/// The set bonus is Iaido: stand without attacking for 1.5 seconds and the next stroke counts
	/// every hit as edge. A stance and a release - the class's distance rule turned into timing.
	/// </summary>
	[AutoloadEquip(EquipType.Head)]
	public class RoninKabuto : ModItem
	{
		public const int ReapingCrit = 6;

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 22;
			Item.value = Item.buyPrice(gold: 6);
			Item.rare = ItemRarityID.Orange;
			Item.defense = 6;
		}

		public override void UpdateEquip(Player player)
			=> player.GetCritChance<ReapingDamage>() += ReapingCrit;

		public override bool IsArmorSet(Item head, Item body, Item legs)
			=> body.type == ModContent.ItemType<RoninDo>() && legs.type == ModContent.ItemType<RoninSuneate>();

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.Terrapex.SetBonus.Ronin",
				ReaperPlayer.IaidoTicks / 60f);
			player.GetModPlayer<ReaperPlayer>().roninSet = true;
		}
	}
}
