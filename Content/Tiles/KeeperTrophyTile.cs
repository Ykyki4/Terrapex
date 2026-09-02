using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Terrapex.Content.Tiles
{
	/// <summary>Plan item #72's tile: a 3x3 trophy hung on a wall.</summary>
	public class KeeperTrophyTile : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileLavaDeath[Type] = true;
			Main.tileLighted[Type] = true;
			TileID.Sets.DisableSmartCursor[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
			TileObjectData.newTile.Height = 3;
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16 };
			TileObjectData.addTile(Type);

			DustType = DustID.Stone;
			AddMapEntry(new Color(160, 100, 190), CreateMapEntryName());
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
		{
			r = 0.12f;
			g = 0.04f;
			b = 0.16f;
		}
	}
}
