using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>Plan item #89. A hundred and fifty of flight, cut from the same cloth.</summary>
	[AutoloadEquip(EquipType.Wings)]
	public class RiftWings : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
			ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(150, 8.2f, 2.5f);
		}

		public override void SetDefaults()
		{
			Item.width = 34;
			Item.height = 26;
			Item.value = Item.sellPrice(gold: 15);
			Item.rare = ItemRarityID.Yellow;
			Item.accessory = true;
		}

		public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling,
			ref float ascentWhenRising, ref float maxCanAscendMultiplier,
			ref float maxAscentMultiplier, ref float constantAscend)
		{
			ascentWhenFalling = 0.88f;
			ascentWhenRising = 0.16f;
			maxCanAscendMultiplier = 1f;
			maxAscentMultiplier = 3.1f;
			constantAscend = 0.135f;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<VoidCloth>(16)
				.AddIngredient<RiftEssence>(10)
				.AddIngredient(ItemID.SoulofFlight, 20)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
