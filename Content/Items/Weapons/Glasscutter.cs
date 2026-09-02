using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>Plan item #31. Crafted, not dropped: the glass branch's own blade.</summary>
	public class Glasscutter : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 40;
			Item.height = 40;
			Item.damage = 27;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 4.2f;
			Item.useTime = 18;
			Item.useAnimation = 18;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.UseSound = SoundID.Item1;
			Item.value = Item.sellPrice(silver: 80);
			Item.rare = ItemRarityID.Green;
			Item.crit = 12;
			Item.shoot = ModContent.ProjectileType<GlassEdge>();
			Item.shootSpeed = 11f;
		}

		// Fires on every swing, but the pane is short-lived and slows down fast, so it extends
		// the blade rather than replacing it. On a crit it throws a second, wider pane.
		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			Projectile.NewProjectile(source, position, velocity,
				type, (int)(damage * 0.45f), knockback * 0.4f, player.whoAmI);

			if (Main.rand.Next(100) < player.GetWeaponCrit(Item))
			{
				Projectile.NewProjectile(source, position, velocity.RotatedBy(0.22f) * 0.85f,
					type, (int)(damage * 0.45f), knockback * 0.4f, player.whoAmI);
				Projectile.NewProjectile(source, position, velocity.RotatedBy(-0.22f) * 0.85f,
					type, (int)(damage * 0.45f), knockback * 0.4f, player.whoAmI);
			}
			return false;
		}

		public override void MeleeEffects(Player player, Rectangle hitbox)
		{
			if (Main.rand.NextBool(4))
			{
				Dust d = Dust.NewDustDirect(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height,
					DustID.Glass, 0f, 0f, 120, default, 0.9f);
				d.noGravity = true;
				d.velocity *= 0.4f;
			}
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Riftglass>(12)
				.AddIngredient<FissuriteBar>(6)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
