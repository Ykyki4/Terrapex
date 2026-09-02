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
	/// <summary>Plan item #73. Expert-mode bag for the Keeper of the Rift.</summary>
	public class KeeperBag : ModItem
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
			Item.consumable = true;
			Item.value = 0;
		}

		public override bool CanRightClick() => true;

		public override void ModifyItemLoot(ItemLoot itemLoot)
		{
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<RiftEssence>(), 1, 22, 32));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<PlateShard>(), 1, 14, 22));
			itemLoot.Add(ItemDropRule.OneFromOptions(1,
				ModContent.ItemType<RiftshardCleaver>(),
				ModContent.ItemType<Rib>(),
				ModContent.ItemType<Riftflow>(),
				ModContent.ItemType<WardenPlate>(),
				ModContent.ItemType<OrbitLash>(),
				ModContent.ItemType<ShardCaster>()));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<CompanionEye>()));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<KeeperMask>(), 4));
			itemLoot.Add(ItemDropRule.Common(ItemID.GoldCoin, 1, 10, 16));
		}
	}
}
