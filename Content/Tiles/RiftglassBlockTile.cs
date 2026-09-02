using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Tiles
{
	/// <summary>Plan item #34's tile. Glass that keeps a little of the rift's light.</summary>
	public class RiftglassBlockTile : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileSolid[Type] = true;
			Main.tileBlockLight[Type] = false;   // it is glass: light goes through
			Main.tileLighted[Type] = true;
			Main.tileMergeDirt[Type] = false;

			MinPick = 0;
			DustType = DustID.Glass;
			HitSound = SoundID.Shatter;

			AddMapEntry(new Color(150, 110, 190), CreateMapEntryName());
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
		{
			r = 0.16f;
			g = 0.05f;
			b = 0.22f;
		}
	}
}
