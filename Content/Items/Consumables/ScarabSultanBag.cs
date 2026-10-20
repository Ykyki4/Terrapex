using Terraria;
using Terrapex.Content.Items.Accessories.Reaper;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Accessories;
using Terrapex.Content.Items.Armor;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.NPCs.Bosses;

namespace Terrapex.Content.Items.Consumables
{
	/// <summary>The Scarab Sultan's treasure bag: one weapon from the pool, and the Sultan's Carapace.</summary>
	public class ScarabSultanBag : ModItem
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
			itemLoot.Add(ItemDropRule.OneFromOptions(1, ScarabSultan.Weapons));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<ScarabAmulet>(), 3));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<SultansCarapace>()));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<GildedChitin>(), 1, 10, 16));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Sunsand>(), 1, 10, 16));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<ScarabSultanMask>(), 7));
			itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<ScarabSultan>()));
		}
	}
}
