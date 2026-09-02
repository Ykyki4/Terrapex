using System.Collections.Generic;
using Terraria;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.GameContent.Generation;
using Terraria.WorldBuilding;
using Terrapex.Content.Tiles;

namespace Terrapex.Common.Systems
{
	// Fissurite sits in the cavern layer, just under the iron band and well above
	// the lava: rare enough to be a find, common enough that a full set of tools
	// plus armour is one honest mining trip and not three.
	public class FissuriteWorldGen : ModSystem
	{
		public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
		{
			int shinies = tasks.FindIndex(pass => pass.Name.Equals("Shinies"));
			if (shinies == -1)
				return;

			tasks.Insert(shinies + 1, new PassLegacy("Terrapex: Fissurite", (progress, config) =>
			{
				progress.Message = "Cracking the stone";

				int veins = (int)(Main.maxTilesX * Main.maxTilesY * 5.4E-05);
				int top = (int)Main.rockLayer;
				int bottom = Main.maxTilesY - 220;

				for (int k = 0; k < veins; k++)
				{
					int x = WorldGen.genRand.Next(0, Main.maxTilesX);
					int y = WorldGen.genRand.Next(top, bottom);
					WorldGen.TileRunner(x, y,
						WorldGen.genRand.Next(4, 10),      // size
						WorldGen.genRand.Next(4, 9),       // steps
						ModContent.TileType<FissuriteOreTile>());

					progress.Set(k / (float)veins);
				}
			}));
		}
	}
}
