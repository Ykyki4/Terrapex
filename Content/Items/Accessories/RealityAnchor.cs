using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// Plan item #127. The plan asked for immunity to every debuff *this mod* applies, and that
	/// turned out to be nothing at all: all three of Terrapex's debuffs — Cracked, Rift Mark,
	/// Glassthread — are tags applied to enemies, never to the player. Granting immunity to
	/// them would have been an accessory that did literally nothing.
	///
	/// So it does what the name actually claims instead: nothing gets to move you or take your
	/// senses. Knockback, and every vanilla debuff that steers the player or blinds them.
	/// </summary>
	public class RealityAnchor : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		/// <summary>Everything that either moves you or stops you reading the arena.</summary>
		private static readonly int[] Refused = {
			BuffID.Confused, BuffID.Cursed, BuffID.Silenced, BuffID.Slow, BuffID.Weak,
			BuffID.BrokenArmor, BuffID.Blackout, BuffID.Darkness, BuffID.Obstructed,
			BuffID.Chilled, BuffID.Frozen, BuffID.Stoned, BuffID.Webbed,
			BuffID.Suffocation, BuffID.WindPushed, BuffID.VortexDebuff
		};

		public override void SetDefaults()
		{
			Item.width = 28;
			Item.height = 30;
			Item.value = Item.sellPrice(gold: 24);
			Item.rare = ItemRarityID.Red;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			player.GetModPlayer<TerrapexPlayer>().realityAnchor = true;
			player.noKnockback = true;
			foreach (int buff in Refused)
				player.buffImmune[buff] = true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<PrimordiumBar>(12)
				.AddIngredient(ItemID.LunarBar, 10)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
