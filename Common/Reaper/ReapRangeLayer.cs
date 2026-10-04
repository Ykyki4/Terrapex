using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terrapex.Content.Items.Weapons.Reaper;

namespace Terrapex.Common.Reaper
{
	/// <summary>
	/// The reach, drawn: a dotted circle round the reaper while a scythe is in hand, the outer
	/// band picked out as the edge, and a reticle exactly where a click would land.
	///
	/// The mechanic is "the further out, the harder" - and a rule about distance is only fair if
	/// the distance is on screen. The reticle warms from pale to gold as it nears the rim and pulses
	/// inside the edge band, so the player can see the crit before they take it. Local player only:
	/// four reapers' circles on one screen would be noise, and nobody else's mouse is known anyway.
	/// </summary>
	public class ReapRangeLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.FrontAccFront);

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
		{
			Player p = drawInfo.drawPlayer;
			return drawInfo.shadow == 0f && p.whoAmI == Main.myPlayer && !p.dead && !Main.gameMenu
				&& p.HeldItem.ModItem is ScytheItem;
		}

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			Player p = drawInfo.drawPlayer;
			if (p.HeldItem.ModItem is not ScytheItem scythe)
				return;

			Texture2D px = TextureAssets.MagicPixel.Value;
			Rectangle one = new(0, 0, 1, 1);
			Vector2 center = p.MountedCenter + new Vector2(0f, p.gfxOffY) - Main.screenPosition;
			float range = scythe.RangePixels;
			Color tint = scythe.SlashColor;

			// the rim: short dashes, a 2x2 pixel apiece, so it reads as the mod's grid
			int dashes = Math.Max(24, (int)(MathHelper.TwoPi * range / 12f));
			for (int i = 0; i < dashes; i++)
			{
				Vector2 at = center + Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / dashes) * range;
				Add(ref drawInfo, px, one, at, tint * 0.45f, 2f);
			}
			// the edge band's inner line, fainter and sparser: the crit starts here
			float inner = range * ScytheItem.EdgeBand;
			int innerDashes = dashes / 2;
			for (int i = 0; i < innerDashes; i++)
			{
				Vector2 at = center + Vector2.UnitX.RotatedBy(MathHelper.TwoPi * (i + 0.5f) / innerDashes) * inner;
				Add(ref drawInfo, px, one, at, new Color(255, 214, 120) * 0.22f, 2f);
			}

			// the reticle: four ticks round where the cut will land
			Vector2 point = scythe.StrikePoint(p, Main.MouseWorld, out float share) - Main.screenPosition;
			bool edge = share >= ScytheItem.EdgeBand;
			Color mark = Color.Lerp(new Color(230, 225, 240), new Color(255, 214, 120), share);
			float gap = 6f;
			if (edge)
			{
				float pulse = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 10f);
				mark = Color.Lerp(new Color(255, 214, 120), Color.White, pulse * 0.5f);
				gap = 7f + pulse * 2f;
			}
			for (int i = 0; i < 4; i++)
			{
				Vector2 dir = Vector2.UnitX.RotatedBy(MathHelper.PiOver2 * i + MathHelper.PiOver4);
				Add(ref drawInfo, px, one, point + dir * gap, mark * 0.9f, 2f);
				Add(ref drawInfo, px, one, point + dir * (gap + 3f), mark * 0.9f, 2f);
			}
		}

		private static void Add(ref PlayerDrawSet drawInfo, Texture2D px, Rectangle one, Vector2 at, Color color, float size)
		{
			drawInfo.DrawDataCache.Add(new DrawData(px, new Vector2((int)at.X, (int)at.Y), one, color, 0f,
				new Vector2(0.5f, 0.5f), size, SpriteEffects.None));
		}
	}
}
