using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Materials
{
	/// <summary>Plan item #76. Pulled off a Weaverling, still trying to reach something.</summary>
	public class VoidThread : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 25;

		public override void SetDefaults()
		{
			Item.width = 20;
			Item.height = 20;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 30);
			Item.rare = ItemRarityID.LightPurple;
		}
	}
}
