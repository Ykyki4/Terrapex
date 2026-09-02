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
	/// Plan item #98. A minion that does not follow you: it hangs where it was cast and cuts
	/// whatever passes. Two of them make a corridor, which is the only reason to take it over
	/// a flying minion that never needs thinking about.
	/// </summary>
	public class Sailcloth : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 42;
			Item.height = 42;
			Item.damage = 62;
			Item.DamageType = DamageClass.Summon;
			Item.mana = 14;
			Item.knockBack = 2f;
			Item.useTime = 28;
			Item.useAnimation = 28;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item44;
			Item.value = Item.sellPrice(gold: 22);
			Item.rare = ItemRarityID.Lime;
			Item.buffType = ModContent.BuffType<SailclothBuff>();
			Item.shoot = ModContent.ProjectileType<SailclothMinion>();
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
				.AddIngredient<EchoAlloy>(12)
				.AddIngredient(ItemID.ChlorophyteBar, 6)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
