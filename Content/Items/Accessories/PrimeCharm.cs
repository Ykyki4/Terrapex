using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>Plan item #126.</summary>
	public class PrimeCharm : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 28;
			Item.height = 28;
			Item.value = Item.sellPrice(gold: 22);
			Item.rare = ItemRarityID.Red;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			player.maxMinions++;
			player.GetDamage(DamageClass.Summon) += 0.15f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<PrimordiumBar>(12)
				.AddIngredient<EchoAlloy>(12)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
