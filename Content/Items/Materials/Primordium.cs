using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Materials
{
	/// <summary>
	/// Plan item #112. Whatever the crack was made out of before it was a crack. Everything in
	/// the last tier is drawn from this, and it comes from exactly two places: the First Keeper
	/// itself, and the echoes of it that got loose.
	/// </summary>
	public class Primordium : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 25;

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 24;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Red;
		}
	}
}
