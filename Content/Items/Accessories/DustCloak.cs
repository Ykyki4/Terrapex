using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// Plan item #45. The tier's mobility slot: a double-tap dash that leaves the player
	/// as a smear of dust. No damage on the dash — the Shield of Cthulhu already owns
	/// that niche, and a dash that also hits makes the whole tier's melee redundant.
	/// </summary>
	public class DustCloak : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 28;
			Item.value = Item.sellPrice(gold: 1, silver: 50);
			Item.rare = ItemRarityID.Orange;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			player.GetModPlayer<TerrapexPlayer>().dustCloak = true;
			player.moveSpeed += 0.10f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<RiftDust>(30)
				.AddIngredient<Riftglass>(8)
				.AddIngredient(ItemID.Silk, 10)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
