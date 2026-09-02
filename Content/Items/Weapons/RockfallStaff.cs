using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// Plan item #23. Three stones queued over the cursor, each announcing its landing
	/// spot exactly as the boss's did.
	/// </summary>
	public class RockfallStaff : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 38;
			Item.height = 38;
			Item.damage = 24;
			Item.DamageType = DamageClass.Magic;
			Item.mana = 9;
			Item.knockBack = 4f;
			Item.useTime = 34;
			Item.useAnimation = 34;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item14;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Green;
			Item.shoot = ModContent.ProjectileType<FriendlyRockfall>();
			Item.shootSpeed = 0f;
		}

		public override Vector2? HoldoutOffset() => new Vector2(-2f, -2f);

		public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source,
			Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			Vector2 aim = Main.MouseWorld;
			for (int i = 0; i < 3; i++)
			{
				Vector2 spot = new Vector2(
					aim.X + (i - 1) * 46f + Main.rand.NextFloat(-10f, 10f),
					aim.Y - 260f - Main.rand.NextFloat(0f, 30f));
				Projectile.NewProjectile(source, spot, Vector2.Zero, type, damage, knockback, player.whoAmI);
			}
			return false;
		}
	}
}
