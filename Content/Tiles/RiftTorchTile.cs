using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Terrapex.Content.Tiles
{
	// RiftTorchTile.png is 132x22: six 20x20 frames on a 22 px pitch.
	//   frameX 0 / 22 / 44 — lit, placed on the floor / left wall / right wall
	//   frameX 66 and up   — the unlit copies Terraria swaps to when a torch is off
	// _Flame.png matches frame for frame and is drawn additively on top in PostDraw.
	public class RiftTorchTile : ModTile
	{
		private Asset<Texture2D> flameTexture;

		public override void SetStaticDefaults()
		{
			Main.tileLighted[Type] = true;
			Main.tileFrameImportant[Type] = true;
			Main.tileSolid[Type] = false;
			Main.tileNoAttach[Type] = true;
			Main.tileNoFail[Type] = true;
			Main.tileWaterDeath[Type] = false;   // the crack does not care about water
			Main.tileLavaDeath[Type] = false;

			TileID.Sets.FramesOnKillWall[Type] = true;
			TileID.Sets.DisableSmartCursor[Type] = true;
			TileID.Sets.DisableSmartInteract[Type] = true;
			TileID.Sets.Torch[Type] = true;
			AddToArray(ref TileID.Sets.RoomNeeds.CountsAsTorch);

			DustType = DustID.PurpleTorch;
			AdjTiles = new int[] { TileID.Torches };
			VanillaFallbackOnModDeletion = TileID.Torches;

			// floor + both walls + on a background wall, exactly as vanilla torches
			TileObjectData.newTile.CopyFrom(TileObjectData.GetTileData(TileID.Torches, 0));
			TileObjectData.newTile.WaterDeath = false;
			TileObjectData.newTile.LavaDeath = false;
			TileObjectData.newTile.WaterPlacement = LiquidPlacement.Allowed;
			TileObjectData.newTile.LavaPlacement = LiquidPlacement.Allowed;
			TileObjectData.addTile(Type);

			AddMapEntry(new Color(186, 128, 224), CreateMapEntryName());

			flameTexture = ModContent.Request<Texture2D>(Texture + "_Flame");
		}

		public override void NumDust(int i, int j, bool fail, ref int num) => num = Main.rand.Next(1, 3);

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
		{
			if (Main.tile[i, j].TileFrameX < 66)
			{
				r = 0.72f;
				g = 0.34f;
				b = 0.95f;
			}
		}

		public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height,
			ref short tileFrameX, ref short tileFrameY)
		{
			offsetY = WorldGen.SolidTile(i, j - 1) ? 4 : 0;
		}

		public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
		{
			Tile tile = Main.tile[i, j];
			if (!TileDrawing.IsVisible(tile) || tile.TileFrameX >= 66)
				return;

			int offsetY = WorldGen.SolidTile(i, j - 1) ? 4 : 0;
			Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
			ulong seed = Main.TileFrameSeed ^ (ulong)((long)j << 32 | (long)(uint)i);
			Color color = new Color(100, 100, 100, 0);

			for (int k = 0; k < 7; k++)
			{
				float xx = Utils.RandomInt(ref seed, -10, 11) * 0.15f;
				float yy = Utils.RandomInt(ref seed, -10, 1) * 0.35f;
				spriteBatch.Draw(flameTexture.Value,
					new Vector2(i * 16 - (int)Main.screenPosition.X - 2f + xx,
						j * 16 - (int)Main.screenPosition.Y + offsetY + yy) + zero,
					new Rectangle(tile.TileFrameX, tile.TileFrameY, 20, 20),
					color, 0f, default, 1f, SpriteEffects.None, 0f);
			}
		}

		public override void EmitParticles(int i, int j, Tile tileCache, short tileFrameX, short tileFrameY,
			Color tileLight, bool visible)
		{
			if (!visible || tileFrameX >= 66 || !Main.rand.NextBool(40))
				return;

			Dust d = Dust.NewDustDirect(new Vector2(i * 16 + 4, j * 16), 4, 4, DustID.PurpleTorch, 0f, 0f, 100);
			if (!Main.rand.NextBool(3))
				d.noGravity = true;
			d.velocity *= 0.3f;
			d.velocity.Y -= 1.5f;
		}
	}
}
