using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>Plan item #124. The summoner head points the whole bench at whatever the eye is on, and pays them again for hitting it.</summary>
	[AutoloadEquip(EquipType.Head)]
	public class FirstCrown : ModItem
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
			player.maxMinions += 2;
			player.GetDamage(DamageClass.Summon) += 0.14f;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
			=> body.type == ModContent.ItemType<FirstCuirass>()
			&& legs.type == ModContent.ItemType<FirstGreaves>();

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.Terrapex.SetBonus.FirstSummon");
			TerrapexPlayer mp = player.GetModPlayer<TerrapexPlayer>();
			mp.firstSet = true;
			mp.firstSummon = true;
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
