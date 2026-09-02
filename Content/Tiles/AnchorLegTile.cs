using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Terrapex.Content.Tiles
{
	/// <summary>
	/// The Weaver's leg, planted. Every T5 recipe is worked on it, and it also carries the
	/// Rift Altar's adjacency so the tier does not force the player to keep both.
	/// </summary>
	public class AnchorLegTile : ModTile
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

			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
			TileObjectData.newTile.Origin = new Point16(1, 2);
			TileObjectData.addTile(Type);

			DustType = DustID.Vortex;
			AdjTiles = new int[] { TileID.WorkBenches, ModContent.TileType<RiftAltarTile>() };
			AddMapEntry(new Color(53, 201, 184), CreateMapEntryName());
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
		{
			r = 0.10f;
			g = 0.30f;
			b = 0.28f;
		}

		public override void NearbyEffects(int i, int j, bool closer)
		{
			if (!closer || !Main.rand.NextBool(70))
				return;
			Dust d = Dust.NewDustPerfect(new Vector2(i * 16 + 8, j * 16 + 4),
				DustID.Vortex, new Vector2(0f, -Main.rand.NextFloat(0.3f, 0.9f)), 130, default, 0.8f);
			d.noGravity = true;
		}
	}
}
