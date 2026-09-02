using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Consumables
{
	/// <summary>Plan item #74. The boss fight's own reading aid, bottled.</summary>
	public class RiftSightPotion : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 20;

		public override void SetDefaults()
		{
			Item.width = 20;
			Item.height = 26;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 12);
			Item.rare = ItemRarityID.Pink;
			Item.useStyle = ItemUseStyleID.DrinkLiquid;
			Item.useAnimation = 17;
			Item.useTime = 17;
			Item.useTurn = true;
			Item.UseSound = SoundID.Item3;
			Item.consumable = true;
			Item.buffType = ModContent.BuffType<RiftSight>();
			Item.buffTime = 60 * 60 * 6;
		}

		public override void AddRecipes()
		{
			CreateRecipe(2)
				.AddIngredient(ItemID.BottledWater, 2)
				.AddIngredient<RiftEssence>()
				.AddIngredient(ItemID.Deathweed)
				.AddTile(TileID.Bottles)
				.Register();
		}
	}
}
