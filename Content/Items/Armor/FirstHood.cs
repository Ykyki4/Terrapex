using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>Plan item #123. The magic head reads a disc at the cursor instead of a cone out of the player - the one head that can regard something it is not facing, paid for with reach.</summary>
	[AutoloadEquip(EquipType.Head)]
	public class FirstHood : ModItem
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
			player.GetDamage(DamageClass.Magic) += 0.17f;
			player.statManaMax2 += 120;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
			=> body.type == ModContent.ItemType<FirstCuirass>()
			&& legs.type == ModContent.ItemType<FirstGreaves>();

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.Terrapex.SetBonus.FirstMagic");
			TerrapexPlayer mp = player.GetModPlayer<TerrapexPlayer>();
			mp.firstSet = true;
			mp.firstMagic = true;
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
