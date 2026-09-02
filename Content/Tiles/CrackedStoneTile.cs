using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Tiles
{
	public class CrackedStoneTile : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileSolid[Type] = true;
			Main.tileBlockLight[Type] = true;
			Main.tileMergeDirt[Type] = true;

			MinPick = 0;
			DustType = DustID.Stone;
			HitSound = SoundID.Tink;

			AddMapEntry(new Color(104, 98, 116), CreateMapEntryName());
		}
	}
}
