using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// Plan item #105. One killing blow a fight is refused outright. Three minutes of cooldown
	/// is what keeps it a save rather than a second health bar.
	/// </summary>
	public class WeaveCharm : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 26;
			Item.value = Item.sellPrice(gold: 18);
			Item.rare = ItemRarityID.Lime;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
			=> player.GetModPlayer<TerrapexPlayer>().weaveCharm = true;

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<EchoAlloy>(10)
				.AddIngredient<WeaverHeart>(1)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
