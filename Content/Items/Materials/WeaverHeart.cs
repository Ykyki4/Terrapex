using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Materials
{
	/// <summary>Plan item #94. What was doing the weaving.</summary>
	public class WeaverHeart : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 3;

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 26;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(gold: 3);
			Item.rare = ItemRarityID.Lime;
		}
	}
}
