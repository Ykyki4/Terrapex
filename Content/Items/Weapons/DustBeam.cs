using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Items.Weapons
{
	public class DustBeam : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 36;
			Item.height = 36;
			Item.damage = 15;
			Item.DamageType = DamageClass.Magic;
			Item.mana = 6;
			Item.knockBack = 2.5f;
			Item.useTime = 22;
			Item.useAnimation = 22;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item43;
			Item.value = Item.sellPrice(silver: 42);
			Item.rare = ItemRarityID.Blue;
			Item.shoot = ModContent.ProjectileType<DustBeamShot>();
			Item.shootSpeed = 9f;
		}

		public override Vector2? HoldoutOffset() => new Vector2(-2f, -2f);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<FissuriteBar>(), 8)
				.AddIngredient(ModContent.ItemType<RiftDust>(), 10)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
