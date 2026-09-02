using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// Plan item #88. Every fifth shot comes out twice. A flat damage bonus would have been
	/// simpler, but this one scales with how fast a weapon fires rather than with its number,
	/// so it changes which weapon you pick instead of just adding to whichever you already had.
	/// </summary>
	public class ShardResonator : ModItem
	{
		public const int Every = 5;

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 24;
			Item.value = Item.sellPrice(gold: 9);
			Item.rare = ItemRarityID.Yellow;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
			=> player.GetModPlayer<TerrapexPlayer>().shardResonator = true;

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<VoidCloth>(6)
				.AddIngredient<RiftEssence>(8)
				.AddIngredient(ItemID.SoulofSight, 8)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
