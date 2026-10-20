using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Accessories.Trees
{
	/// <summary>
	/// "Any Hermes Boots" for the Wayfarer's Boots: the four pre-hardmode run-speed boots are
	/// one rung in vanilla too, so any of them will do. The display text is vanilla's own
	/// "Any" plus the item name, so the group needs no localization key of ours.
	/// </summary>
	public class TreeRecipeGroups : ModSystem
	{
		public const string AnyHermesBoots = "Terrapex:AnyHermesBoots";

		public override void AddRecipeGroups()
		{
			RecipeGroup.RegisterGroup(AnyHermesBoots, new RecipeGroup(
				() => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.HermesBoots)}",
				ItemID.HermesBoots, ItemID.FlurryBoots, ItemID.SailfishBoots, ItemID.SandBoots));
		}
	}
}
