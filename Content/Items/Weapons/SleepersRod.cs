using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>Plan item #24. Summons a sleeper eye to follow the player.</summary>
	public class SleepersRod : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
			ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
			ItemID.Sets.LockOnIgnoresCollision[Type] = true;
		}

		public override void SetDefaults()
		{
			Item.width = 36;
			Item.height = 36;
			Item.damage = 17;
			Item.DamageType = DamageClass.Summon;
			Item.mana = 10;
			Item.knockBack = 2.5f;
			Item.useTime = 26;
			Item.useAnimation = 26;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item44;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Green;
			Item.buffType = ModContent.BuffType<SleeperEyeBuff>();
			Item.shoot = ModContent.ProjectileType<SleeperEye>();
		}

		public override Vector2? HoldoutOffset() => new Vector2(-2f, -2f);

		public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source,
			Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			player.AddBuff(Item.buffType, 2);
			var proj = Projectile.NewProjectileDirect(source, Main.MouseWorld, Vector2.Zero,
				type, damage, knockback, player.whoAmI);
			proj.originalDamage = Item.damage;
			return false;
		}
	}
}
