using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Tiles
{
	public class FissuriteOreTile : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileSolid[Type] = true;
			Main.tileBlockLight[Type] = true;
			Main.tileLighted[Type] = true;
			Main.tileMergeDirt[Type] = true;
			Main.tileShine[Type] = 900;
			Main.tileShine2[Type] = true;
			Main.tileOreFinderPriority[Type] = 320;
			Main.tileSpelunker[Type] = true;

			TileID.Sets.Ore[Type] = true;
			TileID.Sets.OreMergesWithMud[Type] = true;

			// Silver/tungsten and up. The vein sits just below the iron band, so an
			// early copper pick should still leave the player something to want.
			MinPick = 45;
			DustType = DustID.PurpleTorch;
			HitSound = SoundID.Tink;

			AddMapEntry(new Color(146, 108, 186), CreateMapEntryName());
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
		{
			// barely there — enough to catch the eye down a dark shaft, not enough
			// to light the room for free
			r = 0.11f;
			g = 0.03f;
			b = 0.15f;
		}
	}
}
