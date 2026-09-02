using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// The Broodmother's shell. A T1 accessory that pays for digging rather than for fighting,
	/// which is what the tier is actually spent doing.
	/// </summary>
	public class FacetCarapace : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 24;
			Item.value = Item.sellPrice(silver: 60);
			Item.rare = ItemRarityID.Blue;
			Item.accessory = true;
			Item.defense = 4;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
			=> player.pickSpeed -= 0.15f;
	}
}
