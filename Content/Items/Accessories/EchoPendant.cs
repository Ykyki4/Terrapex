using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// Plan item #44. The Hollow Echo's own trick, worn: everything alive shows through
	/// the stone. In the dungeon that is worth more than defence.
	/// </summary>
	public class EchoPendant : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 26;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Orange;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			player.detectCreature = true;
			player.GetCritChance(DamageClass.Generic) += 3;
		}
	}
}
