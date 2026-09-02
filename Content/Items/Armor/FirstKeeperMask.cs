using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>Plan item #129. Vanity.</summary>
	[AutoloadEquip(EquipType.Head)]
	public class FirstKeeperMask : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
			ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false;
		}

		public override void SetDefaults()
		{
			Item.width = 28;
			Item.height = 26;
			Item.value = Item.sellPrice(gold: 3);
			Item.rare = ItemRarityID.Red;
			Item.vanity = true;
		}
	}
}
