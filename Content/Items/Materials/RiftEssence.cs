using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Materials
{
	/// <summary>Plan item #58. What is left of the Keeper once the shell is gone.</summary>
	public class RiftEssence : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 25;

		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 22;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 20);
			Item.rare = ItemRarityID.Pink;
		}
	}
}
