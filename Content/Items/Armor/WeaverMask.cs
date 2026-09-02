using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>Plan item #108. Vanity.</summary>
	[AutoloadEquip(EquipType.Head)]
	public class WeaverMask : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
			ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false;
		}

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 24;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Lime;
			Item.vanity = true;
		}
	}
}
