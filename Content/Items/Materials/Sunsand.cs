using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Materials
{
	/// <summary>Sand that has held the sun too long and still glows - the Dust Devil's. Goes into the Sun Idol.</summary>
	public class Sunsand : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 25;

		public override void SetDefaults()
		{
			Item.width = 20;
			Item.height = 16;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 4);
			Item.rare = ItemRarityID.Blue;
		}

		public override void PostUpdate() => Lighting.AddLight(Item.Center, 0.35f, 0.25f, 0.08f);

		public override Color? GetAlpha(Color lightColor) => Color.Lerp(lightColor, Color.White, 0.5f);
	}
}
