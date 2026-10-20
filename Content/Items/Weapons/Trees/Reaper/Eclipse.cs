using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Weapons.Reaper;

namespace Terrapex.Content.Items.Weapons.Trees.Reaper
{
	/// <summary>
	/// Node 2, T4: the Harvest Moon with the Keeper's Harvest set over it like a dark disc. The
	/// cycle gains the Keeper's rift shards as a fifth rite, and a cut on the edge gives two rites
	/// at once - the current one and the next - under a corona drawn on the point.
	/// </summary>
	public class Eclipse : MoonScythe
	{
		public override int Node => 2;
		public override float Reach => 112f;
		public override float RangeTiles => 19f;
		public override Color SlashColor => new(255, 190, 120);

		protected override void SetScytheDefaults() => Defaults(48, 78, 23, 6f, ItemRarityID.Pink, 8);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<HarvestMoon>()
				.AddIngredient<KeepersHarvest>()
				.AddIngredient(ItemID.SoulofMight, 5)
				.AddIngredient(ItemID.SoulofSight, 5)
				.AddIngredient(ItemID.SoulofFright, 5)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}
}
