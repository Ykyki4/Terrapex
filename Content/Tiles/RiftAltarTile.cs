using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Terrapex.Content.Tiles
{
	/// <summary>
	/// Plan item #51. The mod's own crafting station, and the reason T2 exists as a tier:
	/// from here on every rift recipe lives on this altar instead of dissolving into the
	/// vanilla anvil menu.
	/// </summary>
	public class RiftAltarTile : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileTable[Type] = true;
			Main.tileSolidTop[Type] = true;
			Main.tileLavaDeath[Type] = false;
			Main.tileLighted[Type] = true;
			TileID.Sets.DisableSmartCursor[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16 };
			TileObjectData.newTile.Origin = new Point16(1, 1);
			TileObjectData.addTile(Type);

			DustType = DustID.Stone;
			AdjTiles = new int[] { TileID.WorkBenches };
			AddMapEntry(new Color(146, 96, 186), CreateMapEntryName());
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
		{
			r = 0.22f;
			g = 0.07f;
			b = 0.30f;
		}

		public override void NearbyEffects(int i, int j, bool closer)
		{
			// one mote a second from the top of the slab, so the altar reads as lit
			// rather than as a table with a purple map colour
			if (!closer || !Main.rand.NextBool(90))
				return;

			Dust d = Dust.NewDustPerfect(new Vector2(i * 16 + 8, j * 16 + 4),
				DustID.PurpleTorch, new Vector2(0f, -Main.rand.NextFloat(0.3f, 0.9f)), 140, default, 0.8f);
			d.noGravity = true;
		}
	}
}
