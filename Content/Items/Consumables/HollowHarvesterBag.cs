using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Accessories;
using Terrapex.Content.Items.Armor;
using Terrapex.Content.Items.Weapons.Reaper;
using Terrapex.Content.NPCs.Bosses;

namespace Terrapex.Content.Items.Consumables
{
	/// <summary>The Hollow Harvester's treasure bag. In expert the Crow Charm is guaranteed.</summary>
	public class HollowHarvesterBag : ModItem
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
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<HollowHarvest>()));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrowCharm>()));
			itemLoot.Add(ItemDropRule.Common(ItemID.Hay, 1, 40, 60));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<HollowHarvesterMask>(), 7));
			itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<HollowHarvester>()));
		}
	}
}
