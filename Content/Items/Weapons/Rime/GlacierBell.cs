using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;
using Terrapex.Content.Projectiles.Rime;

namespace Terrapex.Content.Items.Weapons.Rime
{
	/// <summary>
	/// The Rime's summon drop: a bell of glacier ice on a red strap. Ringing it calls a little
	/// rime-spirit that darts into enemies and frostburns them.
	/// </summary>
	public class GlacierBell : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
			ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
			ItemID.Sets.LockOnIgnoresCollision[Type] = true;
		}

		public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 34;
			Item.damage = 21;
			Item.DamageType = DamageClass.Summon;
			Item.mana = 10;
			Item.knockBack = 2.5f;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item35;
			Item.buffType = ModContent.BuffType<RimeSpriteBuff>();
			Item.shoot = ModContent.ProjectileType<RimeSprite>();
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Orange;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			player.AddBuff(Item.buffType, 2);
			Projectile proj = Projectile.NewProjectileDirect(source, Main.MouseWorld, Vector2.Zero, type, damage, knockback, player.whoAmI);
			proj.originalDamage = Item.damage;
			return false;
		}
	}
}
