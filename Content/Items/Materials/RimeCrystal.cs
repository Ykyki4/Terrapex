using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Materials
{
	/// <summary>What is left of a Frost Wisp: a cold crystal that never quite stops glowing. Goes into the Frozen Horn.</summary>
	public class RimeCrystal : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 25;

		public override void SetDefaults()
		{
			Item.width = 18;
			Item.height = 22;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 4);
			Item.rare = ItemRarityID.Blue;
		}

		public override void PostUpdate() => Lighting.AddLight(Item.Center, 0.1f, 0.25f, 0.35f);

		public override Color? GetAlpha(Color lightColor) => Color.Lerp(lightColor, Color.White, 0.5f);
	}
}
