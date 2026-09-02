using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>Plan item #26. Vanity: the lid, worn.</summary>
	[AutoloadEquip(EquipType.Head)]
	public class DormantEyeMask : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
			ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.None;
		}

		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 20;
			Item.value = Item.sellPrice(silver: 40);
			Item.rare = ItemRarityID.Blue;
			Item.vanity = true;
		}
	}
}
