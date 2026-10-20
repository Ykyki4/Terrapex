using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Items.Weapons.Reaper;

namespace Terrapex.Content.Items.Weapons.Trees.Reaper
{
	/// <summary>
	/// Node 4, T6: the tree's last scythe, with a black hole set in its neck where the moon was.
	/// Everything below it still runs - the cycle, the double rite on the edge, the night - and
	/// every cut now opens a vortex on the point that drags small enemies to its middle, wears at
	/// what it holds, and collapses into all five rites at once. The Reap opens a larger one.
	/// </summary>
	public class EventHorizon : MoonScythe
	{
		public override int Node => 4;
		public override float Reach => 134f;
		public override float RangeTiles => 26f;
		public override Color SlashColor => new(236, 226, 255);

		protected override void SetScytheDefaults() => Defaults(60, 240, 20, 6.5f, ItemRarityID.Red, 30);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<LongNight>()
				.AddIngredient<FinalHarvest>()
				.AddIngredient<Lastlight>()
				.AddIngredient(ItemID.LunarBar, 10)
				.AddIngredient<PrimordiumBar>(5)
				.AddTile(TileID.LunarCraftingStation)
				.Register();
		}
	}
}
