using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// Plan item #106. Eight percent more of everything, six defence less. A straight trade
	/// with no cleverness in it, which is the point — the tier is full of conditional gear and
	/// one slot should stay legible.
	/// </summary>
	public class ThreadOfFate : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 24;
			Item.value = Item.sellPrice(gold: 16);
			Item.rare = ItemRarityID.Lime;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			player.GetDamage(DamageClass.Generic) += 0.08f;
			player.statDefense -= 6;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<EchoAlloy>(8)
				.AddIngredient(ItemID.SoulofFright, 12)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
