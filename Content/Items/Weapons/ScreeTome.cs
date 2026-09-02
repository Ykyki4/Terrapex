using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Items.Placeable;
using Terrapex.Content.Projectiles;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// Plan item #40. The Rockfall Staff dropped stones from above; the book throws the
	/// same idea sideways, as a shotgun cone. That is deliberate — the pair should read
	/// as one spell learned twice, not as two unrelated rock weapons.
	/// </summary>
	public class ScreeTome : ModItem
	{
		private const int Stones = 5;
		private const float Spread = 0.30f;

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 36;
			Item.height = 42;
			Item.damage = 22;
			Item.DamageType = DamageClass.Magic;
			Item.mana = 12;
			Item.knockBack = 3.5f;
			Item.useTime = 34;
			Item.useAnimation = 34;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item8;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Orange;
			Item.shoot = ModContent.ProjectileType<ScreeStone>();
			Item.shootSpeed = 10.5f;
		}

		public override Vector2? HoldoutOffset() => new Vector2(-4f, -2f);

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			for (int i = 0; i < Stones; i++)
			{
				// speed varies as well as angle: a cone of identical stones arrives as a
				// single wall and reads as one hit instead of five
				Vector2 v = velocity.RotatedByRandom(Spread) * Main.rand.NextFloat(0.82f, 1.12f);
				Projectile.NewProjectile(source, position, v, type, damage, knockback, player.whoAmI);
			}
			return false;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Riftglass>(10)
				.AddIngredient<CrackedStone>(30)
				.AddIngredient(ItemID.Book)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
