using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles.Reaper;

namespace Terrapex.Content.Items.Weapons.Reaper
{
	/// <summary>
	/// T1. Its Reap does not spin: it throws three glass crescents in a fan at the cursor, each
	/// shattering into shards where it dies. The first scythe whose Reap reaches past its own
	/// blade, which is the tier's crack-as-glass idea - the light bends further than the edge.
	/// </summary>
	public class GlassReaper : ScytheItem
	{
		public override float Reach => 94f;

		public override float RangeTiles => 13f;

		public override Color SlashColor => new(185, 198, 238);

		protected override void SetScytheDefaults()
		{
			Item.width = 44;
			Item.height = 44;
			Item.damage = 24;
			Item.knockBack = 5f;
			Item.useTime = 27;
			Item.useAnimation = 27;
			Item.value = Item.sellPrice(silver: 80);
			Item.rare = ItemRarityID.Green;
		}

		public override void Reap(Player player, IEntitySource source, Vector2 center,
			Vector2 aim, int damage, float knockback)
		{
			for (int i = -1; i <= 1; i++)
			{
				Projectile.NewProjectile(source, center, aim.RotatedBy(i * 0.28f) * 13f,
					ModContent.ProjectileType<ReapGlass>(), damage * 2, knockback, player.whoAmI);
			}
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Item71 with { Pitch = 0.4f }, center);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Riftglass>(12)
				.AddIngredient<FissuriteBar>(6)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
