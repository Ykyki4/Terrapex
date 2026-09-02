using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Materials
{
	/// <summary>
	/// Plan item #56. Knocked off the Keeper's plates rather than dropped by the boss, so a
	/// player who ignores the shell and burns the core walks away short of steel.
	/// </summary>
	public class PlateShard : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 25;

		public override void SetDefaults()
		{
			Item.width = 20;
			Item.height = 20;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 8);
			Item.rare = ItemRarityID.Pink;
		}
	}
}
