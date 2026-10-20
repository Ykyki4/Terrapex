using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Trees.Ranged;
using Terrapex.Content.Tiles;
using WeftGun = Terrapex.Content.Items.Weapons.Weft;

namespace Terrapex.Content.Items.Weapons.Trees.Ranged
{
	/// <summary>
	/// Node 3, the Terra Blade step. The Compass Rose's winds and fan, and now distance pays: an
	/// arrow that has flown 25 tiles catches the trade wind - it stops falling, hits for x1.4 and
	/// streams ribbons behind it. The Weft went in, so a tailwind arrow threads its first hit on to
	/// the next body; the Chlorophyte Shotbow went in, so every shot looses a lighter second arrow
	/// of the opposite wind beside the first.
	/// </summary>
	public class TradeWind : WindBow
	{
		protected override int FanEvery => 4;
		protected override int ArrowFlags => WindArrow.FlagTailwind | WindArrow.FlagStitch;
		protected override bool SecondArrow => true;

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 34;
			Item.height = 82;
			Item.damage = 64;
			Item.DamageType = DamageClass.Ranged;
			Item.knockBack = 3f;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item5;
			Item.useAmmo = AmmoID.Arrow;
			Item.shoot = ProjectileID.WoodenArrowFriendly;
			Item.shootSpeed = 13f;
			Item.value = Item.sellPrice(gold: 18);
			Item.rare = ItemRarityID.Yellow;
		}

		public override Vector2? HoldoutOffset() => new Vector2(-4f, 0f);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<CompassRose>()
				.AddIngredient<WeftGun>()
				.AddIngredient(ItemID.ChlorophyteShotbow)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
