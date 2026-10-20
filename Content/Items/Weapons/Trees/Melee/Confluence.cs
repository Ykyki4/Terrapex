using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Weapons.Rime;
using Terrapex.Content.Items.Weapons.Scarab;
using Terrapex.Content.Items.Weapons.Shellback;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons.Trees.Melee
{
	/// <summary>
	/// Node 1 of the melee tree, the mod's Night's Edge: four swords from four places laminated
	/// into one. Each swing carries the next current - tide, sun, frost, bone - with that
	/// ingredient's own on-hit trick, and every third swing lets a wave of the current go.
	/// </summary>
	public class Confluence : ConfluenceBlade
	{
		public override int Node => 1;
		protected override float WaveShare => 0.6f;
		protected override float Reach => 64f;

		public override void SetDefaults() => Defaults(56, 44, 22, 5.5f, ItemRarityID.Orange, 4, 1.1f);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<WreckersCutlass>()
				.AddIngredient<SultansKhopesh>()
				.AddIngredient<Glacierbreaker>()
				.AddIngredient<Bonefissure>()
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
