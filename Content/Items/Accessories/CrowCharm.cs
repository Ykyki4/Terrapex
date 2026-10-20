using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// The Hollow Harvester's charm: every Reap lets a flock of six straw crows out in a ring. A
	/// Reap is a crowd move, and the crows carry it to the edges of the crowd.
	/// </summary>
	public class CrowCharm : ModItem
	{
		public const int Crows = 6;

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 24;
			Item.accessory = true;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Orange;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
			=> player.GetModPlayer<ReaperPlayer>().crowCharm = true;
	}
}
