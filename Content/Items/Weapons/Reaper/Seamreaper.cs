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
	/// T4, void thread and void cloth: the tier's dark machine with turquoise thread for lights. A
	/// needle more than a blade, its thread looped back from the point to the haft.
	/// </summary>
	public class Seamreaper : ScytheItem
	{
		public override float Reach => 112f;

		public override float RangeTiles => 20f;

		public override Color SlashColor => new(127, 242, 224);

		protected override void SetScytheDefaults()
		{
			Item.width = 52;
			Item.height = 52;
			Item.damage = 70;
			Item.knockBack = 5f;
			Item.useTime = 24;
			Item.useAnimation = 24;
			Item.value = Item.sellPrice(gold: 6);
			Item.rare = ItemRarityID.Lime;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<VoidThread>(12)
				.AddIngredient<VoidCloth>(8)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
