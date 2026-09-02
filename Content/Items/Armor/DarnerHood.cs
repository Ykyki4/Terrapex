using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>
	/// Plan item #83, now the Darner set's magic head.
	///
	/// The set was classless in its first pass, which made it the strongest armour in the mod
	/// for every build at once and gave the player nothing to choose. It is four heads on one
	/// coat now: the thread bonus is shared, and each head decides what the thread is for.
	/// </summary>
	[AutoloadEquip(EquipType.Head)]
	public class DarnerHood : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 24;
			Item.value = Item.sellPrice(gold: 6);
			Item.rare = ItemRarityID.Yellow;
			Item.defense = 12;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetDamage(DamageClass.Magic) += 0.11f;
			player.statManaMax2 += 60;
			player.manaCost -= 0.18f;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
			=> body.type == ModContent.ItemType<DarnerCoat>()
			&& legs.type == ModContent.ItemType<DarnerBoots>();

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.Terrapex.SetBonus.DarnerMagic");
			TerrapexPlayer mp = player.GetModPlayer<TerrapexPlayer>();
			mp.darnerSet = true;
			mp.darnerMagic = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<VoidCloth>(8)
				.AddIngredient(ItemID.HallowedBar, 8)
				.AddIngredient(ItemID.SoulofNight, 5)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
