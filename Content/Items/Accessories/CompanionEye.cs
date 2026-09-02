using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// Plan item #69. It keeps the nearest enemy marked and tells your minions where to look,
	/// which is the same job the Rib does by hand.
	/// </summary>
	public class CompanionEye : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 24;
			Item.value = Item.sellPrice(gold: 4);
			Item.rare = ItemRarityID.Pink;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			player.GetModPlayer<TerrapexPlayer>().companionEye = true;
			player.GetCritChance(DamageClass.Generic) += 4;
		}
	}
}
