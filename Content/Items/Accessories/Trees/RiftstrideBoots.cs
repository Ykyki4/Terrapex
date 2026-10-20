using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Accessories.Trees
{
	/// <summary>
	/// Node 2: the Wayfarer's Boots with Rocket Boots built in (Spectre-grade flight), and a
	/// landing from height that throws the nearby pack back without hurting it.
	/// Effects live in <see cref="TreeEffects"/>; see CRAFTING_TREES.md.
	/// </summary>
	public class RiftstrideBoots : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 38;
			Item.height = 36;
			Item.value = Item.sellPrice(gold: 6);
			Item.rare = ItemRarityID.Pink;
			Item.accessory = true;
			Item.defense = 2;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			TreeEffects.Riftstride(player, hideVisual);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<WayfarersBoots>()
				.AddIngredient(ItemID.RocketBoots)
				.AddIngredient<RiftsteelBar>(8)
				.AddTile(TileID.TinkerersWorkbench)
				.Register();
		}
	}
}
