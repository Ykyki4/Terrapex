using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Accessories
{
	// Defence that only exists once the fight has gone badly. T0 has no healing to
	// spare, so the reward for staying in is a floor under the last half of the bar.
	public class PocketShard : ModItem
	{
		public const int Defense = 4;

		public override void SetDefaults()
		{
			Item.width = 20;
			Item.height = 20;
			Item.value = Item.sellPrice(silver: 50);
			Item.rare = ItemRarityID.Blue;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			if (player.statLife * 2 <= player.statLifeMax2)
			{
				player.statDefense += Defense;

				if (Main.rand.NextBool(20))
				{
					Dust d = Dust.NewDustDirect(player.position, player.width, player.height,
						DustID.PurpleTorch, 0f, 0f, 150, default, 0.8f);
					d.noGravity = true;
					d.velocity *= 0.2f;
				}
			}
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<FissuriteBar>(), 4)
				.AddIngredient(ModContent.ItemType<RiftDust>(), 8)
				.AddIngredient(ItemID.Chain)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
