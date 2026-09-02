using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// Plan item #59. A rib pulled off the Keeper and strung. Every arrow it fires becomes a
	/// marking arrow, so the bow's value is what it does for everyone else's damage.
	/// </summary>
	public class Rib : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 34;
			Item.height = 56;
			Item.damage = 34;
			Item.DamageType = DamageClass.Ranged;
			Item.knockBack = 3.4f;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item5;
			Item.value = Item.sellPrice(gold: 6);
			Item.rare = ItemRarityID.Pink;
			Item.useAmmo = AmmoID.Arrow;
			Item.shoot = ModContent.ProjectileType<RibArrow>();
			Item.shootSpeed = 12f;
		}

		public override Vector2? HoldoutOffset() => new Vector2(-6f, 0f);

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			Projectile.NewProjectile(source, position, velocity,
				ModContent.ProjectileType<RibArrow>(), damage, knockback, player.whoAmI);
			return false;
		}
	}
}
