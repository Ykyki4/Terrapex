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
	/// <summary>
	/// Plan item #81. Summons the thing that lives on the other end of the thread.
	/// </summary>
	public class Spindle : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 40;
			Item.height = 40;
			Item.damage = 34;
			Item.DamageType = DamageClass.Summon;
			Item.mana = 10;
			Item.knockBack = 3f;
			Item.useTime = 26;
			Item.useAnimation = 26;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item44;
			Item.value = Item.sellPrice(gold: 11);
			Item.rare = ItemRarityID.Yellow;
			Item.buffType = ModContent.BuffType<SpindleBuff>();
			Item.shoot = ModContent.ProjectileType<SpindleMinion>();
			Item.shootSpeed = 8f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			player.AddBuff(Item.buffType, 2);
			Projectile p = Projectile.NewProjectileDirect(source, Main.MouseWorld, Vector2.Zero,
				type, damage, knockback, player.whoAmI);
			p.originalDamage = Item.damage;
			return false;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<VoidCloth>(5)
				.AddIngredient(ItemID.HallowedBar, 10)
				.AddIngredient(ItemID.SoulofMight, 6)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
