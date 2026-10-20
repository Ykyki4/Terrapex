using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Materials
{
	/// <summary>
	/// Scraped off the Barnacle Crabs on the beach. The Barnacled Bell is cast round eight of them,
	/// so the boss's summon starts with its mobs - the beach teaches the fight's theme first.
	/// </summary>
	public class BarnacleCluster : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 25;
			ItemID.Sets.SortingPriorityMaterials[Type] = 40;
		}

		public override void SetDefaults()
		{
			Item.width = 20;
			Item.height = 18;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(copper: 40);
			Item.rare = ItemRarityID.White;
			Item.material = true;
		}
	}
}
