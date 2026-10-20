using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Accessories.Trees
{
	/// <summary>
	/// Node 2: the dash goes through the pack and hits all of it, ends in a short invulnerability,
	/// and the Frozen Turtle Shell's barrier comes up below half life.
	/// Effects live in <see cref="TreeEffects"/>; see CRAFTING_TREES.md.
	/// </summary>
	public class RiftAegis : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 38;
			Item.height = 38;
			Item.value = Item.sellPrice(gold: 7);
			Item.rare = ItemRarityID.Lime;
			Item.accessory = true;
			Item.defense = 10;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			TreeEffects.Rift(player);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<SquallAegis>()
				.AddIngredient(ItemID.FrozenTurtleShell)
				.AddIngredient<RiftsteelBar>(8)
				.AddTile(TileID.TinkerersWorkbench)
				.Register();
		}
	}
}
