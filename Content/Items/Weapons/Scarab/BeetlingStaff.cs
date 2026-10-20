using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;
using Terrapex.Content.Projectiles.Scarab;

namespace Terrapex.Content.Items.Weapons.Scarab
{
	/// <summary>
	/// The summon drop: a staff topped with a gilded scarab. Its beetling does what the Sultan
	/// does - it lines up on a target, holds for a moment, and dives straight through.
	/// </summary>
	public class BeetlingStaff : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
			ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
			ItemID.Sets.LockOnIgnoresCollision[Type] = true;
		}

		public override void SetDefaults()
		{
			Item.width = 40;
			Item.height = 40;
			Item.damage = 14;
			Item.DamageType = DamageClass.Summon;
			Item.mana = 10;
			Item.knockBack = 2f;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item44;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Green;
			Item.buffType = ModContent.BuffType<BeetlingBuff>();
			Item.shoot = ModContent.ProjectileType<BeetlingMinion>();
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			player.AddBuff(Item.buffType, 2);
			Projectile proj = Projectile.NewProjectileDirect(source, Main.MouseWorld, Vector2.Zero,
				type, damage, knockback, player.whoAmI);
			proj.originalDamage = Item.damage;
			return false;
		}
	}
}
