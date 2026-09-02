using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>Plan item #128. Two hundred and sixty of flight, and a dash on top.</summary>
	[AutoloadEquip(EquipType.Wings)]
	public class FirstWings : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
			ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(260, 10.5f, 3.2f);
		}

		public override void SetDefaults()
		{
			Item.width = 38;
			Item.height = 28;
			Item.value = Item.sellPrice(gold: 30);
			Item.rare = ItemRarityID.Red;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			// dashType 1 is Tabi's, which is the one that leaves the player in control of where
			// they land - the shield dash would fight the flight this accessory already grants
			if (player.dashType == 0)
				player.dashType = 1;
		}

		public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling,
			ref float ascentWhenRising, ref float maxCanAscendMultiplier,
			ref float maxAscentMultiplier, ref float constantAscend)
		{
			ascentWhenFalling = 0.95f;
			ascentWhenRising = 0.20f;
			maxCanAscendMultiplier = 1f;
			maxAscentMultiplier = 3.6f;
			constantAscend = 0.15f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<PrimordiumBar>(14)
				.AddIngredient<VoidCloth>(16)
				.AddIngredient(ItemID.SoulofFlight, 20)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
