using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Ammo
{
	/// <summary>
	/// Plan item #120. Endless, because ammo is not a difficulty in the last tier of a mod and
	/// pretending it is only means a trip back to a chest.
	///
	/// Non-consumable ammo is how vanilla's endless pouches work: nothing is spent because
	/// nothing is <c>consumable</c>. It is a stack of one and it stays a stack of one.
	/// </summary>
	public class Primordust : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 24;
			Item.damage = 32;
			Item.DamageType = DamageClass.Ranged;
			Item.knockBack = 2.5f;
			Item.maxStack = 1;
			Item.consumable = false;
			Item.ammo = AmmoID.Bullet;
			Item.shoot = ModContent.ProjectileType<PrimalBullet>();
			Item.shootSpeed = 5f;
			Item.value = Item.sellPrice(gold: 8);
			Item.rare = ItemRarityID.Red;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<PrimordiumBar>(5)
				.AddIngredient(ItemID.EndlessMusketPouch, 1)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
