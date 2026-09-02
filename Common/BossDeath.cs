using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;

namespace Terrapex.Common
{
	/// <summary>
	/// The throes every boss in this mod dies through.
	///
	/// Vanilla's default is that a boss simply stops existing on the frame its health hits
	/// zero, which throws away the one moment the whole fight was building to. This holds the
	/// NPC alive at one hit point, takes its attacks away, and spends two and a half seconds
	/// coming apart: the body slows to a stop, the dust thickens, the light swells, and the
	/// screen gets three escalating shakes before the final burst.
	///
	/// It is shared rather than copied four times because the *timing* is the part that has to
	/// match — four bosses whose deaths land on different rhythms read as four different mods.
	/// What each boss supplies is its own dust and its own colour, which is enough to tell them
	/// apart without letting them drift.
	/// </summary>
	public class BossDeath
	{
		public const int DefaultTicks = 150;

		/// <summary>-1 before it starts, counting down during, 0 once the throes are spent.</summary>
		private int timer = -1;
		private int ticks = DefaultTicks;

		public bool Dying => timer > 0;

		/// <summary>0 at the first frame of the throes, 1 at the last. For draw code.</summary>
		public float Progress => timer <= 0 ? 1f : 1f - timer / (float)ticks;

		/// <summary>
		/// Call from <c>ModNPC.CheckDead</c>. Returns false the first time, which keeps the boss
		/// alive, and true only once the throes have actually run out.
		///
		/// <paramref name="onBegin"/> is the place to drop adds and standing hazards — a web or
		/// a shell left up during the death would keep killing the player through the one part
		/// of the fight that is supposed to be over.
		/// </summary>
		public bool CheckDead(NPC npc, int deathTicks = DefaultTicks, Action onBegin = null)
		{
			if (timer == 0)
				return true;
			if (timer < 0)
			{
				ticks = deathTicks;
				timer = deathTicks;
				npc.life = 1;
				npc.dontTakeDamage = true;
				npc.velocity *= 0.25f;
				npc.netUpdate = true;
				onBegin?.Invoke();
			}
			return false;
		}

		/// <summary>
		/// Call at the top of <c>AI</c> while <see cref="Dying"/>, then return. True on the frame
		/// the boss actually dies, after which the NPC may already be inactive — do not touch it.
		/// </summary>
		public bool Tick(NPC npc, int dust, Color light)
		{
			float f = Progress;

			npc.velocity *= 0.93f;
			npc.rotation += npc.velocity.X * 0.01f;
			Lighting.AddLight(npc.Center, light.R / 255f * (0.6f + f), light.G / 255f * (0.6f + f),
				light.B / 255f * (0.6f + f));

			// the spray thickens as it goes, so the death has a direction instead of a level hum
			int count = 2 + (int)(f * 10f);
			for (int i = 0; i < count; i++)
			{
				Dust d = Dust.NewDustDirect(npc.position, npc.width, npc.height, dust,
					Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 4f), 90, default,
					1f + f * 1.4f);
				d.noGravity = true;
			}

			// three shakes on the way down, each harder than the last
			if (timer == ticks - 4 || timer == ticks / 2 || timer == 24)
			{
				SoundEngine.PlaySound(SoundID.NPCDeath14 with { Pitch = -0.4f + f * 0.5f }, npc.Center);
				Shake(npc, 6f + f * 10f, 18);
			}

			if (--timer > 0)
				return false;

			// the burst, then the actual death
			SoundEngine.PlaySound(SoundID.Item14, npc.Center);
			Shake(npc, 18f, 40);
			for (int i = 0; i < 90; i++)
			{
				Dust d = Dust.NewDustPerfect(npc.Center, dust,
					Main.rand.NextVector2Circular(11f, 11f), 80, default, 2f);
				d.noGravity = true;
			}
			npc.life = 0;
			npc.checkDead();
			return true;
		}

		private static void Shake(NPC npc, float strength, int frames)
		{
			if (Main.netMode == NetmodeID.Server)
				return;
			Main.instance.CameraModifiers.Add(new PunchCameraModifier(npc.Center,
				Main.rand.NextVector2Unit(), strength, 6f, frames, 2400f, "BossDeath"));
		}
	}
}
