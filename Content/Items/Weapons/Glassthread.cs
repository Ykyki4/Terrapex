using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// Plan item #42. The long whip of the tier: more reach than the Dust Lash and a tag
	/// that buys crits for the minions instead of flat damage.
	/// </summary>
	public class Glassthread : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 44;
			Item.height = 44;
			Item.damage = 24;
			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.knockBack = 2f;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.shoot = ModContent.ProjectileType<GlassthreadWhip>();
			Item.shootSpeed = 4.6f;
			Item.value = Item.sellPrice(gold: 1, silver: 80);
			Item.rare = ItemRarityID.Orange;
			Item.UseSound = SoundID.Item152;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Riftglass>(14)
				.AddIngredient(ItemID.Cobweb, 30)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
