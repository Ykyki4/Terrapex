using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Weapons.Trees.Magic
{
	/// <summary>
	/// Says what the next cast is. A book whose page changes under you is unreadable unless the
	/// page is on screen, and the one place the eye already is during a fight is the player:
	///
	/// - Almanac, Equinox: the page's glyph over the head. The Almanac adds four pips under it,
	///   one per cast left before the book turns the page itself.
	/// - Solstice: the glyph, and the opposite page's glyph smaller beside it - the pair it casts.
	/// - Cosmogony: no head glyph at all. The four pages stand round the player, each with its
	///   glyph, and the chosen one is lit; the ring *is* the indicator.
	///
	/// The head glyph is drawn for the local player only, like the Harvest bar; the Cosmogony's
	/// ring is drawn for everyone, because it is the weapon, not a HUD.
	/// </summary>
	public static class AlmanacDraw
	{
		public const string GlyphPath = "Terrapex/Content/Projectiles/Trees/Magic/AlmanacGlyphs";
		public const string PagePath = "Terrapex/Content/Projectiles/Trees/Magic/AlmanacPage";
		public const int GlyphSize = 18;

		public static readonly Color Regard = new(0xff, 0xd7, 0x9a);

		public static Texture2D Glyphs => ModContent.Request<Texture2D>(GlyphPath).Value;
		public static Texture2D Page => ModContent.Request<Texture2D>(PagePath).Value;
		public static Texture2D Flare => ModContent.Request<Texture2D>("Terrapex/Content/Projectiles/RiftFlare").Value;

		public static Rectangle GlyphFrame(int page) => new(0, page * GlyphSize, GlyphSize, GlyphSize);

		public static Color Add(Color c, float s) => new Color(c.R, c.G, c.B, 0) * s;

		public static void Orbit(ref PlayerDrawSet info, bool front)
		{
			Player p = info.drawPlayer;
			AlmanacPlayer book = p.GetModPlayer<AlmanacPlayer>();
			Texture2D page = Page, glyphs = Glyphs, flare = Flare;
			for (int k = 0; k < AlmanacItem.Pages; k++)
			{
				float a = book.orbit + k * MathHelper.TwoPi / AlmanacItem.Pages;
				// the half of the ring above the player stands behind them
				if ((Math.Sin(a) >= 0f) != front)
					continue;

				Vector2 at = (p.MountedCenter + book.OrbitOffset(k) + new Vector2(0f, p.gfxOffY) - Main.screenPosition).Floor();
				bool chosen = k == book.page;
				float open = book.OpenAmount;
				float tilt = (float)Math.Cos(a) * 0.18f;
				float scale = MathHelper.Lerp(0.55f, 1f, open) * (front ? 1f : 0.88f);
				Color tint = AlmanacItem.PageColor[k];

				info.DrawDataCache.Add(new DrawData(flare, at, null,
					Add(chosen ? Regard : tint, (chosen ? 0.55f : 0.25f) + 0.35f * book.charge),
					0f, flare.Size() * 0.5f, (chosen ? 0.55f : 0.4f) * scale, SpriteEffects.None));

				Color paper = Color.Lerp(Color.White, Lighting.GetColor((int)(p.Center.X / 16f), (int)(p.Center.Y / 16f)), 0.3f);
				if (!front)
					paper = Color.Lerp(paper, new Color(120, 126, 160), 0.35f);
				info.DrawDataCache.Add(new DrawData(page, at, null, paper, tilt, page.Size() * 0.5f, scale, SpriteEffects.None));
				info.DrawDataCache.Add(new DrawData(glyphs, at, GlyphFrame(k), Color.White * (chosen ? 1f : 0.8f), tilt,
					new Vector2(GlyphSize * 0.5f), scale * 0.8f, SpriteEffects.None));
			}
		}

		public static void Head(ref PlayerDrawSet info, AlmanacItem item)
		{
			Player p = info.drawPlayer;
			AlmanacPlayer book = p.GetModPlayer<AlmanacPlayer>();
			Texture2D glyphs = Glyphs, flare = Flare;

			float bob = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f) * 1.5f;
			Vector2 at = (new Vector2(p.Center.X, p.Top.Y - 16f + bob + p.gfxOffY) - Main.screenPosition).Floor();
			float pop = 1f + 0.45f * (book.turnFlash / 14f);
			Color tint = AlmanacItem.PageColor[book.page];

			info.DrawDataCache.Add(new DrawData(flare, at, null, Add(tint, 0.3f + 0.5f * book.charge), 0f,
				flare.Size() * 0.5f, 0.42f * pop, SpriteEffects.None));
			info.DrawDataCache.Add(new DrawData(glyphs, at, GlyphFrame(book.page), Color.White, 0f,
				new Vector2(GlyphSize * 0.5f), pop, SpriteEffects.None));

			if (item.Level == 3)
			{
				int opposite = (book.page + 2) % AlmanacItem.Pages;
				Vector2 side = at + new Vector2(15f, 4f);
				info.DrawDataCache.Add(new DrawData(glyphs, side, GlyphFrame(opposite), Color.White * 0.75f, 0f,
					new Vector2(GlyphSize * 0.5f), 0.6f, SpriteEffects.None));
			}

			if (item.Level == 1)
			{
				int left = AlmanacPlayer.CastsPerPage - book.casts;
				Texture2D px = TextureAssets.MagicPixel.Value;
				for (int i = 0; i < AlmanacPlayer.CastsPerPage; i++)
				{
					Rectangle pip = new((int)at.X - 9 + i * 5, (int)at.Y + 12, 3, 3);
					Rectangle rim = new(pip.X - 1, pip.Y - 1, 5, 5);
					info.DrawDataCache.Add(new DrawData(px, rim, new Rectangle(0, 0, 1, 1), new Color(36, 23, 51) * 0.85f,
						0f, Vector2.Zero, SpriteEffects.None));
					info.DrawDataCache.Add(new DrawData(px, pip, new Rectangle(0, 0, 1, 1),
						i < left ? tint : new Color(63, 51, 87), 0f, Vector2.Zero, SpriteEffects.None));
				}
			}
		}

		public static AlmanacItem Held(Player p) => p.HeldItem?.ModItem as AlmanacItem;
	}

	/// <summary>The far half of the Cosmogony's ring, behind the player.</summary>
	public class AlmanacBackLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Wings);

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
			=> drawInfo.shadow == 0f && !drawInfo.drawPlayer.dead && AlmanacDraw.Held(drawInfo.drawPlayer)?.Level == 4;

		protected override void Draw(ref PlayerDrawSet drawInfo) => AlmanacDraw.Orbit(ref drawInfo, false);
	}

	/// <summary>The near half of the ring, and the page glyph over the head.</summary>
	public class AlmanacFrontLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.FrontAccFront);

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
			=> drawInfo.shadow == 0f && !drawInfo.drawPlayer.dead && AlmanacDraw.Held(drawInfo.drawPlayer) != null;

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			AlmanacItem item = AlmanacDraw.Held(drawInfo.drawPlayer);
			if (item.Level == 4)
				AlmanacDraw.Orbit(ref drawInfo, true);
			else if (drawInfo.drawPlayer.whoAmI == Main.myPlayer)
				AlmanacDraw.Head(ref drawInfo, item);
		}
	}
}
