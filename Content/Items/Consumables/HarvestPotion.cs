using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;

namespace Terrapex.Content.Items.Consumables
{
	/// <summary>
	/// The class's potion, from the alchemy table like the other three classes' own: water, a
	/// daybloom and a handful of hay. Every class in Terraria has one; a fifth class without one
	/// would feel like a guest.
	/// </summary>
	public class HarvestPotion : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 20;

		public override void SetDefaults()
		{
			Item.width = 20;
			Item.height = 30;
			Item.useStyle = ItemUseStyleID.DrinkLiquid;
			Item.useAnimation = 17;
			Item.useTime = 17;
			Item.useTurn = true;
			Item.UseSound = SoundID.Item3;
			Item.maxStack = Item.CommonMaxStack;
			Item.consumable = true;
			Item.rare = ItemRarityID.Blue;
			Item.value = Item.buyPrice(silver: 20);
			Item.buffType = ModContent.BuffType<HarvestBuff>();
			Item.buffTime = 60 * 60 * 4;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.BottledWater)
				.AddIngredient(ItemID.Daybloom)
				.AddIngredient(ItemID.Hay, 5)
				.AddTile(TileID.Bottles)
				.Register();
		}
	}
}
