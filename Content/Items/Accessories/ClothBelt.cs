using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// Plan item #87. Nothing gets to slow you down. It pairs with the Stitch on purpose —
	/// a blink weapon is worth much less if a web or a chill can pin you between casts.
	/// </summary>
	public class ClothBelt : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 18;
			Item.value = Item.sellPrice(gold: 7);
			Item.rare = ItemRarityID.Yellow;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			player.GetModPlayer<TerrapexPlayer>().clothBelt = true;
			player.buffImmune[BuffID.Slow] = true;
			player.buffImmune[BuffID.Chilled] = true;
			player.buffImmune[BuffID.Frozen] = true;
			player.buffImmune[BuffID.Webbed] = true;
			player.buffImmune[BuffID.Stoned] = true;
			player.buffImmune[BuffID.Weak] = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<VoidCloth>(8)
				.AddIngredient(ItemID.SoulofNight, 6)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
