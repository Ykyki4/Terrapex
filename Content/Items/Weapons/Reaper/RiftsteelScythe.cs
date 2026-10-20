using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles.Reaper;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons.Reaper
{
	/// <summary>
	/// T3, crafted from riftsteel at the Rift Altar: the first hardmode scythe a reaper makes
	/// rather than finds. Crafted, so it cuts the shared way and differs only in reach.
	/// </summary>
	public class RiftsteelScythe : ScytheItem
	{
		public override float Reach => 104f;

		public override float RangeTiles => 18f;

		public override Color SlashColor => new(201, 189, 209);

		protected override void SetScytheDefaults()
		{
			Item.width = 48;
			Item.height = 48;
			Item.damage = 46;
			Item.knockBack = 5.5f;
			Item.useTime = 26;
			Item.useAnimation = 26;
			Item.value = Item.sellPrice(gold: 3);
			Item.rare = ItemRarityID.Pink;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<RiftsteelBar>(12)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
