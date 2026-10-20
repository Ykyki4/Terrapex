using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Tiles;
using Blunderbuss = Terrapex.Content.Items.Weapons.Shellback.BarnacleBlunderbuss;
using Sunwing = Terrapex.Content.Items.Weapons.Scarab.SunwingBow;
using Hoarfrost = Terrapex.Content.Items.Weapons.Rime.HoarfrostBow;
using CleftBow = Terrapex.Content.Items.Weapons.Cleft;

namespace Terrapex.Content.Items.Weapons.Trees.Ranged
{
	/// <summary>
	/// Node 1 of the ranged tree, the bows' Night's Edge. Four weapons from four places - the
	/// Shellback's gun, the Scarab's bow, the Rime's bow and the Cleft - and wooden arrows leave
	/// it as their four winds in turn: spray bursts into three, sun glances off walls, frost drops
	/// an icicle where it lands, the cleft goes through.
	/// </summary>
	public class Wayfinder : WindBow
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 64;
			Item.damage = 30;
			Item.DamageType = DamageClass.Ranged;
			Item.knockBack = 2.5f;
			Item.useTime = 23;
			Item.useAnimation = 23;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item5;
			Item.useAmmo = AmmoID.Arrow;
			Item.shoot = ProjectileID.WoodenArrowFriendly;
			Item.shootSpeed = 10f;
			Item.value = Item.sellPrice(gold: 4);
			Item.rare = ItemRarityID.Orange;
		}

		public override Vector2? HoldoutOffset() => new Vector2(-4f, 0f);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Blunderbuss>()
				.AddIngredient<Sunwing>()
				.AddIngredient<Hoarfrost>()
				.AddIngredient<CleftBow>()
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
