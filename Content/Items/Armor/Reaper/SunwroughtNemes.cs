using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles.Reaper;

namespace Terrapex.Content.Items.Armor.Reaper
{
	/// <summary>
	/// The Sunwrought set: gilded chitin and lapis, from the Scarab Sultan's materials at an anvil.
	/// 15 defence. See <c>CLASS_REAPER.md</c>.
	///
	/// The bonus turns the full meter from a button into a stance. While the Harvest is full, every
	/// strike rolls a sun out of the point it hit, away from the reaper, by the Sultan's own ball
	/// physics - and the reticle carries the sun's whole dotted path while it is, the same promise
	/// the Sultan makes before every kick. The sun gathers as it rolls: each enemy it passes through
	/// adds <see cref="SunwroughtSun.GrowPerHit"/>. A Sunwrought reaper therefore has a real choice
	/// the moment the bar flashes: spend it on the Reap, or keep cutting and keep it full.
	/// </summary>
	[AutoloadEquip(EquipType.Head)]
	public class SunwroughtNemes : ModItem
	{
		public const int ReapingCrit = 6;
		public const float RangeBonus = 0.10f;

		public const float SunShare = 0.6f;
		/// <summary>Ticks between suns, so a fast scythe does not carpet the floor.</summary>
		public const int SunCooldown = 30;

		public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ReapingCrit, (int)(RangeBonus * 100f));

		public override void SetDefaults()
		{
			Item.width = 28;
			Item.height = 30;
			Item.value = Item.sellPrice(silver: 80);
			Item.rare = ItemRarityID.Green;
			Item.defense = 4;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetCritChance<ReapingDamage>() += ReapingCrit;
			player.GetModPlayer<ReaperPlayer>().rangeMult += RangeBonus;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs)
			=> body.type == ModContent.ItemType<SunwroughtCarapace>() && legs.type == ModContent.ItemType<SunwroughtGreaves>();

		public override void UpdateArmorSet(Player player)
		{
			player.setBonus = Language.GetTextValue("Mods.Terrapex.SetBonus.SunwroughtSet",
				(int)(SunShare * 100f), (int)(SunwroughtSun.GrowPerHit * 100f));
			player.GetModPlayer<BiomeSetPlayer>().sunwroughtSet = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<GildedChitin>(), 6)
				.AddIngredient(ModContent.ItemType<Sunsand>(), 4)
				.AddIngredient(ItemID.Sapphire, 2)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
