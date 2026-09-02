using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// Plan item #43. Dropped by the Mirrorling, and it does what the Mirrorling does:
	/// hands the shot back. The reflected bolt keeps its own speed, so a fast projectile
	/// is a better gift than a slow one.
	/// </summary>
	public class MirrorCharm : ModItem
	{
		/// <summary>One shot in six. High enough to notice, low enough not to be a shield.</summary>
		public const int ReflectChance = 6;

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 24;
			Item.value = Item.sellPrice(gold: 1, silver: 20);
			Item.rare = ItemRarityID.Orange;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
			=> player.GetModPlayer<TerrapexPlayer>().mirrorCharm = true;
	}
}
