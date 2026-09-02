using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>Plan items #35-37: the tier's magic set, ground out of riftglass.</summary>
	[AutoloadEquip(EquipType.Head)]
	public class GlassblowerHood : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 20;
			Item.value = Item.sellPrice(silver: 70);
			Item.rare = ItemRarityID.Green;
			Item.defense = 5;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetCritChance(DamageClass.Magic) += 6;
			player.statManaMax2 += 20;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
			=> body.type == ModContent.ItemType<GlassblowerRobe>()
			&& legs.type == ModContent.ItemType<GlassblowerBoots>();

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.Terrapex.SetBonus.Glassblower");
			player.GetModPlayer<TerrapexPlayer>().glassblowerSet = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Riftglass>(10)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
