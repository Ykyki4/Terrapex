using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Weapons
{
	public class Crackthrower : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 38;
			Item.damage = 13;
			Item.DamageType = DamageClass.Ranged;
			Item.knockBack = 2.2f;
			Item.useTime = 24;
			Item.useAnimation = 24;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item5;
			Item.value = Item.sellPrice(silver: 38);
			Item.rare = ItemRarityID.Blue;
			Item.useAmmo = AmmoID.Arrow;
			Item.shoot = ProjectileID.WoodenArrowFriendly;
			Item.shootSpeed = 7.4f;
		}

		public override Vector2? HoldoutOffset() => new Vector2(-4f, 0f);

		// One arrow in six leaves the bow doubled — the tier's "sometimes it cracks"
		// motif, kept as a flat roll so it never needs explaining in a tooltip twice.
		public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source,
			Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			if (Main.rand.NextBool(6))
			{
				Vector2 spread = velocity.RotatedByRandom(MathHelper.ToRadians(6f));
				Projectile.NewProjectile(source, position, spread, type, damage, knockback, player.whoAmI);
			}
			return true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<FissuriteBar>(), 9)
				.AddIngredient(ItemID.Wood, 8)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
