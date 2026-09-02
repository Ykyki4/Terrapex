using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Tools
{
	public class FissuriteHammer : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 34;
			Item.height = 34;
			Item.damage = 14;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 6f;
			Item.useTime = 19;
			Item.useAnimation = 26;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.hammer = 60;
			Item.autoReuse = true;
			Item.useTurn = true;
			Item.UseSound = SoundID.Item1;
			Item.value = Item.sellPrice(silver: 20);
			Item.rare = ItemRarityID.Blue;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<FissuriteBar>(), 10)
				.AddIngredient(ItemID.Wood, 4)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
