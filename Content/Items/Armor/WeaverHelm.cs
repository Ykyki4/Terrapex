using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>The Weaver set's melee head. All four share the loom bonus and then decide what
	/// the thread is for.</summary>
	[AutoloadEquip(EquipType.Head)]
	public class WeaverHelm : ModItem
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
			player.GetDamage(DamageClass.Melee) += 0.14f;
			player.GetAttackSpeed(DamageClass.Melee) += 0.08f;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
			=> body.type == ModContent.ItemType<WeaverChasuble>()
			&& legs.type == ModContent.ItemType<WeaverTreads>();

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.Terrapex.SetBonus.WeaverMelee");
			TerrapexPlayer mp = player.GetModPlayer<TerrapexPlayer>();
			mp.weaverSet = true;
			mp.weaverMelee = true;
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
