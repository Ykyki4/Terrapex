using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terrapex.Common.Configs;
using Terrapex.Content.Items.Weapons.Reaper;

namespace Terrapex.Common.Reaper
{
	/// <summary>
	/// The reach, drawn: the stretch of rim facing the cursor while a scythe is in hand, the outer
	/// band picked out as the edge, and a reticle exactly where a click would land.
	///
	/// It used to be the whole dotted circle at 45%, in front of the player, and it sat over every
	/// shot coming in - "мешает уклоняться". Only the distance along the aim is information, so only
	/// that arc is drawn (±<see cref="HalfArc"/>, fading out to its ends), behind the player, and at
	/// the opacity the client config sets. <see cref="ReachStyle.Full"/> keeps a faint full ring
	/// for those who want it; <see cref="ReachStyle.Reticle"/> drops the rim altogether.
	///
	/// The mechanic is "the further out, the harder" - and a rule about distance is only fair if
	/// the distance is on screen. The reticle warms from pale to gold as it nears the rim and pulses
	/// inside the edge band, so the player can see the crit before they take it. Local player only:
	/// four reapers' circles on one screen would be noise, and nobody else's mouse is known anyway.
	/// </summary>
	public class ReapRangeLayer : PlayerDrawLayer
	{
		/// <summary>Half the angle of rim drawn either side of the aim.</summary>
		public const float HalfArc = 0.9f;

		// behind the body: the reach is a floor marking, not a thing in front of the reaper
		public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.JimsCloak);

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
		{
			Player p = drawInfo.drawPlayer;
			return drawInfo.shadow == 0f && p.whoAmI == Main.myPlayer && !p.dead && !Main.gameMenu
				&& p.HeldItem.ModItem is ScytheItem;
		}

		/// <summary>
		/// How much of the rim at <paramref name="angle"/> is drawn, 0..1, for the local player: full
		/// facing the cursor, nothing past <see cref="HalfArc"/> in Arc mode, a faint floor in Full
		/// mode, and nothing at all in Reticle mode. Every overlay on the rim goes through this, so
		/// the tree layers fade the same way the circle does.
		/// </summary>
		public static float Facing(Player p, float angle)
		{
			ReachStyle style = TerrapexClientConfig.Style;
			if (style == ReachStyle.Reticle)
				return 0f;
			float aim = (Main.MouseWorld - p.MountedCenter).ToRotation();
			float off = Math.Abs(MathHelper.WrapAngle(angle - aim)) / HalfArc;
			float w = off >= 1f ? 0f : (1f - off) * (1f - off);
			return style == ReachStyle.Full ? 0.25f + 0.75f * w : w;
		}

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			Player p = drawInfo.drawPlayer;
			if (p.HeldItem.ModItem is not ScytheItem scythe)
				return;

			Texture2D px = TextureAssets.MagicPixel.Value;
			Rectangle one = new(0, 0, 1, 1);
			Vector2 center = p.MountedCenter + new Vector2(0f, p.gfxOffY) - Main.screenPosition;
			float range = scythe.RangeFor(p);
			Color tint = scythe.SlashColor;
			float alpha = TerrapexClientConfig.Opacity;

			// the rim: short dashes, a 2x2 pixel apiece, so it reads as the mod's grid
			int dashes = Math.Max(24, (int)(MathHelper.TwoPi * range / 12f));
			for (int i = 0; i < dashes; i++)
			{
				float a = MathHelper.TwoPi * i / dashes;
				float w = Facing(p, a);
				if (w <= 0.01f)
					continue;
				Add(ref drawInfo, px, one, center + a.ToRotationVector2() * range, tint * (0.4f * w * alpha), 2f);
			}
			// the edge band's inner line, fainter and sparser: the crit starts here
			float inner = range * ScytheItem.EdgeBand;
			int innerDashes = dashes / 2;
			for (int i = 0; i < innerDashes; i++)
			{
				float a = MathHelper.TwoPi * (i + 0.5f) / innerDashes;
				float w = Facing(p, a);
				if (w <= 0.01f)
					continue;
				Add(ref drawInfo, px, one, center + a.ToRotationVector2() * inner, new Color(255, 214, 120) * (0.2f * w * alpha), 2f);
			}

			// the reticle: four ticks round where the cut will land. It stays brighter than the
			// rim - it is the one thing on screen the player is actually aiming with
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
			float markAlpha = 0.35f + 0.5f * alpha;
			for (int i = 0; i < 4; i++)
			{
				Vector2 dir = Vector2.UnitX.RotatedBy(MathHelper.PiOver2 * i + MathHelper.PiOver4);
				Add(ref drawInfo, px, one, point + dir * gap, mark * markAlpha, 2f);
				Add(ref drawInfo, px, one, point + dir * (gap + 3f), mark * markAlpha, 2f);
			}
		}

		private static void Add(ref PlayerDrawSet drawInfo, Texture2D px, Rectangle one, Vector2 at, Color color, float size)
		{
			drawInfo.DrawDataCache.Add(new DrawData(px, new Vector2((int)at.X, (int)at.Y), one, color, 0f,
				new Vector2(0.5f, 0.5f), size, SpriteEffects.None));
		}
	}
}
