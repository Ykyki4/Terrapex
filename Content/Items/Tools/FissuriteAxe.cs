using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Tools
{
	public class FissuriteAxe : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 34;
			Item.height = 34;
			Item.damage = 11;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 4.5f;
			Item.useTime = 15;
			Item.useAnimation = 22;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.axe = 15;
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
