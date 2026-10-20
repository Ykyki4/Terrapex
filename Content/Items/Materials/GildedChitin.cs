using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Materials
{
	/// <summary>A gold-lacquered shell plate, off the Sand Roller and the Scarab Sultan. Goes into the Sun Idol.</summary>
	public class GildedChitin : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 25;

		public override void SetDefaults()
		{
			Item.width = 20;
			Item.height = 18;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 4);
			Item.rare = ItemRarityID.Blue;
		}
	}
}
