using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Scarab;

namespace Terrapex.Content.Items.Weapons.Scarab
{
	/// <summary>
	/// The ranged drop: a recurve bow whose limbs are a scarab's opened wing-cases. Wooden Arrows
	/// leave it as Scarab Arrows, which glance off the first wall they meet - the Sultan's sun
	/// bounces, and so do these. Any other arrow is fired as it is.
	/// </summary>
	public class SunwingBow : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 48;
			Item.damage = 24;
			Item.DamageType = DamageClass.Ranged;
			Item.knockBack = 2.5f;
			Item.useTime = 25;
			Item.useAnimation = 25;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.noMelee = true;
			Item.autoReuse = true;
			Item.UseSound = SoundID.Item5;
			Item.shoot = ProjectileID.WoodenArrowFriendly;
			Item.shootSpeed = 9f;
			Item.useAmmo = AmmoID.Arrow;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Green;
		}

		public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type,
			ref int damage, ref float knockback)
		{
			if (type == ProjectileID.WoodenArrowFriendly)
				type = ModContent.ProjectileType<ScarabArrow>();
		}

		public override Vector2? HoldoutOffset() => new Vector2(-2f, 0f);
	}
}
