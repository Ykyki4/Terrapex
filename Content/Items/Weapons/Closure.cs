using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// Plan item #118. The longest whip in the mod, and its tag does one thing rather than a
	/// percentage: a minion striking a marked target always crits.
	///
	/// That is deliberately not a number the player has to model. Every other tag in this mod
	/// is a multiplier somewhere in a tooltip; this one is legible from the first swing because
	/// yellow numbers start coming out of the thing you hit.
	/// </summary>
	public class Closure : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 42;
			Item.height = 42;
			Item.damage = 118;
			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.knockBack = 4f;
			Item.useTime = 32;
			Item.useAnimation = 32;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.shoot = ModContent.ProjectileType<ClosureWhip>();
			Item.shootSpeed = 6.2f;
			Item.value = Item.sellPrice(gold: 36);
			Item.rare = ItemRarityID.Red;
			Item.UseSound = SoundID.Item152;
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
