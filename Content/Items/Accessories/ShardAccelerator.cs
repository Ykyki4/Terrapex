using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// Plan item #70. The boss's shards wind up as they travel; this makes yours do it too.
	/// It rewards long shots and does nothing at point blank, which is the trade.
	/// </summary>
	public class ShardAccelerator : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 24;
			Item.value = Item.sellPrice(gold: 5);
			Item.rare = ItemRarityID.Pink;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
			=> player.GetModPlayer<TerrapexPlayer>().shardAccelerator = true;

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<RiftsteelBar>(6)
				.AddIngredient<RiftEssence>(3)
				.AddIngredient(ItemID.SoulofFlight, 10)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}
}
