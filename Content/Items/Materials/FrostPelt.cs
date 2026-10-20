using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Materials
{
	/// <summary>A Drift Hare's winter coat, white and thick. Goes into the Frozen Horn.</summary>
	public class FrostPelt : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 25;

		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 20;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 3);
			Item.rare = ItemRarityID.Blue;
		}
	}
}
