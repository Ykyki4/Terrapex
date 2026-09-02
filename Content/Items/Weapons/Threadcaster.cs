using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// Plan item #79. Ranged access to the same thread. Where the Seam needs two swings in
	/// melee range, this needs two shots — the trade is reach for damage per hit.
	/// </summary>
	public class Threadcaster : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 50;
			Item.height = 24;
			Item.damage = 46;
			Item.DamageType = DamageClass.Ranged;
			Item.knockBack = 2.5f;
			Item.useTime = 15;
			Item.useAnimation = 15;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item11;
			Item.value = Item.sellPrice(gold: 12);
			Item.rare = ItemRarityID.Yellow;
			Item.useAmmo = AmmoID.Bullet;
			Item.shoot = ModContent.ProjectileType<ThreadShot>();
			Item.shootSpeed = 9f;
		}

		public override Vector2? HoldoutOffset() => new Vector2(-8f, 0f);

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			Projectile.NewProjectile(source, position, velocity.RotatedByRandom(0.03f),
				ModContent.ProjectileType<ThreadShot>(), damage, knockback, player.whoAmI);
			return false;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<VoidCloth>(6)
				.AddIngredient(ItemID.HallowedBar, 12)
				.AddIngredient(ItemID.SoulofSight, 8)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
