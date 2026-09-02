using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>The Darner set's summoner head.</summary>
	[AutoloadEquip(EquipType.Head)]
	public class DarnerCowl : ModItem
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
			player.maxMinions += 1;
			player.GetDamage(DamageClass.Summon) += 0.09f;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
			=> body.type == ModContent.ItemType<DarnerCoat>()
			&& legs.type == ModContent.ItemType<DarnerBoots>();

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.Terrapex.SetBonus.DarnerSummon");
			TerrapexPlayer mp = player.GetModPlayer<TerrapexPlayer>();
			mp.darnerSet = true;
			mp.darnerSummon = true;
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
