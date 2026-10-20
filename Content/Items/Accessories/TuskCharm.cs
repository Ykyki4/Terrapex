using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>The Thornback Boar's tusk on a cord: a little armour penetration, for anyone.</summary>
	public class TuskCharm : ModItem
	{
		public const int Penetration = 5;

		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 22;
			Item.accessory = true;
			Item.value = Item.sellPrice(silver: 60);
			Item.rare = ItemRarityID.Blue;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
			=> player.GetArmorPenetration(DamageClass.Generic) += Penetration;
	}
}
