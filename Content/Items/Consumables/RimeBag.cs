using Terraria;
using Terrapex.Content.Items.Accessories.Reaper;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Accessories;
using Terrapex.Content.Items.Armor;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Items.Weapons.Rime;
using Terrapex.Content.NPCs.Bosses;

namespace Terrapex.Content.Items.Consumables
{
	/// <summary>The Rime's treasure bag: one weapon of five, the Permafrost Heart, pelts and the mask's chance.</summary>
	public class RimeBag : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 3;
			ItemID.Sets.BossBag[Type] = true;
			ItemID.Sets.PreHardmodeLikeBossBag[Type] = true;
		}

		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 32;
			Item.maxStack = Item.CommonMaxStack;
			Item.rare = ItemRarityID.Expert;
			Item.expert = true;
			Item.consumable = true;
			Item.value = 0;
		}

		public override bool CanRightClick() => true;

		public override void ModifyItemLoot(ItemLoot itemLoot)
		{
			itemLoot.Add(ItemDropRule.OneFromOptions(1,
				ModContent.ItemType<Glacierbreaker>(),
				ModContent.ItemType<HoarfrostBow>(),
				ModContent.ItemType<SnowcapStaff>(),
				ModContent.ItemType<GlacierBell>(),
				ModContent.ItemType<FrostfellScythe>()));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<RimeboundWrap>(), 3));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<PermafrostHeart>()));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<FrostPelt>(), 1, 6, 10));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<RimeMask>(), 7));
			itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<RimeColossus>()));
		}
	}
}
