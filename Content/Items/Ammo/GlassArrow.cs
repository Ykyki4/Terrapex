using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Items.Ammo
{
	/// <summary>Plan item #32. Fifty at a time, and every one of them breaks.</summary>
	public class GlassArrow : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 99;

		public override void SetDefaults()
		{
			Item.width = 10;
			Item.height = 28;
			Item.damage = 11;
			Item.DamageType = DamageClass.Ranged;
			Item.knockBack = 2.2f;
			Item.maxStack = Item.CommonMaxStack;
			Item.consumable = true;
			Item.ammo = AmmoID.Arrow;
			Item.shoot = ModContent.ProjectileType<GlassArrowProjectile>();
			Item.shootSpeed = 3.2f;
			Item.value = Item.sellPrice(copper: 12);
			Item.rare = ItemRarityID.Blue;
		}

		public override void AddRecipes()
		{
			CreateRecipe(50)
				.AddIngredient<Riftglass>(2)
				.AddIngredient(ItemID.WoodenArrow, 50)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
