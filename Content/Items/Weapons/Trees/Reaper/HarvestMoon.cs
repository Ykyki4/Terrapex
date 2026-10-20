using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Weapons.Reaper;
using Terrapex.Content.Items.Weapons.Rime;
using Terrapex.Content.Items.Weapons.Scarab;
using Terrapex.Content.Items.Weapons.Shellback;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons.Trees.Reaper
{
	/// <summary>
	/// Node 1, T2: the four end-of-pre-hardmode scythes melted into one harvest-moon crescent.
	/// Each cut ends in the next of their four after-effects - the Undertow's whirlpool, Khepri's
	/// sun pebble, the Frostfell's icicle, the Hellreaper's burning crescent - and the Reap gives
	/// all four at once on the reticle.
	/// </summary>
	public class HarvestMoon : MoonScythe
	{
		public override int Node => 1;
		public override float Reach => 106f;
		public override float RangeTiles => 17f;
		public override Color SlashColor => new(255, 214, 140);

		protected override void SetScytheDefaults() => Defaults(44, 42, 25, 5.5f, ItemRarityID.Orange, 4);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Undertow>()
				.AddIngredient<KhepriScythe>()
				.AddIngredient<FrostfellScythe>()
				.AddIngredient<Hellreaper>()
				.AddTile<RiftAltarTile>()
				.Register();
		}
	}
}
