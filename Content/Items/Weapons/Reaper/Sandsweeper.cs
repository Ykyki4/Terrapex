using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Reaper;

namespace Terrapex.Content.Items.Weapons.Reaper
{
	/// <summary>
	/// The desert's scythe, ground out of antlion mandibles and sturdy fossil. A long, slow blade
	/// whose Reap does not spin at all: it throws a fan of sand that arcs and falls.
	/// </summary>
	public class Sandsweeper : ScytheItem
	{
		public override float Reach => 90f;

		public override float RangeTiles => 11f;

		public override Color SlashColor => new(232, 200, 122);

		protected override void SetScytheDefaults()
		{
			Item.width = 40;
			Item.height = 40;
			Item.damage = 15;
			Item.knockBack = 6f;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.value = Item.sellPrice(silver: 40);
			Item.rare = ItemRarityID.Blue;
		}

		public override void Reap(Player player, IEntitySource source, Vector2 center,
			Vector2 aim, int damage, float knockback)
		{
			BoltFan(player, source, center, aim, 7, 0.9f, 11f, ReapBolt.Sand, (int)(damage * 1.6f), knockback);
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Item71 with { Pitch = -0.3f }, center);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.AntlionMandible, 4)
				.AddIngredient(ItemID.FossilOre, 10)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
