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
	/// The Darner set's melee head.
	///
	/// One coat, one pair of boots, four heads. The set bonus is the same thread in every
	/// case — stitched enemies take 20% more from you — but each head then rewrites what the
	/// thread is *for*, so picking a class changes how the tier is played rather than only
	/// which damage number goes up.
	/// </summary>
	[AutoloadEquip(EquipType.Head)]
	public class DarnerMask : ModItem
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
			player.GetDamage(DamageClass.Melee) += 0.10f;
			player.GetCritChance(DamageClass.Melee) += 6;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
			=> body.type == ModContent.ItemType<DarnerCoat>()
			&& legs.type == ModContent.ItemType<DarnerBoots>();

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.Terrapex.SetBonus.DarnerMelee");
			TerrapexPlayer mp = player.GetModPlayer<TerrapexPlayer>();
			mp.darnerSet = true;
			mp.darnerMelee = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<VoidCloth>(8)
				.AddIngredient(ItemID.HallowedBar, 8)
				.AddIngredient(ItemID.SoulofMight, 5)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
