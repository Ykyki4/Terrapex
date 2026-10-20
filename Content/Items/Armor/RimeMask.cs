using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>The Rime's horned face and icicle beard, as vanity. Drawn 2x2.</summary>
	[AutoloadEquip(EquipType.Head)]
	public class RimeMask : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 24;
			Item.rare = ItemRarityID.Blue;
			Item.vanity = true;
			Item.value = Item.sellPrice(silver: 75);
		}
	}
}
