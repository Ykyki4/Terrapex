using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>The Riftsteel set's summoner head.</summary>
	[AutoloadEquip(EquipType.Head)]
	public class RiftsteelCrown : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 24;
			Item.value = Item.sellPrice(gold: 4);
			Item.rare = ItemRarityID.Pink;
			Item.defense = 9;
		}

		public override void UpdateEquip(Player player)
		{
			player.maxMinions += 1;
			player.GetDamage(DamageClass.Summon) += 0.10f;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
			=> body.type == ModContent.ItemType<RiftsteelBreastplate>()
			&& legs.type == ModContent.ItemType<RiftsteelGreaves>();

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.Terrapex.SetBonus.Riftsteel");
			player.GetModPlayer<TerrapexPlayer>().GrantGuardPlates(RiftsteelHelm.SetPlates);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<RiftsteelBar>(10)
				.AddIngredient<RiftEssence>(4)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}
}
