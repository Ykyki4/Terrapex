using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Accessories.Trees
{
	/// <summary>
	/// Node 3: Riftstride, Lava Waders and the Anklet of the Wind - run 7.5, lava, and off a ledge
	/// at a run the thread runs out under your feet for one second.
	/// Effects live in <see cref="TreeEffects"/>; see CRAFTING_TREES.md.
	/// </summary>
	public class ThreadstepBoots : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 38;
			Item.height = 36;
			Item.value = Item.sellPrice(gold: 9);
			Item.rare = ItemRarityID.Lime;
			Item.accessory = true;
			Item.defense = 3;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			TreeEffects.Threadstep(player, hideVisual);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<RiftstrideBoots>()
				.AddIngredient(ItemID.LavaWaders)
				.AddIngredient(ItemID.AnkletoftheWind)
				.AddIngredient<VoidThread>(10)
				.AddTile(TileID.TinkerersWorkbench)
				.Register();
		}
	}
}
