using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Particles;

namespace Terrapex.Content.Items.Weapons.Trees.Magic
{
	/// <summary>
	/// The page the book is open at belongs to the player, not to the item, for the same reason
	/// the T4 stitch does: crafting the next book up, or swapping to another weapon and back,
	/// keeps the page you were on.
	///
	/// Only the owner's copy decides anything - every cast reads it on the client that fires -
	/// so it is not synced. Other clients draw the Cosmogony's orbit from their own clock, and
	/// the indicator is drawn for the local player only.
	/// </summary>
	public class AlmanacPlayer : ModPlayer
	{
		public const int CastsPerPage = 4;
		public const int CloseTicks = 22;

		public int page;
		public int casts;

		/// <summary>Counts down after a page turn; the glyph pops while it runs.</summary>
		public int turnFlash;

		/// <summary>Counts down after a Cosmogony cast: the pages slam into the book and reopen.</summary>
		public int closeTimer;

		/// <summary>0..1 while a held book is charging - set by <c>AlmanacCharge</c> every tick.</summary>
		public float charge;

		public float orbit;

		public void TurnPage(Player p, int step)
		{
			page = (page + step + AlmanacItem.Pages) % AlmanacItem.Pages;
			casts = 0;
			turnFlash = 14;
			SoundEngine.PlaySound(SoundID.Item1 with { Pitch = 0.6f, Volume = 0.6f }, p.Center);
			AlmanacFX.TurnPage(p.Top + new Vector2(0f, -18f), AlmanacItem.PageColor[page]);
		}

		public void Close() => closeTimer = CloseTicks;

		/// <summary>
		/// How far the pages stand out from the book. During a close they are slammed in over the
		/// first third and open again over the rest - the shut is fast, the reopening slow.
		/// </summary>
		public float OpenAmount
		{
			get
			{
				if (closeTimer <= 0)
					return 1f;
				float t = 1f - closeTimer / (float)CloseTicks;
				return t < 0.3f ? 1f - t / 0.3f : (float)Math.Sin((t - 0.3f) / 0.7f * MathHelper.PiOver2);
			}
		}

		/// <summary>Offset from the player's centre to page <paramref name="k"/> of the Cosmogony's ring.</summary>
		public Vector2 OrbitOffset(int k)
		{
			float a = orbit + k * MathHelper.TwoPi / AlmanacItem.Pages;
			float r = 50f * MathHelper.Lerp(0.12f, 1f, OpenAmount);
			// a flattened ring, so the pages read as standing around the player rather than above
			return new Vector2((float)Math.Cos(a) * r, (float)Math.Sin(a) * r * 0.62f - 6f);
		}

		public override void PostUpdate()
		{
			if (turnFlash > 0)
				turnFlash--;
			if (closeTimer > 0)
				closeTimer--;
			// a held charge winds the ring up, so the charge is visible on the Cosmogony too
			orbit += 0.032f + 0.10f * charge;
			if (orbit > MathHelper.TwoPi)
				orbit -= MathHelper.TwoPi;
			charge = Math.Max(0f, charge - 0.1f);
		}
	}
}
