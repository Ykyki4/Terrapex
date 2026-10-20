using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Weapons.Trees.Melee
{
	/// <summary>
	/// Node 2, the True Night's Edge step: the Cleaver's tear goes in, so the wave now leaves the
	/// blade on every swing, and every fourth swing throws all four currents at once in a fan.
	/// </summary>
	public class TrueConfluence : ConfluenceBlade
	{
		public override int Node => 2;
		protected override float WaveShare => 0.5f;
		protected override float Reach => 72f;

		public override void SetDefaults() => Defaults(62, 78, 20, 6f, ItemRarityID.LightPurple, 10, 1.1f);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Confluence>()
				.AddIngredient<RiftshardCleaver>()
				.AddIngredient(ItemID.SoulofMight, 5)
				.AddIngredient(ItemID.SoulofSight, 5)
				.AddIngredient(ItemID.SoulofFright, 5)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}
}
