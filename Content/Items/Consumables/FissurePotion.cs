using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Consumables
{
	public class FissurePotion : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 20;

		public override void SetDefaults()
		{
			Item.width = 20;
			Item.height = 26;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 1);
			Item.rare = ItemRarityID.Blue;
			Item.useStyle = ItemUseStyleID.DrinkLiquid;
			Item.useAnimation = 17;
			Item.useTime = 17;
			Item.useTurn = true;
			Item.UseSound = SoundID.Item3;
			Item.consumable = true;
			Item.buffType = ModContent.BuffType<FissureSight>();
			Item.buffTime = 60 * 60 * 4;   // four minutes, same as vanilla's utility potions
		}

		public override void AddRecipes()
		{
			CreateRecipe(2)
				.AddIngredient(ItemID.BottledWater, 2)
				.AddIngredient(ModContent.ItemType<RiftDust>(), 4)
				.AddIngredient(ItemID.Daybloom)
				.AddTile(TileID.Bottles)
				.Register();
		}
	}
}
