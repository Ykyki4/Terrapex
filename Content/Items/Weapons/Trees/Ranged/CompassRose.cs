using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using RibBow = Terrapex.Content.Items.Weapons.Rib;

namespace Terrapex.Content.Items.Weapons.Trees.Ranged
{
	/// <summary>
	/// Node 2, the True Night's Edge step. Everything the Wayfinder does, and every fourth shot is
	/// the needle of the compass: all four winds at once in a fan. The Rib went into it, so the
	/// fan's arrows carry its mark - whatever they hit takes more from everyone for four seconds.
	/// </summary>
	public class CompassRose : WindBow
	{
		protected override int FanEvery => 4;

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 74;
			Item.damage = 50;
			Item.DamageType = DamageClass.Ranged;
			Item.knockBack = 3f;
			Item.useTime = 21;
			Item.useAnimation = 21;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item5;
			Item.useAmmo = AmmoID.Arrow;
			Item.shoot = ProjectileID.WoodenArrowFriendly;
			Item.shootSpeed = 11.5f;
			Item.value = Item.sellPrice(gold: 10);
			Item.rare = ItemRarityID.LightPurple;
		}

		public override Vector2? HoldoutOffset() => new Vector2(-4f, 0f);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Wayfinder>()
				.AddIngredient<RibBow>()
				.AddIngredient(ItemID.SoulofMight, 5)
				.AddIngredient(ItemID.SoulofSight, 5)
				.AddIngredient(ItemID.SoulofFright, 5)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}
}
