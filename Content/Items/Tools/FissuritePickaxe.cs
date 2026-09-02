using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Tools
{
	// Pick power 60 — above gold (55), below the deathbringer/nightmare pair (65).
	// It exists so fissurite pays for the next fissurite.
	public class FissuritePickaxe : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 34;
			Item.height = 34;
			Item.damage = 8;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 3f;
			Item.useTime = 13;
			Item.useAnimation = 19;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.pick = 60;
			Item.autoReuse = true;
			Item.useTurn = true;
			Item.UseSound = SoundID.Item1;
			Item.value = Item.sellPrice(silver: 22);
			Item.rare = ItemRarityID.Blue;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<FissuriteBar>(), 12)
				.AddIngredient(ItemID.Wood, 4)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
