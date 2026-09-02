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
	/// Plan item #116. It does not fire anything. It decides that a circle of the world is
	/// being looked at, and pulses four times inside it.
	///
	/// There is no travel time and no aiming skill, so it is priced the other way: only one
	/// field at a time, and the field stays where it was put. Placing it well is the whole
	/// weapon, which makes it the magic counterpart to the tier's argument about facing.
	/// </summary>
	public class Regard : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 44;
			Item.height = 44;
			Item.damage = 165;
			Item.DamageType = DamageClass.Magic;
			Item.mana = 22;
			Item.knockBack = 3f;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.noMelee = true;
			Item.autoReuse = true;
			Item.UseSound = SoundID.Item28;
			Item.value = Item.sellPrice(gold: 40);
			Item.rare = ItemRarityID.Red;
			Item.shoot = ModContent.ProjectileType<GazeField>();
			Item.shootSpeed = 1f;
		}

		public override bool CanUseItem(Player player)
			=> player.ownedProjectileCounts[ModContent.ProjectileType<GazeField>()] < 1;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			Projectile p = Projectile.NewProjectileDirect(source, Main.MouseWorld, Vector2.Zero,
				type, damage, knockback, player.whoAmI);
			p.Center = Main.MouseWorld;
			return false;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<PrimordiumBar>(16)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
