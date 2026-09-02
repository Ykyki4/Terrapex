using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// Plan item #99. Its reach grows with every minion you are running, which makes it the
	/// summoner whip that wants a full bench rather than the usual one that wants none.
	/// </summary>
	public class Rend : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 40;
			Item.height = 40;
			Item.damage = 68;
			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.knockBack = 3.5f;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.shoot = ModContent.ProjectileType<RendWhip>();
			Item.shootSpeed = 5.6f;
			Item.value = Item.sellPrice(gold: 22);
			Item.rare = ItemRarityID.Lime;
			Item.UseSound = SoundID.Item152;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<EchoAlloy>(12)
				.AddIngredient(ItemID.ChlorophyteBar, 6)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
