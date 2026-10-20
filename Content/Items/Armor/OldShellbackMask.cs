using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>The Old Shellback's face - shell brow, eye stalks and mandibles - as vanity. Drawn 2x2.</summary>
	[AutoloadEquip(EquipType.Head)]
	public class OldShellbackMask : ModItem
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
