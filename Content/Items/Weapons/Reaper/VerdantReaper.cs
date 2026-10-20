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
	/// Chlorophyte, after Plantera: a blade of green crystal that grows in clusters rather than in
	/// a line. The last crafted scythe before the Weaver's tier.
	/// </summary>
	public class VerdantReaper : ScytheItem
	{
		public override float Reach => 112f;

		public override float RangeTiles => 21f;

		public override Color SlashColor => new(140, 230, 110);

		protected override void SetScytheDefaults()
		{
			Item.width = 52;
			Item.height = 52;
			Item.damage = 82;
			Item.knockBack = 5.5f;
			Item.useTime = 23;
			Item.useAnimation = 23;
			Item.value = Item.sellPrice(gold: 7);
			Item.rare = ItemRarityID.Lime;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.ChlorophyteBar, 16)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}
}
