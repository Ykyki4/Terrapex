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
	/// The First Keeper set's reaper head. Its twist: the regard is not a cone but the reach - everything
	/// inside the circle a scythe draws is being looked at.
	/// </summary>
	[AutoloadEquip(EquipType.Head)]
	public class FirstVeil : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 28;
			Item.height = 26;
			Item.value = Item.sellPrice(gold: 22);
			Item.rare = ItemRarityID.Red;
			Item.defense = 18;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetDamage<ReapingDamage>() += 0.17f;
			player.GetCritChance<ReapingDamage>() += 10;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
			=> body.type == ModContent.ItemType<FirstCuirass>()
			&& legs.type == ModContent.ItemType<FirstGreaves>();

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.Terrapex.SetBonus.FirstReaper");
			TerrapexPlayer mp = player.GetModPlayer<TerrapexPlayer>();
			mp.firstSet = true;
			mp.firstReaper = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<PrimordiumBar>(14)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
