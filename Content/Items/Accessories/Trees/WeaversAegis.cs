using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Accessories.Trees
{
	/// <summary>
	/// Node 3: Paladin's Shield and Master Ninja Gear (dodge, climb) on top, and every dash leaves a
	/// friendly thread along its path for two seconds.
	/// Effects live in <see cref="TreeEffects"/>; see CRAFTING_TREES.md.
	/// </summary>
	public class WeaversAegis : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 38;
			Item.height = 42;
			Item.value = Item.sellPrice(gold: 11);
			Item.rare = ItemRarityID.Yellow;
			Item.accessory = true;
			Item.defense = 16;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			TreeEffects.Weaver(player);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<RiftAegis>()
				.AddIngredient(ItemID.PaladinsShield)
				.AddIngredient(ItemID.MasterNinjaGear)
				.AddIngredient<EchoAlloy>(5)
				.AddTile(TileID.TinkerersWorkbench)
				.Register();
		}
	}
}
