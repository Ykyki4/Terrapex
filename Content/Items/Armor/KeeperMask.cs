using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>Plan item #71. Vanity: the Keeper's shell, worn open.</summary>
	[AutoloadEquip(EquipType.Head)]
	public class KeeperMask : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
			ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.None;
		}

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 24;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Pink;
			Item.vanity = true;
		}
	}
}
