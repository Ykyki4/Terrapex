using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Rime;

namespace Terrapex.Content.Items.Weapons.Rime
{
	/// <summary>
	/// The Rime's ranged drop: a horn-tipped bow crusted with ice. Wooden arrows leave it as
	/// icicles that fly flat, drop, and burst into three splinters wherever they break. Any other
	/// arrow flies as itself.
	/// </summary>
	public class HoarfrostBow : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 50;
			Item.damage = 30;
			Item.DamageType = DamageClass.Ranged;
			Item.knockBack = 2.5f;
			Item.useTime = 26;
			Item.useAnimation = 26;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item5;
			Item.useAmmo = AmmoID.Arrow;
			Item.shoot = ProjectileID.WoodenArrowFriendly;
			Item.shootSpeed = 9.5f;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Orange;
		}

		public override Vector2? HoldoutOffset() => new Vector2(-4f, 0f);

		public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type,
			ref int damage, ref float knockback)
		{
			if (type == ProjectileID.WoodenArrowFriendly)
				type = ModContent.ProjectileType<RimeArrow>();
		}
	}
}
