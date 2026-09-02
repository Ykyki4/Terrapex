using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>The Weaver set's ranged head. Its bonus is more chain, not more damage.</summary>
	[AutoloadEquip(EquipType.Head)]
	public class WeaverVisor : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 24;
			Item.value = Item.sellPrice(gold: 12);
			Item.rare = ItemRarityID.Lime;
			Item.defense = 14;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Ranged) += 0.14f;
			// ammo, not crit: the Darner visor already owns crit at the tier below
			player.ammoCost80 = true;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
			=> body.type == ModContent.ItemType<WeaverChasuble>()
			&& legs.type == ModContent.ItemType<WeaverTreads>();

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.Terrapex.SetBonus.WeaverRanged");
			TerrapexPlayer mp = player.GetModPlayer<TerrapexPlayer>();
			mp.weaverSet = true;
			mp.weaverRanged = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<EchoAlloy>(10)
				.AddIngredient(ItemID.ChlorophyteBar, 6)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
