using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Terrapex.Common.Players
{
	/// <summary>
	/// Draws the First Keeper set's cone.
	///
	/// The rule the whole mod follows is that a tell must be drawn rather than implied, and it
	/// applies to the player's own tools as much as to a boss's attacks: a set bonus that says
	/// "things you look at take more damage" is bookkeeping unless the player can see where the
	/// looking stops. Two edges, a faint fill, and a mark on every enemy currently inside it —
	/// which also means the summoner head's minion targeting is visible instead of mysterious.
	///
	/// Only ever drawn for the local player. Everyone else's cursor is not knowable here, and a
	/// cone drawn off a stale angle is worse than none.
	/// </summary>
	public class RegardLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.HeldItem);

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
			=> drawInfo.drawPlayer.whoAmI == Main.myPlayer
			&& drawInfo.drawPlayer.GetModPlayer<TerrapexPlayer>().firstSet;

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			Player player = drawInfo.drawPlayer;
			TerrapexPlayer mp = player.GetModPlayer<TerrapexPlayer>();
			Vector2 from = player.MountedCenter;

			if (mp.firstMagic)
			{
				// the magic head reads a disc at the cursor, so that is what gets drawn — the
				// two heads must not share a shape or the player cannot tell which is on
				RiftDraw.Ring(mp.RegardCursor, RiftDraw.Glow(225, 232, 250, 0.30f),
					TerrapexPlayer.RegardDisc / 32f * 0.55f, Main.GlobalTimeWrappedHourly * 0.4f);
				RiftDraw.Bloom(mp.RegardCursor, RiftDraw.Glow(255, 255, 255, 0.22f), 0.7f);
			}
			else
			{
				float half = mp.RegardHalf;
				float reach = mp.RegardRange;
				float aim = mp.RegardAim;

				for (int k = -1; k <= 1; k += 2)
					RiftDraw.Line(from, from + (aim + k * half).ToRotationVector2() * reach,
						RiftDraw.Glow(225, 232, 250, 0.16f), 1.8f);

				const int Spokes = 6;
				for (int i = 1; i < Spokes; i++)
				{
					float a = aim - half + i * (half * 2f / Spokes);
					RiftDraw.Line(from, from + a.ToRotationVector2() * reach,
						RiftDraw.Glow(200, 208, 235, 0.05f), 1.2f);
				}
			}

			// the marks. A cone with nothing lit inside it does not say which things it caught,
			// and that is the only question the player actually has.
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!mp.Regarded(n))
					continue;
				RiftDraw.Ring(n.Center, RiftDraw.Glow(255, 255, 255, 0.30f),
					0.5f + n.width / 90f, Main.GlobalTimeWrappedHourly * 1.1f);
				RiftDraw.Bloom(n.Center, RiftDraw.Glow(255, 240, 210, 0.18f), 0.55f);
			}
		}
	}
}
