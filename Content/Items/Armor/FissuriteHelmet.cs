using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Armor
{
	[AutoloadEquip(EquipType.Head)]
	public class FissuriteHelmet : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 20;
			Item.value = Item.sellPrice(silver: 30);
			Item.rare = ItemRarityID.Blue;
			Item.defense = 3;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetCritChance(DamageClass.Melee) += 4;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
			=> body.type == ModContent.ItemType<FissuriteBreastplate>()
			&& legs.type == ModContent.ItemType<FissuriteGreaves>();

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.Terrapex.SetBonus.Fissurite",
				TerrapexPlayer.HitsPerCrack, (int)(Content.Buffs.Cracked.DamageBonus * 100f));
			player.GetModPlayer<TerrapexPlayer>().fissuriteSet = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<FissuriteBar>(), 12)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
