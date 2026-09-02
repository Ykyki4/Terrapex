using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Consumables
{
	/// <summary>Plan item #50. Nobody should eat this. It works.</summary>
	public class GlassBroth : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 5;
			ItemID.Sets.IsFood[Type] = true;
		}

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 24;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 8);
			Item.rare = ItemRarityID.Orange;
			Item.useStyle = ItemUseStyleID.EatFood;
			Item.useAnimation = 27;
			Item.useTime = 27;
			Item.useTurn = true;
			Item.UseSound = SoundID.Item2;
			Item.consumable = true;
			Item.buffType = ModContent.BuffType<GlassBrothBuff>();
			Item.buffTime = 60 * 60 * 12;   // twelve minutes, the vanilla plate's duration
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Riftglass>(4)
				.AddIngredient(ItemID.BowlofSoup)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
