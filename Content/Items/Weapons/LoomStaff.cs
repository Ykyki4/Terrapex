using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// Plan item #97. It does not fire — it puts up a frame where you point, and the frame
	/// fires. One at a time, so the decision is where to stand it, not how fast to click.
	/// </summary>
	public class LoomStaff : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 44;
			Item.height = 44;
			Item.damage = 74;
			Item.DamageType = DamageClass.Magic;
			Item.mana = 22;
			Item.knockBack = 2f;
			Item.useTime = 34;
			Item.useAnimation = 34;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.noMelee = true;
			Item.autoReuse = false;
			Item.UseSound = SoundID.Item8;
			Item.value = Item.sellPrice(gold: 24);
			Item.rare = ItemRarityID.Lime;
			Item.shoot = ModContent.ProjectileType<LoomFrame>();
			Item.shootSpeed = 1f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			// the old frame comes down when a new one goes up
			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				Projectile p = Main.projectile[i];
				if (p.active && p.owner == player.whoAmI && p.type == type)
					p.Kill();
			}
			Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero, type, damage,
				knockback, player.whoAmI);
			return false;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<EchoAlloy>(14)
				.AddIngredient(ItemID.ChlorophyteBar, 8)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
