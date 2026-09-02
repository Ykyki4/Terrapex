using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Placeable
{
	public class RiftTorch : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 100;
			ItemID.Sets.SingleUseInGamepad[Type] = true;
			ItemID.Sets.Torches[Type] = true;
		}

		public override void SetDefaults()
		{
			Item.DefaultToPlaceableTile(ModContent.TileType<RiftTorchTile>());
			Item.width = 14;
			Item.height = 24;
			Item.value = Item.sellPrice(copper: 60);
			Item.rare = ItemRarityID.White;
			Item.holdStyle = ItemHoldStyleID.HoldFront;
			Item.noWet = true;      // stays lit underwater
			Item.useTurn = true;
		}

		public override void HoldItem(Player player)
		{
			if (Main.rand.NextBool(6))
			{
				Dust d = Dust.NewDustDirect(new Vector2(player.itemLocation.X - 4f, player.itemLocation.Y - 22f),
					4, 4, DustID.PurpleTorch, 0f, 0f, 100);
				d.noGravity = true;
				d.velocity *= 0.3f;
				d.velocity.Y -= 1.2f;
			}
			Lighting.AddLight(player.itemLocation + new Vector2(0f, -18f), 0.72f, 0.34f, 0.95f);
		}

		public override void AddRecipes()
		{
			CreateRecipe(3)
				.AddIngredient(ItemID.Torch, 3)
				.AddIngredient(ModContent.ItemType<RiftDust>())
				.Register();
		}
	}
}
