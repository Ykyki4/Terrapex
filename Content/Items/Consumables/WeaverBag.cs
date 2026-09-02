using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Accessories;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Items.Placeable;
using Terrapex.Content.Items.Weapons;

namespace Terrapex.Content.Items.Consumables
{
	/// <summary>Plan item #110. Expert bag for the Weaver of the Rift.</summary>
	public class WeaverBag : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 3;
			ItemID.Sets.BossBag[Type] = true;
		}

		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 32;
			Item.maxStack = Item.CommonMaxStack;
			Item.rare = ItemRarityID.Expert;
			Item.expert = true;
		}

		public override bool CanRightClick() => true;

		public override void ModifyItemLoot(ItemLoot itemLoot)
		{
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<AnchorLeg>()));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<WeaverHeart>(), 1, 6, 9));
			itemLoot.Add(ItemDropRule.OneFromOptions(1,
				ModContent.ItemType<Warp>(),
				ModContent.ItemType<Weft>(),
				ModContent.ItemType<LoomStaff>(),
				ModContent.ItemType<Sailcloth>(),
				ModContent.ItemType<Rend>()));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Shuttle>(), 4));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<ThreadOfFate>(), 1));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<WeaveCharm>(), 1));
		}
	}
}
