using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles.Trees.Ranged;
using UnblinkingItem = Terrapex.Content.Items.Weapons.Unblinking;

namespace Terrapex.Content.Items.Weapons.Trees.Ranged
{
	/// <summary>
	/// Node 4, the Zenith step. Winds, fan, tailwind, thread and second arrow all stay; every
	/// eighth shot a rose of eight bows opens over the player - the four Wayfinder bows, the Rib,
	/// the Weft, the Shotbow and the Phantasm - and each looses its own arrow at the cursor in
	/// turn, before the Unblinking's eye at the centre lays one stare down the same line.
	/// </summary>
	public class Horizon : WindBow
	{
		protected override int FanEvery => 4;
		protected override int RoseEvery => 8;
		protected override int ArrowFlags => WindArrow.FlagTailwind | WindArrow.FlagStitch;
		protected override bool SecondArrow => true;

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 44;
			Item.height = 94;
			Item.damage = 110;
			Item.DamageType = DamageClass.Ranged;
			Item.knockBack = 3.5f;
			Item.useTime = 16;
			Item.useAnimation = 16;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item5;
			Item.useAmmo = AmmoID.Arrow;
			Item.shoot = ProjectileID.WoodenArrowFriendly;
			Item.shootSpeed = 15f;
			Item.value = Item.sellPrice(gold: 50);
			Item.rare = ItemRarityID.Purple;
		}

		public override Vector2? HoldoutOffset() => new Vector2(-4f, 0f);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<TradeWind>()
				.AddIngredient<UnblinkingItem>()
				.AddIngredient(ItemID.Phantasm)
				.AddIngredient(ItemID.LunarBar, 10)
				.AddIngredient<PrimordiumBar>(5)
				.AddTile(TileID.LunarCraftingStation)
				.Register();
		}
	}
}
