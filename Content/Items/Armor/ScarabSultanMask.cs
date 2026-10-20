using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>The Scarab Sultan's crowned head, as vanity. Drawn 2x2, like the boss.</summary>
	[AutoloadEquip(EquipType.Head)]
	public class ScarabSultanMask : ModItem
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
