using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Tools
{
	/// <summary>Plan item #91. Axe power 200%, matching the drill it is made beside.</summary>
	public class RiftAxe : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 40;
			Item.height = 40;
			Item.damage = 38;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 5f;
			Item.useTime = 8;
			Item.useAnimation = 20;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.useTurn = true;
			Item.axe = 40;
			Item.tileBoost = 1;
			Item.UseSound = SoundID.Item1;
			Item.value = Item.sellPrice(gold: 9);
			Item.rare = ItemRarityID.Yellow;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<VoidCloth>(4)
				.AddIngredient(ItemID.HallowedBar, 12)
				.AddIngredient(ItemID.SoulofMight, 5)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
