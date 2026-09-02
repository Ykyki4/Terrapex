using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>Plan item #104.</summary>
	[AutoloadEquip(EquipType.Legs)]
	public class WeaverTreads : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 22;
			Item.value = Item.sellPrice(gold: 12);
			Item.rare = ItemRarityID.Lime;
			Item.defense = 14;
		}

		public override void UpdateEquip(Player player)
		{
			player.moveSpeed += 0.12f;
			player.noFallDmg = true;
			// the legs own the thread-duration line the set bonus used to waste on all four heads
			player.GetModPlayer<TerrapexPlayer>().weaverTreads = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<EchoAlloy>(12)
				.AddIngredient(ItemID.ChlorophyteBar, 8)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
