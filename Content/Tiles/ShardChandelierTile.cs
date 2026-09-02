using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ObjectData;

namespace Terrapex.Content.Tiles
{
	/// <summary>Plan item #53. A 3x3 chandelier hung from the ceiling, vanilla's own shape.</summary>
	public class ShardChandelierTile : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLighted[Type] = true;
			Main.tileLavaDeath[Type] = true;
			TileID.Sets.DisableSmartCursor[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
			TileObjectData.newTile.AnchorTop = new AnchorData(
				AnchorType.SolidTile | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
			TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
			TileObjectData.newTile.Origin = new Point16(1, 0);
			TileObjectData.addTile(Type);

			DustType = DustID.Glass;
			AddMapEntry(new Color(170, 120, 205), CreateMapEntryName());
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
		{
			r = 0.50f;
			g = 0.20f;
			b = 0.66f;
		}
	}
}
