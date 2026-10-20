using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Armor.Reaper
{
	[AutoloadEquip(EquipType.Legs)]
	public class TidewrackBoots : ModItem
	{
		public const float ReapingSpeed = 0.06f;
		public const float MoveSpeed = 0.06f;

		public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs((int)(ReapingSpeed * 100f), (int)(MoveSpeed * 100f));

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 16;
			Item.value = Item.sellPrice(silver: 45);
			Item.rare = ItemRarityID.Blue;
			Item.defense = 3;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetAttackSpeed<ReapingDamage>() += ReapingSpeed;
			player.moveSpeed += MoveSpeed;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<BarnacleCluster>(), 8)
				.AddRecipeGroup("IronBar", 6)
				.AddIngredient(ItemID.Silk, 3)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
