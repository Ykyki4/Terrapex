using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Tools
{
	/// <summary>Plan item #90.</summary>
	public class RiftDrill : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 40;
			Item.height = 40;
			Item.damage = 34;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 3f;
			Item.useTime = 5;
			Item.useAnimation = 22;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.autoReuse = true;
			Item.useTurn = false;
			Item.channel = true;
			Item.noMelee = true;
			Item.pick = 200;
			Item.tileBoost = 2;
			Item.UseSound = SoundID.Item23;
			Item.value = Item.sellPrice(gold: 10);
			Item.rare = ItemRarityID.Yellow;
			Item.shoot = ProjectileID.MythrilDrill;
			Item.shootSpeed = 38f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<VoidCloth>(4)
				.AddIngredient(ItemID.HallowedBar, 14)
				.AddIngredient(ItemID.SoulofMight, 6)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
