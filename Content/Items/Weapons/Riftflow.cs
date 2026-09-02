using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// Plan item #60. The Keeper's own beam, handed to the player: hold to channel, and it
	/// stops at the first wall like the boss's does.
	/// </summary>
	public class Riftflow : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 40;
			Item.height = 40;
			Item.damage = 30;
			Item.DamageType = DamageClass.Magic;
			Item.mana = 8;
			Item.knockBack = 1.5f;
			Item.useTime = 6;
			Item.useAnimation = 6;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.channel = true;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.UseSound = SoundID.Item13;
			Item.value = Item.sellPrice(gold: 6);
			Item.rare = ItemRarityID.Pink;
			Item.shoot = ModContent.ProjectileType<RiftflowBeam>();
			Item.shootSpeed = 1f;
		}

		// one beam at a time, or holding the button stacks them into a white bar
		public override bool CanUseItem(Player player)
			=> player.ownedProjectileCounts[ModContent.ProjectileType<RiftflowBeam>()] < 1;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			Projectile.NewProjectile(source, player.MountedCenter, Vector2.Normalize(velocity),
				type, damage, knockback, player.whoAmI);
			return false;
		}
	}
}
