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
	/// Plan item #39. A crossbow that turns arrows into splitting bolts, so its damage on
	/// paper is low and its damage into a crowd is not.
	/// </summary>
	public class Cleft : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 72;
			Item.height = 36;
			Item.damage = 30;
			Item.DamageType = DamageClass.Ranged;
			Item.knockBack = 3.2f;
			Item.useTime = 32;
			Item.useAnimation = 32;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item5;
			Item.value = Item.sellPrice(gold: 2, silver: 20);
			Item.rare = ItemRarityID.Orange;
			Item.useAmmo = AmmoID.Arrow;
			Item.shoot = ModContent.ProjectileType<CleftBolt>();
			Item.shootSpeed = 11f;
		}

		public override Vector2? HoldoutOffset() => new Vector2(-14f, 0f);

		// Every arrow becomes a bolt, whatever was loaded. Ammo still costs, but the
		// weapon has exactly one behaviour, which is what a signature crossbow needs.
		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			Projectile.NewProjectile(source, position, velocity,
				ModContent.ProjectileType<CleftBolt>(), damage, knockback, player.whoAmI);
			return false;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Riftglass>(16)
				.AddIngredient(ItemID.Bone, 20)
				.AddIngredient(ItemID.Chain, 3)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
