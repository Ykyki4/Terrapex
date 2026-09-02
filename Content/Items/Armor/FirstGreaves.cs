using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Armor
{
	/// <summary>
	/// Plan item #124's legs. They own the reach line the same way the Weaver Treads own the
	/// thread-duration line — a flat number parked on the legs is what frees the four heads to
	/// say four different things instead of four versions of one thing.
	/// </summary>
	[AutoloadEquip(EquipType.Legs)]
	public class FirstGreaves : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 28;
			Item.height = 24;
			Item.value = Item.sellPrice(gold: 26);
			Item.rare = ItemRarityID.Red;
			Item.defense = 30;
		}

		public override void UpdateEquip(Player player)
		{
			player.moveSpeed += 0.16f;
			player.lifeRegen += 6;
			player.noFallDmg = true;
			player.GetModPlayer<TerrapexPlayer>().firstGreaves = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<PrimordiumBar>(16)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
