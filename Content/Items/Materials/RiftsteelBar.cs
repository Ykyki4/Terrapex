using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Materials
{
	/// <summary>
	/// Plan item #57. Shell plus Soul of Night: the tier's only crafted metal, and the gate
	/// on everything the player builds rather than loots.
	/// </summary>
	public class RiftsteelBar : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 25;

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 20;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 45);
			Item.rare = ItemRarityID.Pink;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<PlateShard>(4)
				.AddIngredient(ItemID.SoulofNight, 2)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}
}
