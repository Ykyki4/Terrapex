using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Terrapex.Common.Reaper
{
	/// <summary>
	/// The Harvest meter, drawn at the reaper's feet.
	///
	/// A buff icon or a corner bar can say how full it is but not *when* to press the button, and
	/// the Reap is a timing decision taken in the middle of a crowd. A strip under the player is in
	/// the one place the eye already is. It is drawn only for the local player and only while it
	/// holds something, so a party of reapers does not turn into a row of progress bars.
	/// </summary>
	public class HarvestBarLayer : PlayerDrawLayer
	{
		private const int Width = 34;
		private const int Height = 4;

		public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.FrontAccFront);

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
		{
			Player p = drawInfo.drawPlayer;
			return drawInfo.shadow == 0f && p.whoAmI == Main.myPlayer && !p.dead
				&& p.GetModPlayer<ReaperPlayer>().harvest > 0f;
		}

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			Player p = drawInfo.drawPlayer;
			ReaperPlayer reaper = p.GetModPlayer<ReaperPlayer>();
			float fill = reaper.harvest / ReaperPlayer.HarvestMax;

			Vector2 pos = new Vector2(p.Center.X, p.Bottom.Y + 10f + p.gfxOffY) - Main.screenPosition;
			Rectangle back = new((int)(pos.X - Width / 2f) - 1, (int)pos.Y - 1, Width + 2, Height + 2);
			Rectangle bar = new(back.X + 1, back.Y + 1, (int)(Width * fill), Height);

			Color ink = new(36, 23, 51);
			Color fillColor = Color.Lerp(new Color(138, 107, 69), new Color(232, 207, 148), fill);
			if (reaper.HarvestFull)
			{
				// full is a different state, not just a longer bar: it pulses toward white
				float pulse = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 9f);
				fillColor = Color.Lerp(new Color(255, 214, 120), Color.White, pulse * 0.6f);
			}

			drawInfo.DrawDataCache.Add(new DrawData(TextureAssets.MagicPixel.Value, back,
				new Rectangle(0, 0, 1, 1), ink * 0.85f, 0f, Vector2.Zero, Microsoft.Xna.Framework.Graphics.SpriteEffects.None));
			drawInfo.DrawDataCache.Add(new DrawData(TextureAssets.MagicPixel.Value, bar,
				new Rectangle(0, 0, 1, 1), fillColor, 0f, Vector2.Zero, Microsoft.Xna.Framework.Graphics.SpriteEffects.None));
		}
	}
}
