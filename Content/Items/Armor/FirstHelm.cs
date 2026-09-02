using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>Plan item #121. The melee head: the narrowest regard in the set and the heaviest, because a swing has to be pointed at one thing anyway.</summary>
	[AutoloadEquip(EquipType.Head)]
	public class FirstHelm : ModItem
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
			player.GetDamage(DamageClass.Melee) += 0.16f;
			// armour penetration is the tier's own axis - nothing below T6 grants any, so
			// the last set is not the one below it with bigger percentages on the same lines
			player.GetArmorPenetration(DamageClass.Melee) += 12f;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
			=> body.type == ModContent.ItemType<FirstCuirass>()
			&& legs.type == ModContent.ItemType<FirstGreaves>();

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.Terrapex.SetBonus.FirstMelee");
			TerrapexPlayer mp = player.GetModPlayer<TerrapexPlayer>();
			mp.firstSet = true;
			mp.firstMelee = true;
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
