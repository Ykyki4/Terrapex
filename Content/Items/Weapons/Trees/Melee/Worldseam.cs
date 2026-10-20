using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons.Trees.Melee
{
	/// <summary>
	/// Node 3, the Terra Blade step. Warp's thread goes in: where a wave lands it leaves a seam,
	/// a cut in the air that stands open for 30 ticks and then snaps shut, hitting along its line.
	/// </summary>
	public class Worldseam : ConfluenceBlade
	{
		public override int Node => 3;
		protected override float WaveShare => 0.5f;
		protected override float Reach => 80f;

		public override void SetDefaults() => Defaults(68, 140, 20, 6.5f, ItemRarityID.Yellow, 20, 1.15f);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<TrueConfluence>()
				.AddIngredient<Warp>()
				.AddIngredient(ItemID.BrokenHeroSword)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
