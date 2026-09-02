using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>Plan item #122. The ranged head: the regard reaches twice as far, which is the only version of it that matches how far a gun already shoots.</summary>
	[AutoloadEquip(EquipType.Head)]
	public class FirstVisor : ModItem
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
			player.GetDamage(DamageClass.Ranged) += 0.16f;
			player.GetArmorPenetration(DamageClass.Ranged) += 12f;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
			=> body.type == ModContent.ItemType<FirstCuirass>()
			&& legs.type == ModContent.ItemType<FirstGreaves>();

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.Terrapex.SetBonus.FirstRanged");
			TerrapexPlayer mp = player.GetModPlayer<TerrapexPlayer>();
			mp.firstSet = true;
			mp.firstRanged = true;
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
