using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Weapons.Trees.Melee
{
	/// <summary>
	/// Node 4, the tree's Zenith. Everything below it still runs - the currents, the wave, the
	/// fan, the seams - and on every swing the five blades the tree was built from open round the
	/// player as spectral copies and strike at the cursor, each with its own trick.
	/// </summary>
	public class Meridian : ConfluenceBlade
	{
		public override int Node => 4;
		protected override float WaveShare => 0.45f;
		protected override float Reach => 92f;

		public override void SetDefaults() => Defaults(78, 235, 18, 7f, ItemRarityID.Red, 50, 1.15f);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Worldseam>()
				.AddIngredient<Nothing>()
				.AddIngredient(ItemID.TerraBlade)
				.AddIngredient(ItemID.LunarBar, 10)
				.AddIngredient<PrimordiumBar>(5)
				.AddTile(TileID.LunarCraftingStation)
				.Register();
		}
	}
}
