using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// The class's emblem. The Wall of Flesh's table is vanilla's to keep, so instead any of the four
	/// vanilla emblems is re-forged into this one at the Tinkerer's Workshop - the reaper pays the
	/// same boss for it and simply chooses differently.
	/// </summary>
	public class ReaperEmblem : ModItem
	{
		public const float DamageBonus = 0.15f;
		public const string AnyEmblem = "Terrapex:AnyClassEmblem";

		public override void SetDefaults()
		{
			Item.width = 28;
			Item.height = 28;
			Item.accessory = true;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.LightRed;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
			=> player.GetDamage<ReapingDamage>() += DamageBonus;

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddRecipeGroup("Terrapex:AnyClassEmblem")
				.AddTile(TileID.TinkerersWorkbench)
				.Register();
		}
	}

	public class ReaperRecipeGroups : ModSystem
	{
		public override void AddRecipeGroups()
		{
			RecipeGroup.RegisterGroup(ReaperEmblem.AnyEmblem, new RecipeGroup(
				() => Language.GetTextValue("Mods.Terrapex.Common.AnyClassEmblem"),
				ItemID.WarriorEmblem, ItemID.RangerEmblem, ItemID.SorcererEmblem, ItemID.SummonerEmblem));
		}
	}
}
