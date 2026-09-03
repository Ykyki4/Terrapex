using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terrapex.Common.Systems;

namespace Terrapex.Common.UI
{
	/// <summary>
	/// The Breach's progress bar, in the place a Terraria player already looks for one.
	///
	/// Vanilla's invasion bar cannot be borrowed: <c>Main.ReportInvasionProgress</c> draws one of
	/// the game's own icons and belongs to the vanilla invasion state, so a modded event using it
	/// announces itself as a pirate raid. This is the same shape drawn out of MagicPixel instead,
	/// which also means it costs the mod no texture.
	///
	/// It reads <c>BreachSystem</c> directly rather than keeping a copy. The system syncs its kill
	/// count every kill for exactly this reason — a bar that jumps in tens reads as broken.
	/// </summary>
	public class BreachBar : ModSystem
	{
		private const int BarWidth = 220;
		private const int BarHeight = 12;

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
		{
			int mouse = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
			if (mouse < 0)
				return;

			layers.Insert(mouse, new LegacyGameInterfaceLayer("Terrapex: Breach Progress", () => {
				Draw(Main.spriteBatch);
				return true;
			}, InterfaceScaleType.UI));
		}

		private static void Draw(SpriteBatch spriteBatch)
		{
			if (!BreachSystem.Active || Main.gameMenu || Main.LocalPlayer.dead)
				return;
			// the same radius the event itself uses to decide whether anyone is still there: a
			// player who walked away from the Breach should not keep its bar on their screen
			if (Main.LocalPlayer.Distance(BreachSystem.Origin) > 4000f)
				return;

			int wave = BreachSystem.Wave;
			int waves = BreachSystem.WaveGoals.Length;
			bool finale = wave > waves;

			// during the finale the boss carries its own health bar, so this one stops counting
			// kills and just says which part of the event you are in
			int goal = finale ? 1 : BreachSystem.WaveGoals[ClampWave(wave, waves) - 1];
			int done = finale ? 1 : BreachSystem.Kills;
			float fill = goal <= 0 ? 1f : MathHelper.Clamp(done / (float)goal, 0f, 1f);

			string label = finale
				? Language.GetTextValue("Mods.Terrapex.Common.BreachBarFinale")
				: Language.GetTextValue("Mods.Terrapex.Common.BreachBar", wave, waves, done, goal);

			// UI space, so the bar sits where the player put their interface scale, not where the
			// raw framebuffer happens to end
			float screenW = Main.screenWidth / Main.UIScale;
			float screenH = Main.screenHeight / Main.UIScale;
			int x = (int)(screenW * 0.5f - BarWidth * 0.5f);
			int y = (int)(screenH - 92f);

			Texture2D pixel = TextureAssets.MagicPixel.Value;
			spriteBatch.Draw(pixel, new Rectangle(x - 2, y - 2, BarWidth + 4, BarHeight + 4),
				new Color(16, 12, 26) * 0.85f);
			spriteBatch.Draw(pixel, new Rectangle(x, y, BarWidth, BarHeight),
				new Color(59, 48, 80));

			int filled = (int)(BarWidth * fill);
			if (filled > 0)
			{
				spriteBatch.Draw(pixel, new Rectangle(x, y, filled, BarHeight), new Color(217, 78, 203));
				// a lit top edge, the same trick the sprites use: a flat bar reads as a rectangle
				spriteBatch.Draw(pixel, new Rectangle(x, y, filled, 2), new Color(255, 154, 217));
			}

			Utils.DrawBorderString(spriteBatch, label,
				new Vector2(x + BarWidth * 0.5f, y - 6f), Color.White, 1f, 0.5f, 1f);
		}

		/// <summary>Waves are 1-based and the finale runs one past the end of the table.</summary>
		private static int ClampWave(int wave, int waves) => wave < 1 ? 1 : (wave > waves ? waves : wave);
	}
}
