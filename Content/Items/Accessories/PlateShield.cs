using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// Plan item #25. One slab kept off the Eye's shell, still doing the job it did
	/// there: it eats a hit, then has to grow back.
	/// </summary>
	public class PlateShield : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 26;
			Item.value = Item.sellPrice(gold: 1, silver: 20);
			Item.rare = ItemRarityID.Green;
			Item.accessory = true;
			Item.defense = 2;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			player.GetModPlayer<TerrapexPlayer>().plateShield = true;
		}
	}
}
