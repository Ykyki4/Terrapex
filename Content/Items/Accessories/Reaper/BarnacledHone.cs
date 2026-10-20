using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Accessories.Reaper
{
	/// <summary>
	/// The Old Shellback's reaper accessory: a whetstone gone green in the sea. It ties the class's
	/// two layers together - critical blade hits crust a barnacle on the target (up to
	/// <see cref="MaxBarnacles"/>, drawn on it), and the next strike at range knocks every one of
	/// them off for <see cref="PerBarnacle"/> more damage each and <see cref="HarvestPerBarnacle"/>
	/// Harvest each. Seed with the blade, cash in with the strike.
	/// </summary>
	public class BarnacledHone : ModItem
	{
		public const int MaxBarnacles = 5;
		public const float PerBarnacle = 0.12f;
		public const float HarvestPerBarnacle = 3f;
		/// <summary>Ticks a crust stays on after the last barnacle was added.</summary>
		public const int CrustTicks = 480;

		public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxBarnacles, (int)(PerBarnacle * 100f));

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 22;
			Item.accessory = true;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Blue;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
			=> player.GetModPlayer<BiomeRelicPlayer>().hone = true;
	}
}
