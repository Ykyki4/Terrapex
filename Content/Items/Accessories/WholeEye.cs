using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// Plan item #125. Eight plates, which is the whole shell — the Riftsteel set granted three
	/// and the Carapace Charm four, so this is that idea finished rather than repeated.
	/// Sources do not stack, so wearing it with either of those is not a trap.
	/// </summary>
	public class WholeEye : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 30;
			Item.value = Item.sellPrice(gold: 24);
			Item.rare = ItemRarityID.Red;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
			=> player.GetModPlayer<TerrapexPlayer>().GrantGuardPlates(8);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<PrimordiumBar>(12)
				.AddIngredient<PlateShard>(8)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
