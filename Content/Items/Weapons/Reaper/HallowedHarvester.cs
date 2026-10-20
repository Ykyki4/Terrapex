using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles.Reaper;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons.Reaper
{
	/// <summary>
	/// Post-mechanical: hallowed bars and a soul of each of the three. A blade shaped like a wing,
	/// because Hallowed gear in Terraria reads as light and feathers before it reads as metal.
	/// </summary>
	public class HallowedHarvester : ScytheItem
	{
		public override float Reach => 108f;

		public override float RangeTiles => 19f;

		public override Color SlashColor => new(255, 240, 180);

		protected override void SetScytheDefaults()
		{
			Item.width = 52;
			Item.height = 52;
			Item.damage = 58;
			Item.knockBack = 5f;
			Item.useTime = 24;
			Item.useAnimation = 24;
			Item.value = Item.sellPrice(gold: 5);
			Item.rare = ItemRarityID.LightPurple;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.HallowedBar, 14)
				.AddIngredient(ItemID.SoulofFright, 3)
				.AddIngredient(ItemID.SoulofMight, 3)
				.AddIngredient(ItemID.SoulofSight, 3)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}
}
