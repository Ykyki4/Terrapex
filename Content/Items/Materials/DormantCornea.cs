using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Materials
{
	/// <summary>Plan item #29. The Dormant Eye's guaranteed drop and the root of all of T1.</summary>
	public class DormantCornea : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 15;
			ItemID.Sets.SortingPriorityMaterials[Type] = 61;
		}

		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 20;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 8);
			Item.rare = ItemRarityID.Blue;
			Item.material = true;
		}
	}
}
