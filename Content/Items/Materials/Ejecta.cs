using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Materials
{
	/// <summary>
	/// What the Breach throws up: rock that was on the other side of the crack until the event
	/// opened it. The event's currency rather than its drop — see <c>EVENT_BREACH.md</c>.
	///
	/// Paid out by mouths and by the finale, and spent on the Rift Altar. Making the reward a
	/// currency instead of a weapon table is what gives a player a reason to run the Breach a
	/// second time: they choose what it was for.
	/// </summary>
	public class Ejecta : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 25;

		public override void SetDefaults()
		{
			Item.width = 20;
			Item.height = 20;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 10);
			Item.rare = ItemRarityID.Pink;
		}
	}
}
