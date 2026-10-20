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
	/// The Darner set's reaper head. Its twist: a strike at range on a stitched enemy lands as if on the edge -
	/// a crit, and double Harvest - so the thread tells the reaper where to aim.
	/// </summary>
	[AutoloadEquip(EquipType.Head)]
	public class DarnerVeil : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 24;
			Item.value = Item.sellPrice(gold: 6);
			Item.rare = ItemRarityID.Yellow;
			Item.defense = 12;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetDamage<ReapingDamage>() += 0.11f;
			player.GetCritChance<ReapingDamage>() += 8;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
			=> body.type == ModContent.ItemType<DarnerCoat>()
			&& legs.type == ModContent.ItemType<DarnerBoots>();

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.Terrapex.SetBonus.DarnerReaper");
			TerrapexPlayer mp = player.GetModPlayer<TerrapexPlayer>();
			mp.darnerSet = true;
			mp.darnerReaper = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<VoidCloth>(8)
				.AddIngredient(ItemID.HallowedBar, 8)
				.AddIngredient(ItemID.SoulofFright, 5)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
