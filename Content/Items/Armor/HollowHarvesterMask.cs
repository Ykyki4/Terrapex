using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>The Hollow Harvester's sack head, as vanity. Drawn 2x2, like the class's other armour.</summary>
	[AutoloadEquip(EquipType.Head)]
	public class HollowHarvesterMask : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 22;
			Item.rare = ItemRarityID.Blue;
			Item.vanity = true;
			Item.value = Item.sellPrice(silver: 75);
		}
	}
}
