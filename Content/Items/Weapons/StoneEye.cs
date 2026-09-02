using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>Plan item #22. Every arrow it fires comes out as a ricocheting one.</summary>
	public class StoneEye : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 40;
			Item.damage = 20;
			Item.DamageType = DamageClass.Ranged;
			Item.knockBack = 2.6f;
			Item.useTime = 27;
			Item.useAnimation = 27;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item5;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Green;
			Item.useAmmo = AmmoID.Arrow;
			Item.shoot = ProjectileID.WoodenArrowFriendly;
			Item.shootSpeed = 8.4f;
		}

		public override Vector2? HoldoutOffset() => new Vector2(-4f, 0f);

		public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source,
			Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			Projectile.NewProjectile(source, position, velocity,
				ModContent.ProjectileType<StoneEyeArrow>(), damage, knockback, player.whoAmI);
			return false;
		}
	}
}
