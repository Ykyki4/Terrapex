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
	/// Plan items #46-48. The tier's universal set — no class stat on it at all, only
	/// reach and sight, so a summoner and a gunner both have a reason to wear it while
	/// they wait for the hardmode branches to split.
	/// </summary>
	[AutoloadEquip(EquipType.Head)]
	public class DustseekerHood : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 22;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Orange;
			Item.defense = 6;
		}

		public override void UpdateEquip(Player player)
		{
			player.pickSpeed -= 0.15f;
			player.GetCritChance(DamageClass.Ranged) += 6;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
			=> body.type == ModContent.ItemType<DustseekerGarb>()
			&& legs.type == ModContent.ItemType<DustseekerTreads>();

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.Terrapex.SetBonus.Dustseeker");
			player.detectCreature = true;
			player.GetModPlayer<TerrapexPlayer>().dustseekerSet = true;
			player.GetDamage(DamageClass.Generic) += 0.08f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Riftglass>(8)
				.AddIngredient<RiftDust>(20)
				.AddIngredient(ItemID.Bone, 15)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
