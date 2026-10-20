using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Armor.Reaper
{
	/// <summary>
	/// The Tidewrack set: a drowned sailor's sailcloth, from the Old Shellback's barnacles at an
	/// anvil. 11 defence, between the Scarecrow (7) and the Ronin (20). See <c>CLASS_REAPER.md</c>.
	///
	/// The bonus is the tide. Every strike that lands in the edge band of the reach banks a swell -
	/// up to <see cref="MaxSwells"/>, drawn as beads of water circling the waist - and the next Reap
	/// breaks them: a wave runs along the floor each way from the reaper's feet, exactly to the rim
	/// of the reach circle, <see cref="SwellShare"/> of the weapon's damage per swell. It is the
	/// Shellback's landing wave, handed over, and it changes what a Reap is for: the meter is spent
	/// on the spin as always, and the edge strikes made on the way to it decide how big the tide is.
	/// </summary>
	[AutoloadEquip(EquipType.Head)]
	public class TidewrackHood : ModItem
	{
		public const int ReapingCrit = 5;
		public const float HarvestBonus = 0.10f;

		public const int MaxSwells = 4;
		public const float SwellShare = 0.4f;
		/// <summary>Ticks the swells hold after the last edge strike before they ebb, one a second.</summary>
		public const int SwellHold = 600;
		public const float WaveKnockback = 7f;

		public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ReapingCrit, (int)(HarvestBonus * 100f));

		public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 30;
			Item.value = Item.sellPrice(silver: 45);
			Item.rare = ItemRarityID.Blue;
			Item.defense = 3;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetCritChance<ReapingDamage>() += ReapingCrit;
			player.GetModPlayer<ReaperPlayer>().harvestGain += HarvestBonus;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
			=> body.type == ModContent.ItemType<TidewrackCoat>() && legs.type == ModContent.ItemType<TidewrackBoots>();

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.Terrapex.SetBonus.TidewrackSet", MaxSwells, (int)(SwellShare * 100f));
			player.GetModPlayer<BiomeSetPlayer>().tidewrackSet = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<BarnacleCluster>(), 6)
				.AddIngredient(ItemID.Seashell, 2)
				.AddRecipeGroup("IronBar", 4)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
