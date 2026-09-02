using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>Plan item #107. A quarter of your minion hits land twice.</summary>
	public class EchoSpindle : ModItem
	{
		public const float Chance = 0.25f;
		public const float Share = 0.75f;

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 26;
			Item.value = Item.sellPrice(gold: 16);
			Item.rare = ItemRarityID.Lime;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
			=> player.GetModPlayer<TerrapexPlayer>().echoSpindle = true;

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<EchoAlloy>(10)
				.AddIngredient(ItemID.ChlorophyteBar, 8)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
