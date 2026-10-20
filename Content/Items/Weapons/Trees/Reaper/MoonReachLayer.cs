using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terrapex.Common.Configs;
using Terrapex.Common.Reaper;
using Terrapex.Content.Projectiles.Trees.Reaper;

namespace Terrapex.Content.Items.Weapons.Trees.Reaper
{
	/// <summary>
	/// The moon over the reach. While a scythe of the Harvest Moon tree is in hand, a moon sits on
	/// the top of the reach circle with a halo in the colour of the rite the next cut will end in,
	/// a row of pips under it counting the cycle round, and a faint glow running the whole rim in
	/// that same colour. The cycle is a rotation the reaper plays, so it has to be on screen.
	///
	/// The moon changes with the node: a harvest moon, an eclipse with its corona, a night moon
	/// under stars, a black hole with its ring. Drawn over the shared dotted circle of
	/// <c>ReapRangeLayer</c>, never instead of it, and faded the same way: the halo only along the
	/// arc that faces the cursor, everything at the config's reach opacity. Local player only.
	/// </summary>
	public class MoonReachLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.FrontAccFront);

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
		{
			Player p = drawInfo.drawPlayer;
			return drawInfo.shadow == 0f && p.whoAmI == Main.myPlayer && !p.dead && !Main.gameMenu
				&& p.HeldItem.ModItem is MoonScythe;
		}

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			Player p = drawInfo.drawPlayer;
			if (p.HeldItem.ModItem is not MoonScythe scythe)
				return;
			List<DrawData> cache = drawInfo.DrawDataCache;
			// the moon and its pips are a counter, so they stay - but at the reach opacity, never louder
			float dim = 0.3f + 0.7f * TerrapexClientConfig.Opacity;
			Action<DrawData> emit = d => { d.color *= dim; cache.Add(d); };

			MoonTreePlayer moon = p.GetModPlayer<MoonTreePlayer>();
			Vector2 center = p.MountedCenter + new Vector2(0f, p.gfxOffY) - Main.screenPosition;
			float range = scythe.RangeFor(p);
			int rite = scythe.NextRite(p);
			Color tint = MoonRites.Tints[rite];
			float time = Main.GlobalTimeWrappedHourly;
			bool night = scythe.BringsNight && moon.NightFalls;
			Color rim = night ? new Color(200, 210, 255) : tint;
			float alpha = TerrapexClientConfig.Opacity;

			// the halo along the rim: soft light in the coming rite's colour, breathing
			int halo = Math.Max(24, (int)(MathHelper.TwoPi * range / 22f));
			float breathe = 0.75f + 0.25f * MathF.Sin(time * 2.2f);
			for (int i = 0; i < halo; i++)
			{
				float a = MathHelper.TwoPi * i / halo + time * 0.08f;
				float w = ReapRangeLayer.Facing(p, a);
				if (w <= 0.01f)
					continue;
				MoonDraw.Glow(emit, center + a.ToRotationVector2() * range,
					MoonDraw.Light(rim, (night ? 0.16f : 0.07f) * breathe * w * alpha), 0.26f);
			}

			// the moon, riding the top of the circle
			float angle = -MathHelper.PiOver2 + 0.06f * MathF.Sin(time * 0.7f);
			Vector2 m = center + angle.ToRotationVector2() * range;
			MoonDraw.Glow(emit, m, MoonDraw.Light(tint, 0.5f * breathe), 0.75f);
			switch (scythe.Node)
			{
				case 1:
					MoonDraw.Beads(emit, m, 20f, 16, time * 0.5f, i => MoonDraw.Light(tint, i % 2 == 0 ? 0.4f : 0.2f));
					MoonDraw.Glyph(emit, m, MoonDraw.HarvestGlyph, Color.White);
					break;
				case 2:
					for (int i = 0; i < 8; i++)
						MoonDraw.Ray(emit, m, time * 0.6f + MathHelper.TwoPi * i / 8f, 10f,
							10f + 4f * MathF.Sin(time * 3f + i), 5f, MoonDraw.Light(i % 2 == 0 ? tint : Color.White, 0.55f));
					MoonDraw.Glyph(emit, m, MoonDraw.EclipseGlyph, Color.White);
					break;
				case 3:
					for (int i = 0; i < 6; i++)
					{
						Vector2 star = m + (MathHelper.TwoPi * i / 6f + time * 0.2f).ToRotationVector2() * (18f + 3f * (i % 2));
						float tw = 0.4f + 0.6f * MathF.Max(0f, MathF.Sin(time * 3f + i * 2.1f));
						MoonDraw.Bead(emit, star, MoonDraw.Light(MoonDraw.Prime, tw));
					}
					MoonDraw.Glyph(emit, m, MoonDraw.NightGlyph, Color.White);
					break;
				default:
					MoonDraw.Accretion(emit, m, 18f, 0.34f, -0.32f, 22, time * 3f, false, Color.White, new Color(90, 70, 190), 0.8f);
					MoonDraw.Glyph(emit, m, MoonDraw.HorizonGlyph, Color.White);
					MoonDraw.Accretion(emit, m, 18f, 0.34f, -0.32f, 22, time * 3f, true, Color.White, new Color(90, 70, 190), 0.9f,
						MoonDraw.Pillars, 5);
					break;
			}

			// the pips: one per rite, inside the rim under the moon, the coming one lit and the one
			// after it half lit when an edge cut would take two
			int n = scythe.Rites;
			Vector2 row = m + new Vector2(0f, 30f);
			for (int k = 0; k < n; k++)
			{
				Vector2 at = row + new Vector2((k - (n - 1) / 2f) * 10f, 0f);
				Color c = MoonRites.Tints[k];
				if (k == rite)
				{
					MoonDraw.Glow(emit, at, MoonDraw.Light(c, 0.7f), 0.22f);
					MoonDraw.Bead(emit, at + new Vector2(0f, -2f), c);
					MoonDraw.Bead(emit, at, c);
					MoonDraw.Bead(emit, at + new Vector2(-2f, 0f), c);
					MoonDraw.Bead(emit, at + new Vector2(-2f, -2f), Color.White);
				}
				else if (scythe.EdgeDoubles && k == (rite + 1) % n)
					MoonDraw.Bead(emit, at, MoonDraw.Light(c, night ? 0.95f : 0.5f));
				else
					MoonDraw.Bead(emit, at, new Color(c.R / 3, c.G / 3, c.B / 3) * 0.8f);
			}
		}
	}

	/// <summary>
	/// The Long Night itself: for its three seconds the reach circle goes dark, stars come out
	/// inside it and its rim turns silver - the whole circle is the edge now, and it should look
	/// like a different place. Drawn behind the player so the reaper stands in it, not under it.
	/// The veil is light on purpose: it covers exactly the ground the reaper is dodging on, so it
	/// tints the circle rather than blacking it out.
	/// </summary>
	public class MoonNightLayer : PlayerDrawLayer
	{
		private const int Stars = 44;

		public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Wings);

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
		{
			Player p = drawInfo.drawPlayer;
			return drawInfo.shadow == 0f && p.whoAmI == Main.myPlayer && !p.dead && !Main.gameMenu
				&& p.HeldItem.ModItem is MoonScythe { BringsNight: true }
				&& p.GetModPlayer<MoonTreePlayer>().NightFalls;
		}

		private static float Hash(float x)
		{
			float v = MathF.Sin(x * 12.9898f) * 43758.5453f;
			return v - MathF.Floor(v);
		}

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			Player p = drawInfo.drawPlayer;
			if (p.HeldItem.ModItem is not MoonScythe scythe)
				return;
			List<DrawData> cache = drawInfo.DrawDataCache;
			Action<DrawData> emit = d => cache.Add(d);
			MoonTreePlayer moon = p.GetModPlayer<MoonTreePlayer>();

			float fade = Math.Min(1f, moon.nightAge / 12f) * Math.Min(1f, moon.nightTicks / 20f)
				* (0.3f + 0.7f * TerrapexClientConfig.Opacity);
			Vector2 center = p.MountedCenter + new Vector2(0f, p.gfxOffY) - Main.screenPosition;
			float range = scythe.RangeFor(p);
			float time = Main.GlobalTimeWrappedHourly;

			// the dark: a painted veil, a soft lip just outside it, a deeper core
			MoonDraw.Glow(emit, center, new Color(4, 6, 18) * (0.18f * fade), range * 2.6f / MoonDraw.Flare.Width);
			MoonDraw.Disc(emit, center, range, new Color(6, 8, 24) * (0.2f * fade));

			// the stars: fixed in the circle, each on its own twinkle
			for (int i = 0; i < Stars; i++)
			{
				float r = MathF.Sqrt(Hash(i + 0.31f)) * range * 0.96f;
				float a = Hash(i + 7.7f) * MathHelper.TwoPi + time * 0.03f;
				Vector2 at = center + a.ToRotationVector2() * r;
				float tw = 0.35f + 0.65f * MathF.Max(0f, MathF.Sin(time * (2f + Hash(i) * 3f) + i));
				Color c = i % 9 == 0 ? MoonDraw.Regard : MoonDraw.Prime;
				MoonDraw.Bead(emit, at, MoonDraw.Light(c, tw * fade));
				if (i % 6 == 0)
				{
					MoonDraw.Bead(emit, at + new Vector2(2f, 0f), MoonDraw.Light(c, tw * 0.4f * fade));
					MoonDraw.Bead(emit, at + new Vector2(-2f, 0f), MoonDraw.Light(c, tw * 0.4f * fade));
					MoonDraw.Bead(emit, at + new Vector2(0f, 2f), MoonDraw.Light(c, tw * 0.4f * fade));
					MoonDraw.Bead(emit, at + new Vector2(0f, -2f), MoonDraw.Light(c, tw * 0.4f * fade));
				}
			}

			// the rim, silver and solid: all of it is the edge now
			int beads = Math.Max(48, (int)(MathHelper.TwoPi * range / 6f));
			MoonDraw.Beads(emit, center, range, beads, time * 0.1f,
				i => MoonDraw.Light(new Color(200, 210, 255), (i % 2 == 0 ? 0.7f : 0.4f) * fade));
		}
	}
}
