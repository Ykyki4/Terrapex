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
	/// T0. The Reap is the default spin plus the ground answering it: eight stones thrown out in a
	/// ring, the tier's crack-as-stone idea. The ring is what makes it worth saving the meter for
	/// a crowd rather than spending it on one slime.
	/// </summary>
	public class FissuriteScythe : ScytheItem
	{
		public const int ReapStones = 8;

		public override float Reach => 86f;

		public override float RangeTiles => 11f;

		public override Color SlashColor => new(201, 189, 209);

		protected override void SetScytheDefaults()
		{
			Item.width = 40;
			Item.height = 40;
			Item.damage = 16;
			Item.knockBack = 5f;
			Item.useTime = 28;
			Item.useAnimation = 28;
			Item.value = Item.sellPrice(silver: 40);
			Item.rare = ItemRarityID.Blue;
		}

		public override void Reap(Player player, IEntitySource source, Vector2 center,
			Vector2 aim, int damage, float knockback)
		{
			base.Reap(player, source, center, aim, damage, knockback);
			for (int i = 0; i < ReapStones; i++)
			{
				Vector2 v = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / ReapStones) * 7.5f + new Vector2(0f, -2.5f);
				Projectile.NewProjectile(source, center, v, ModContent.ProjectileType<ReapStone>(),
					damage, knockback * 0.5f, player.whoAmI);
			}
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<FissuriteBar>(12)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
