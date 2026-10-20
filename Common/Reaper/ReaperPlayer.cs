using System;
using Terraria;
using Terraria.ID;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terrapex.Content.Items.Weapons.Reaper;

namespace Terrapex.Common.Reaper
{
	/// <summary>
	/// The reaper's state: the Harvest meter, how much of the blade counts as edge, and the
	/// Ronin set's stance. Numbers are in <c>CLASS_REAPER.md</c> and must stay in step with it.
	///
	/// Harvest is only ever read by its own client - the bar is drawn for the local player and the
	/// Reap is spent through an ordinary item use, which syncs itself - so nothing here is sent.
	/// </summary>
	public class ReaperPlayer : ModPlayer
	{
		public const float HarvestMax = 100f;
		public const float HarvestPerHit = 10f;
		public const float HarvestPerEdgeHit = 20f;
		/// <summary>A strike at range fills the meter too (double inside the edge band), so a reaper who keeps their distance still Reaps.</summary>
		public const float HarvestPerStrikeHit = 6f;
		/// <summary>Ticks without a hit before the meter starts to drain.</summary>
		public const int HarvestHold = 240;
		public const float HarvestDrainPerSecond = 10f;

		/// <summary>The outer share of the blade whose hits always crit.</summary>
		public const float BaseEdgeShare = 0.35f;

		/// <summary>Ronin set: ticks without attacking before the next swing is all edge.</summary>
		public const int IaidoTicks = 90;

		public float harvest;
		private int harvestIdle;

		public float edgeShare;
		public float harvestGain;

		/// <summary>Multiplies every scythe's reach (Long Haft).</summary>
		public float rangeMult;
		/// <summary>Multiplies a Reap's damage (Soul Lantern).</summary>
		public float reapDamage;
		/// <summary>Share of the meter a Reap leaves behind (Soul Lantern).</summary>
		public float reapRefund;
		/// <summary>Multiplies how long the meter holds before draining (Scarecrow set).</summary>
		public float holdMult;

		public bool scarecrowSet;
		public bool crowCharm;

		public bool roninSet;
		private int stillTicks;

		/// <summary>Which way the last swing went; the next one goes the other way.</summary>
		public bool swingFlip;

		public bool HarvestFull => harvest >= HarvestMax;

		/// <summary>Ronin set: the blade is sheathed and the next swing will be all edge.</summary>
		public bool IaidoReady => roninSet && stillTicks >= IaidoTicks;

		public override void ResetEffects()
		{
			edgeShare = BaseEdgeShare;
			harvestGain = 1f;
			rangeMult = 1f;
			reapDamage = 1f;
			reapRefund = 0f;
			holdMult = 1f;
			roninSet = false;
			scarecrowSet = false;
			crowCharm = false;
		}

		public void AddHarvest(float amount)
		{
			harvest = Math.Min(HarvestMax, harvest + amount * harvestGain);
			harvestIdle = (int)(HarvestHold * holdMult);
		}

		public void SpendHarvest() => harvest = HarvestMax * reapRefund;

		/// <summary>Called by a swing as it starts, so the stance is used once.</summary>
		public void DrawBlade() => stillTicks = 0;

		/// <summary>
		/// The Reap is on the right button at any moment a scythe is in hand and the meter is full -
		/// including halfway through a swing with the left button held. It is read here rather than
		/// through the item's alt use, which Terraria will not start while another use is running.
		/// Right-clicking a chest or a door with a full meter will Reap as well; the meter is the
		/// thing you are saving, so that is the cost of keeping it on the mouse.
		/// </summary>
		public override void ProcessTriggers(TriggersSet triggersSet)
		{
			if (!Main.mouseRight || !Main.mouseRightRelease || Player.mouseInterface || Main.drawingPlayerChat)
				return;
			if (Player.dead || Player.CCed || Player.noItems || !HarvestFull)
				return;
			if (Player.HeldItem.ModItem is ScytheItem scythe)
				scythe.TryReap(Player);
		}

		public override void PostUpdate()
		{
			if (harvestIdle > 0)
				harvestIdle--;
			else if (harvest > 0f)
				harvest = Math.Max(0f, harvest - HarvestDrainPerSecond / 60f);

			if (Player.itemAnimation > 0)
				stillTicks = 0;
			else if (stillTicks < IaidoTicks)
			{
				stillTicks++;
				if (stillTicks == IaidoTicks && roninSet && Player.whoAmI == Main.myPlayer)
					IaidoFlash();
			}
		}

		/// <summary>The stance landing is drawn, not implied: a ring of bone-white sparks.</summary>
		private void IaidoFlash()
		{
			for (int i = 0; i < 14; i++)
			{
				Dust d = Dust.NewDustPerfect(Player.Center, DustID.GoldFlame,
					Microsoft.Xna.Framework.Vector2.UnitX.RotatedBy(i / 14f * MathF.Tau) * 2.4f, 90, default, 0.9f);
				d.noGravity = true;
			}
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Item35 with { Volume = 0.5f, Pitch = 0.4f }, Player.Center);
		}
	}
}
