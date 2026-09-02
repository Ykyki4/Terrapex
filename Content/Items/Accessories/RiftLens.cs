using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// Plan item #33. Ground from riftglass: it widens what the player can reach and
	/// what they can see, which is the whole point of a tier made of lenses.
	/// </summary>
	public class RiftLens : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 22;
			Item.value = Item.sellPrice(silver: 60);
			Item.rare = ItemRarityID.Green;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			player.GetModPlayer<TerrapexPlayer>().fissureSight = true;
			player.blockRange += 2;
			player.nightVision = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Riftglass>(8)
				.AddIngredient(ItemID.Lens, 2)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
