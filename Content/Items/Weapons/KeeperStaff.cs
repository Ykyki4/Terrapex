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
	/// <summary>Plan item #117. A Keeper of your own, plates and all.</summary>
	public class KeeperStaff : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
			ItemID.Sets.StaffMinionSlotsRequired[Type] = 1f;
		}

		public override void SetDefaults()
		{
			Item.width = 46;
			Item.height = 46;
			Item.damage = 108;
			Item.DamageType = DamageClass.Summon;
			Item.mana = 16;
			Item.knockBack = 2f;
			Item.useTime = 26;
			Item.useAnimation = 26;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item44;
			Item.value = Item.sellPrice(gold: 36);
			Item.rare = ItemRarityID.Red;
			Item.buffType = ModContent.BuffType<KeeperMinionBuff>();
			Item.shoot = ModContent.ProjectileType<KeeperMinion>();
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
				.AddIngredient<PrimordiumBar>(14)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
