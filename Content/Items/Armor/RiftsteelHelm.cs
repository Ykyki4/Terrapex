using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>
	/// Plan item #64. The melee head of the Riftsteel set. Two helmets share one body and one
	/// pair of greaves, so a player picks a class without re-grinding the whole set.
	/// </summary>
	[AutoloadEquip(EquipType.Head)]
	public class RiftsteelHelm : ModItem
	{
		/// <summary>The set bonus, straight from the plan: three plates, eight seconds.</summary>
		public const int SetPlates = 3;

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
			player.GetDamage(DamageClass.Melee) += 0.08f;
			player.GetCritChance(DamageClass.Melee) += 6;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
			=> body.type == ModContent.ItemType<RiftsteelBreastplate>()
			&& legs.type == ModContent.ItemType<RiftsteelGreaves>();

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.Terrapex.SetBonus.Riftsteel");
			player.GetModPlayer<TerrapexPlayer>().GrantGuardPlates(SetPlates);
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
