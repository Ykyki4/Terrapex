using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Materials
{
	// The T0 catch-all material. Drops from the surface motes and the cave slimes,
	// so it is the thing the player has too much of before they have any ore.
	public class RiftDust : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 25;
			ItemID.Sets.SortingPriorityMaterials[Type] = 56;
		}

		public override void SetDefaults()
		{
			Item.width = 18;
			Item.height = 18;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(copper: 30);
			Item.rare = ItemRarityID.White;
			Item.material = true;
		}
	}
}
