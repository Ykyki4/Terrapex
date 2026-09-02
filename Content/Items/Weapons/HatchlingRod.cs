using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>Plan item #41. A Riftling raised out of dust before it could learn to fly.</summary>
	public class HatchlingRod : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
			ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
			ItemID.Sets.LockOnIgnoresCollision[Type] = true;
		}

		public override void SetDefaults()
		{
			Item.width = 46;
			Item.height = 46;
			Item.damage = 26;
			Item.DamageType = DamageClass.Summon;
			Item.mana = 10;
			Item.knockBack = 3f;
			Item.useTime = 26;
			Item.useAnimation = 26;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item44;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Orange;
			Item.buffType = ModContent.BuffType<HatchlingBuff>();
			Item.shoot = ModContent.ProjectileType<RiftlingHatchling>();
		}

		public override Vector2? HoldoutOffset() => new Vector2(-2f, -2f);

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			player.AddBuff(Item.buffType, 2);
			var proj = Projectile.NewProjectileDirect(source, Main.MouseWorld, Vector2.Zero,
				type, damage, knockback, player.whoAmI);
			proj.originalDamage = Item.damage;
			return false;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<RiftDust>(35)
				.AddIngredient<Riftglass>(10)
				.AddIngredient(ItemID.Bone, 20)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
