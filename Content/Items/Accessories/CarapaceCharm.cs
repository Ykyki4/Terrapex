using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// Plan item #68. Four plates instead of the set's three. It does not stack with the
	/// Riftsteel bonus — the better shell wins — so wearing both is a wasted slot rather
	/// than a hidden requirement.
	/// </summary>
	public class CarapaceCharm : ModItem
	{
		public const int Plates = 4;

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 26;
			Item.value = Item.sellPrice(gold: 5);
			Item.rare = ItemRarityID.Pink;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
			=> player.GetModPlayer<TerrapexPlayer>().GrantGuardPlates(Plates);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<PlateShard>(12)
				.AddIngredient<RiftEssence>(3)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}
}
