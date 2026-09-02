using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Accessories;
using Terrapex.Content.Items.Armor;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Items.Weapons;

namespace Terrapex.Content.Items.Consumables
{
	/// <summary>Plan item #28. Expert-mode bag for the Dormant Eye.</summary>
	public class DormantEyeBag : ModItem
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
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<DormantCornea>(), 1, 12, 20));
			itemLoot.Add(ItemDropRule.OneFromOptions(1,
				ModContent.ItemType<Lid>(),
				ModContent.ItemType<StoneEye>(),
				ModContent.ItemType<RockfallStaff>(),
				ModContent.ItemType<SleepersRod>()));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<PlateShield>()));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<DormantEyeMask>(), 4));
			itemLoot.Add(ItemDropRule.Common(ItemID.GoldCoin, 1, 4, 7));
		}
	}
}
