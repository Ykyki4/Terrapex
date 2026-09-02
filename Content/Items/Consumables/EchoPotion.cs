using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Consumables
{
	/// <summary>
	/// Plan item #49. The Hollow Echo's trick in a bottle — and the only reliable way to
	/// see a Hollow Echo, which is the joke the recipe is built on.
	/// </summary>
	public class EchoPotion : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 20;

		public override void SetDefaults()
		{
			Item.width = 20;
			Item.height = 26;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 3);
			Item.rare = ItemRarityID.Orange;
			Item.useStyle = ItemUseStyleID.DrinkLiquid;
			Item.useAnimation = 17;
			Item.useTime = 17;
			Item.useTurn = true;
			Item.UseSound = SoundID.Item3;
			Item.consumable = true;
			Item.buffType = ModContent.BuffType<EchoSight>();
			Item.buffTime = 60 * 60 * 4;
		}

		public override void AddRecipes()
		{
			CreateRecipe(2)
				.AddIngredient(ItemID.BottledWater, 2)
				.AddIngredient<RiftDust>(6)
				.AddIngredient(ItemID.Deathweed)
				.AddTile(TileID.Bottles)
				.Register();
		}
	}
}
