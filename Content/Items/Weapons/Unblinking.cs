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
	/// Plan item #115. The boss's unblinking stare, handed over — hold it on one thing and it
	/// climbs to two and a half times its own damage, lose contact and it falls off three times
	/// as fast as it built.
	///
	/// It uses no ammunition on purpose. The tier already has an endless pouch for every other
	/// gun, and a channelled beam that also wants feeding is a weapon nobody keeps in the bar.
	/// </summary>
	public class Unblinking : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 58;
			Item.height = 24;
			Item.damage = 78;
			Item.DamageType = DamageClass.Ranged;
			Item.knockBack = 1.5f;
			Item.useTime = 6;
			Item.useAnimation = 6;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.channel = true;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.UseSound = SoundID.Item13;
			Item.value = Item.sellPrice(gold: 40);
			Item.rare = ItemRarityID.Red;
			Item.shoot = ModContent.ProjectileType<UnblinkingBeam>();
			Item.shootSpeed = 1f;
		}

		// one beam at a time, or holding the button stacks them into a white bar
		public override bool CanUseItem(Player player)
			=> player.ownedProjectileCounts[ModContent.ProjectileType<UnblinkingBeam>()] < 1;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			Projectile.NewProjectile(source, player.MountedCenter, Vector2.Normalize(velocity),
				type, damage, knockback, player.whoAmI);
			return false;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<PrimordiumBar>(16)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
