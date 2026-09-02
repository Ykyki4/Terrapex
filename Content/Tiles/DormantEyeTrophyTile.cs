using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Terrapex.Content.Tiles
{
	/// <summary>Plan item #27's tile: a 3x3 trophy hung on a wall, like the vanilla ones.</summary>
	public class DormantEyeTrophyTile : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileLavaDeath[Type] = true;
			TileID.Sets.DisableSmartCursor[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
			TileObjectData.newTile.Height = 3;
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16 };
			TileObjectData.addTile(Type);

			DustType = DustID.Stone;
			AddMapEntry(new Color(140, 110, 170), CreateMapEntryName());
		}
	}
}
