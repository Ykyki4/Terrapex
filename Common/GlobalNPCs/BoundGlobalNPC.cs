using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Common.GlobalNPCs
{
	/// <summary>
	/// T4's whole mechanic. Two enemies are stitched together and a share of every hit runs
	/// down the thread to the other one.
	///
	/// The tell is the thread itself, drawn sagging between the pair, rather than a debuff
	/// icon: the tier is about seeing the seam you made, and an icon in the corner of the
	/// screen would say nothing about *which* two things are joined.
	/// </summary>
	public class BoundGlobalNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		/// <summary>Share of a hit that is passed to the other end.</summary>
		public const float Echo = 0.45f;
		public const int Duration = 60 * 8;
		private const int Segments = 8;

		/// <summary>Index into <c>Main.npc</c>, or -1.</summary>
		public int partner = -1;

		public int Partner => partner;
		public int timer;

		/// <summary>Counts down while a bead runs the thread, right after an echo.</summary>
		public int pulse;

		/// <summary>One hop only. Without this two bound targets echo into each other forever.</summary>
		private static bool echoing;

		public bool IsBound => timer > 0 && partner >= 0 && partner < Main.maxNPCs
			&& Main.npc[partner].active;

		/// <summary>Stitches two targets together. A thread is always a pair, never a chain.</summary>
		public static void Bind(NPC a, NPC b, int ticks = Duration)
		{
			if (a == null || b == null || !a.active || !b.active || a.whoAmI == b.whoAmI)
				return;
			if (a.friendly || b.friendly || a.townNPC || b.townNPC)
				return;

			BoundGlobalNPC ga = a.GetGlobalNPC<BoundGlobalNPC>();
			BoundGlobalNPC gb = b.GetGlobalNPC<BoundGlobalNPC>();
			Cut(ga.partner);
			Cut(gb.partner);

			ga.partner = b.whoAmI;
			ga.timer = ticks;
			gb.partner = a.whoAmI;
			gb.timer = ticks;

			Terraria.Audio.SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.45f, Pitch = 0.5f }, a.Center);
			for (int i = 0; i <= 14; i++)
			{
				Vector2 at = Vector2.Lerp(a.Center, b.Center, i / 14f);
				Dust d = Dust.NewDustPerfect(at, DustID.Vortex, Vector2.Zero, 120, default, 0.9f);
				d.noGravity = true;
				d.velocity *= 0.2f;
			}
		}

		/// <summary>
		/// Pulls a stitched pair into each other and burns the thread doing it.
		///
		/// This is the Seam's third beat: stitch two things, then hit either one again to
		/// slam them together. It is what turns the tier's mechanic from bookkeeping into
		/// something you aim, and it deliberately cuts the thread afterwards so the loop is
		/// always stitch, stitch, pull rather than one stitch and free damage forever.
		/// </summary>
		public static void Yank(NPC a, NPC b, int damage)
		{
			if (a == null || b == null || !a.active || !b.active)
				return;

			Vector2 mid = (a.Center + b.Center) * 0.5f;
			Pull(a, mid);
			Pull(b, mid);

			echoing = true;
			a.SimpleStrikeNPC(damage, 0);
			b.SimpleStrikeNPC(damage, 0);
			echoing = false;

			Cut(a.GetGlobalNPC<BoundGlobalNPC>().partner);
			Cut(a.whoAmI);

			Terraria.Audio.SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.6f }, mid);
			for (int i = 0; i < 22; i++)
			{
				Dust d = Dust.NewDustPerfect(mid, DustID.Vortex,
					Main.rand.NextVector2Circular(5.5f, 5.5f), 90, default, 1.2f);
				d.noGravity = true;
			}
		}

		private static void Pull(NPC npc, Vector2 toward)
		{
			// heavier things move less, so this never yanks a boss across the arena
			float give = 1f - MathHelper.Clamp(npc.knockBackResist <= 0f ? 1f : 0f, 0f, 1f);
			Vector2 dir = toward - npc.Center;
			if (dir.LengthSquared() < 1f)
				return;
			npc.velocity += Vector2.Normalize(dir) * 9f * MathHelper.Max(give, npc.knockBackResist);
			npc.netUpdate = true;
		}

		private static void Cut(int index)
		{
			if (index < 0 || index >= Main.maxNPCs)
				return;
			NPC n = Main.npc[index];
			if (!n.active)
				return;
			BoundGlobalNPC g = n.GetGlobalNPC<BoundGlobalNPC>();
			g.partner = -1;
			g.timer = 0;
		}

		public override void PostAI(NPC npc)
		{
			if (pulse > 0)
				pulse--;
			if (timer <= 0)
				return;

			timer--;
			// the other end may have died, been despawned, or had its slot reused
			if (!IsBound)
			{
				partner = -1;
				timer = 0;
			}
		}

		public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
			=> Pass(npc, damageDone);

		public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
			=> Pass(npc, damageDone);

		private void Pass(NPC npc, int damageDone)
		{
			if (echoing || !IsBound || damageDone <= 0)
				return;

			NPC other = Main.npc[partner];
			echoing = true;
			other.SimpleStrikeNPC((int)(damageDone * Echo), 0);
			echoing = false;

			pulse = 18;
			BoundGlobalNPC g = other.GetGlobalNPC<BoundGlobalNPC>();
			g.pulse = 18;
		}

		public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			// only the lower index draws, or every thread is drawn twice
			if (!IsBound || npc.whoAmI > partner)
				return;

			NPC other = Main.npc[partner];
			Vector2 a = npc.Center - Main.screenPosition;
			Vector2 b = other.Center - Main.screenPosition;
			float len = Vector2.Distance(a, b);
			if (len < 4f)
				return;

			Texture2D px = TextureAssets.MagicPixel.Value;
			Rectangle src = new Rectangle(0, 0, 1, 1);

			// fades in as it is drawn and out as it lapses, so it never pops
			float life = Math.Min(1f, timer / 40f) * Math.Min(1f, (Duration - timer) / 12f);
			float beat = 1f + 0.25f * (float)Math.Sin(Main.GameUpdateCount * 0.15f);

			// a straight line between two enemies reads as a laser, not as thread. The sag
			// is what makes it cloth.
			float sag = MathHelper.Clamp(len * 0.12f, 4f, 42f);
			Vector2 prev = a;
			for (int i = 1; i <= Segments; i++)
			{
				float t = i / (float)Segments;
				Vector2 p = Vector2.Lerp(a, b, t);
				p.Y += (float)Math.Sin(t * MathHelper.Pi) * sag;

				Vector2 seg = p - prev;
				float rot = seg.ToRotation();
				float run = seg.Length() + 1f;

				// alpha 0 in the default premultiplied batch adds light
				spriteBatch.Draw(px, prev, src, new Color(18, 124, 120, 0) * (0.55f * life), rot,
					new Vector2(0f, 0.5f), new Vector2(run, 5f * beat), SpriteEffects.None, 0f);
				spriteBatch.Draw(px, prev, src, new Color(53, 201, 184, 0) * (0.85f * life), rot,
					new Vector2(0f, 0.5f), new Vector2(run, 2f), SpriteEffects.None, 0f);
				prev = p;
			}

			if (pulse <= 0)
				return;

			// a bead running the thread, so an echo is visibly the thread's doing
			float at = 1f - pulse / 18f;
			Vector2 bead = Vector2.Lerp(a, b, at);
			bead.Y += (float)Math.Sin(at * MathHelper.Pi) * sag;
			Texture2D flare = ModContent.Request<Texture2D>("Terrapex/Content/Projectiles/RiftFlare").Value;
			spriteBatch.Draw(flare, bead, null, new Color(216, 255, 247, 0) * (pulse / 18f), 0f,
				flare.Size() * 0.5f, 0.22f, SpriteEffects.None, 0f);
		}
	}
}
