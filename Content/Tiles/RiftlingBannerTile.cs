using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Terrapex.Content.Tiles
{
	/// <summary>Plan item #75's tile. Vanilla's own banner shape: 1x3, hung from the ceiling.</summary>
	public class RiftlingBannerTile : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLavaDeath[Type] = true;
			TileID.Sets.DisableSmartCursor[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2Top);
			TileObjectData.newTile.Height = 3;
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16 };
			TileObjectData.newTile.Origin = new Point16(0, 0);
			TileObjectData.newTile.AnchorTop = new AnchorData(
				AnchorType.SolidTile | AnchorType.SolidSide | AnchorType.SolidBottom, 1, 0);
			TileObjectData.addTile(Type);

			DustType = DustID.Silk;
			AddMapEntry(new Color(140, 90, 170), CreateMapEntryName());
		}
	}
}
