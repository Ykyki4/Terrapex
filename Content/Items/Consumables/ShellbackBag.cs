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
	/// <summary>The Old Shellback's treasure bag: one weapon from the same pool, and the Hermit's Hull.</summary>
	public class ShellbackBag : ModItem
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
			itemLoot.Add(ItemDropRule.OneFromOptions(1, OldShellback.WeaponPool));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BarnacledHone>(), 3));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<HermitsHull>()));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BarnacleCluster>(), 1, 10, 16));
			itemLoot.Add(ItemDropRule.Common(ItemID.Seashell, 1, 3, 5));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<OldShellbackMask>(), 7));
			itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<OldShellback>()));
		}
	}
}
