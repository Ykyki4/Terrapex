using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Terrapex.Content.Tiles
{
	/// <summary>
	/// Plan item #52. A 1x2 hanging lantern. It carries the tier's own light colour, which
	/// is the entire reason to build one instead of a vanilla lantern.
	/// </summary>
	public class RiftLanternTile : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLighted[Type] = true;
			Main.tileLavaDeath[Type] = true;
			TileID.Sets.DisableSmartCursor[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2Top);
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16 };
			TileObjectData.addTile(Type);

			DustType = DustID.Glass;
			AddMapEntry(new Color(160, 110, 200), CreateMapEntryName());
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
		{
			r = 0.42f;
			g = 0.16f;
			b = 0.58f;
		}
	}
}
