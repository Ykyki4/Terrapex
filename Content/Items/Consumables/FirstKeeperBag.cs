using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Accessories;
using Terrapex.Content.Items.Ammo;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Items.Mounts;
using Terrapex.Content.Items.Pets;
using Terrapex.Content.Items.Weapons;

namespace Terrapex.Content.Items.Consumables
{
	/// <summary>Plan item #131. Expert bag for the First Keeper.</summary>
	public class FirstKeeperBag : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 3;
			ItemID.Sets.BossBag[Type] = true;
		}

		public override void SetDefaults()
		{
			Item.width = 34;
			Item.height = 34;
			Item.maxStack = Item.CommonMaxStack;
			Item.rare = ItemRarityID.Expert;
			Item.expert = true;
		}

		public override bool CanRightClick() => true;

		public override void ModifyItemLoot(ItemLoot itemLoot)
		{
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Primordium>(), 1, 30, 40));
			itemLoot.Add(ItemDropRule.OneFromOptions(1,
				ModContent.ItemType<FirstShard>(),
				ModContent.ItemType<Unblinking>(),
				ModContent.ItemType<Regard>(),
				ModContent.ItemType<KeeperStaff>(),
				ModContent.ItemType<Closure>()));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Nothing>(), 10));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<WholeEye>(), 1));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<RealityAnchor>(), 1));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Primordust>(), 1));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Shardling>(), 1));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<RidingPlate>(), 4));
		}
	}
}
