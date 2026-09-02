using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Accessories
{
	// "Rift dust does not hold you." Movement plus immunity to Slow — the plan's
	// sinking-sand idea has no vanilla hook to hang on, so the effect is the part
	// of it a player can actually feel.
	public class DustyBoots : ModItem
	{
		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 20;
			Item.value = Item.sellPrice(silver: 45);
			Item.rare = ItemRarityID.Blue;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			player.moveSpeed += 0.12f;
			player.runAcceleration += 0.02f;
			player.buffImmune[BuffID.Slow] = true;

			if (!hideVisual && player.velocity.LengthSquared() > 9f && Main.rand.NextBool(8))
			{
				Dust d = Dust.NewDustDirect(player.BottomLeft, player.width, 4,
					DustID.PurpleTorch, 0f, 0f, 160, default, 0.7f);
				d.noGravity = true;
				d.velocity *= 0.2f;
			}
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<RiftDust>(), 20)
				.AddIngredient(ItemID.Leather, 3)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
