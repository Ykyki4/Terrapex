using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>The Riftsteel set's magic head. Four heads share one body, so choosing a class costs a
	/// helmet rather than a whole second grind.</summary>
	[AutoloadEquip(EquipType.Head)]
	public class RiftsteelHood : ModItem
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
			player.GetDamage(DamageClass.Magic) += 0.09f;
			player.statManaMax2 += 40;
			player.manaCost -= 0.08f;
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
