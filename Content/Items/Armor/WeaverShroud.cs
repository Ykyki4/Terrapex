using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Common.Reaper;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>
	/// The Weaver set's reaper head. No crit, like the rest of the tier. Its twist: the loom takes an
	/// anchor on every hit, not every third - a reaper strikes where it chooses, so the web goes up fast.
	/// </summary>
	[AutoloadEquip(EquipType.Head)]
	public class WeaverShroud : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 24;
			Item.value = Item.sellPrice(gold: 12);
			Item.rare = ItemRarityID.Lime;
			Item.defense = 14;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetDamage<ReapingDamage>() += 0.16f;
			player.GetAttackSpeed<ReapingDamage>() += 0.10f;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
			=> body.type == ModContent.ItemType<WeaverChasuble>()
			&& legs.type == ModContent.ItemType<WeaverTreads>();

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.Terrapex.SetBonus.WeaverReaper");
			TerrapexPlayer mp = player.GetModPlayer<TerrapexPlayer>();
			mp.weaverSet = true;
			mp.weaverReaper = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<EchoAlloy>(10)
				.AddIngredient(ItemID.ChlorophyteBar, 6)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
