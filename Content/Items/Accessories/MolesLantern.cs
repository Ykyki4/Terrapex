using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>The Lantern Mole's lantern: light where you stand, and quicker digging.</summary>
	public class MolesLantern : ModItem
	{
		public const float MiningBonus = 0.15f;

		public override void SetDefaults()
		{
			Item.width = 20;
			Item.height = 26;
			Item.accessory = true;
			Item.value = Item.sellPrice(silver: 80);
			Item.rare = ItemRarityID.Blue;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			player.pickSpeed -= MiningBonus;
			Lighting.AddLight(player.Center, 0.9f, 0.7f, 0.35f);
		}
	}
}
