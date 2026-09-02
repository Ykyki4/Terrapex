using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>Plan item #61. A slab of the Keeper's shell, still looking for something to guard.</summary>
	public class WardenPlate : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
			ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
			ItemID.Sets.LockOnIgnoresCollision[Type] = true;
		}

		public override void SetDefaults()
		{
			Item.width = 38;
			Item.height = 38;
			Item.damage = 38;
			Item.DamageType = DamageClass.Summon;
			Item.mana = 10;
			Item.knockBack = 4f;
			Item.useTime = 26;
			Item.useAnimation = 26;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item44;
			Item.value = Item.sellPrice(gold: 6);
			Item.rare = ItemRarityID.Pink;
			Item.buffType = ModContent.BuffType<WardenPlateBuff>();
			Item.shoot = ModContent.ProjectileType<WardenPlateMinion>();
		}

		public override Vector2? HoldoutOffset() => new Vector2(-2f, -2f);

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			player.AddBuff(Item.buffType, 2);
			var proj = Projectile.NewProjectileDirect(source, player.MountedCenter, Vector2.Zero,
				type, damage, knockback, player.whoAmI);
			proj.originalDamage = Item.damage;
			return false;
		}
	}
}
