using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Items.Weapons
{
	// T0 melee. 19 damage sits between gold (17) and the demonite pair (23-25),
	// which is where the whole fissurite tier is meant to live.
	public class Skol : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 40;
			Item.height = 40;
			Item.damage = 19;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 5f;
			Item.useTime = 21;
			Item.useAnimation = 21;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.UseSound = SoundID.Item1;
			Item.value = Item.sellPrice(silver: 40);
			Item.rare = ItemRarityID.Blue;
			Item.scale = 1.05f;
			Item.shoot = ModContent.ProjectileType<StoneChip>();
			Item.shootSpeed = 9f;
		}

		// Every third swing throws a chip. A counter rather than a random roll: the player can
		// feel a rhythm and time it, which a 1-in-3 chance never gives them.
		private int swings;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			if (++swings < 3)
				return false;

			swings = 0;
			Projectile.NewProjectile(source, position, velocity.RotatedByRandom(0.06f),
				type, (int)(damage * 0.55f), knockback * 0.5f, player.whoAmI);
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.6f }, position);
			return false;
		}

		public override void MeleeEffects(Player player, Rectangle hitbox)
		{
			if (Main.rand.NextBool(5))
			{
				Dust d = Dust.NewDustDirect(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height,
					DustID.PurpleTorch, 0f, 0f, 120, default, 0.9f);
				d.noGravity = true;
				d.velocity *= 0.25f;
			}
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<FissuriteBar>(), 9)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
