using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;
using Terrapex.Content.Items.Weapons.Reaper;
using Terrapex.Content.Projectiles.Reaper;

namespace Terrapex.Content.Items.Accessories.Reaper
{
	/// <summary>
	/// The three biome-boss reaper accessories: the Barnacled Hone, the Scarab Amulet and the
	/// Rimebound Wrap. Hits are read from the outside - the blade is <see cref="ScytheSwing"/> in
	/// its swing modes, a strike is <see cref="ReapingSlash"/> - and a Reap is the Harvest meter
	/// falling from full in one tick. Everything runs on the owner's client, which is the only one
	/// that deals its hits; the marks live on <see cref="BiomeRelicGlobalNPC"/>, whose copy on each
	/// client only ever holds that client's own player's marks.
	/// </summary>
	public class BiomeRelicPlayer : ModPlayer
	{
		public bool hone;
		public bool scarab;
		public bool wrap;

		/// <summary>Scarab Amulet: ticks of sunrise left after the last Reap.</summary>
		public int sunrise;
		private float prevHarvest;

		public override void ResetEffects()
		{
			hone = false;
			scarab = false;
			wrap = false;
		}

		public override void PostUpdateEquips()
		{
			if (!scarab)
			{
				sunrise = 0;
				return;
			}
			if (sunrise > 0)
			{
				ReaperPlayer reaper = Player.GetModPlayer<ReaperPlayer>();
				reaper.rangeMult += ScarabAmulet.RangeBonus;
				reaper.edgeShare += ScarabAmulet.EdgeBonus;
			}
		}

		public override void PostUpdate()
		{
			if (sunrise > 0)
			{
				sunrise--;
				if (Main.rand.NextBool(3))
					Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.GoldFlame, 0f, -1.5f, 100, default, 0.8f).noGravity = true;
			}
			if (Player.whoAmI != Main.myPlayer)
				return;

			ReaperPlayer r = Player.GetModPlayer<ReaperPlayer>();
			bool reaped = prevHarvest >= ReaperPlayer.HarvestMax && r.harvest < prevHarvest - 40f;
			prevHarvest = r.harvest;
			if (reaped && scarab && Player.HeldItem.ModItem is ScytheItem scythe)
				Sunrise(scythe);
		}

		private void Sunrise(ScytheItem scythe)
		{
			sunrise = ScarabAmulet.SunriseTicks;
			// the ring runs out to the rim the reach is about to have
			float rim = scythe.RangePixels * (Player.GetModPlayer<ReaperPlayer>().rangeMult + ScarabAmulet.RangeBonus);
			for (int i = 0; i < 36; i++)
			{
				Vector2 dir = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 36f);
				Dust d = Dust.NewDustPerfect(Player.MountedCenter, DustID.GoldFlame, dir * rim / 24f, 60, default, 1.3f);
				d.noGravity = true;
			}
			SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.6f, Pitch = 0.3f }, Player.Center);
		}

		private static bool IsBlade(Projectile p)
			=> p.type == ModContent.ProjectileType<ScytheSwing>() && p.ai[2] != ScytheSwing.ModeReap;

		private static bool IsStrike(Projectile p) => p.type == ModContent.ProjectileType<ReapingSlash>();

		/// <summary>
		/// A hit by the haft: an ordinary swing whose target sits wholly inside the part of the
		/// stroke that is not edge. Iaido strokes are all edge by definition, so never haft.
		/// </summary>
		private bool IsHaft(Projectile p, NPC target)
		{
			if (p.type != ModContent.ProjectileType<ScytheSwing>() || p.ai[2] != ScytheSwing.ModeSwing)
				return false;
			float inner = p.ai[1] * (1f - Player.GetModPlayer<ReaperPlayer>().edgeShare);
			Vector2 c = Player.MountedCenter;
			Rectangle b = target.Hitbox;
			float far = 0f;
			far = Math.Max(far, Vector2.Distance(c, new Vector2(b.Left, b.Top)));
			far = Math.Max(far, Vector2.Distance(c, new Vector2(b.Right, b.Top)));
			far = Math.Max(far, Vector2.Distance(c, new Vector2(b.Left, b.Bottom)));
			far = Math.Max(far, Vector2.Distance(c, new Vector2(b.Right, b.Bottom)));
			return far < inner;
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (Player.whoAmI != Main.myPlayer)
				return;
			BiomeRelicGlobalNPC mark = target.GetGlobalNPC<BiomeRelicGlobalNPC>();
			mark.popPending = false;
			mark.shatterPending = false;
			mark.haftPending = false;

			if (hone && IsStrike(proj) && mark.barnacles > 0)
			{
				modifiers.SourceDamage *= 1f + BarnacledHone.PerBarnacle * mark.barnacles;
				mark.popPending = true;
			}

			if (wrap)
			{
				bool haft = IsHaft(proj, target);
				if (haft)
				{
					mark.haftPending = true;
					if (!target.boss)
						modifiers.Knockback *= RimeboundWrap.KnockbackMult;
				}
				else if (mark.frost > 0 && (IsStrike(proj) || IsBlade(proj)))
				{
					modifiers.SourceDamage *= 1f + RimeboundWrap.ShatterBonus;
					mark.shatterPending = true;
				}
			}
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Player.whoAmI != Main.myPlayer)
				return;
			BiomeRelicGlobalNPC mark = target.GetGlobalNPC<BiomeRelicGlobalNPC>();

			if (mark.popPending)
			{
				mark.popPending = false;
				if (!target.immortal)
					Player.GetModPlayer<ReaperPlayer>().AddHarvest(BarnacledHone.HarvestPerBarnacle * mark.barnacles);
				for (int i = 0; i < mark.barnacles * 3; i++)
					Dust.NewDustPerfect(target.Top, DustID.Pearlsand, Main.rand.NextVector2Circular(3.5f, 3.5f) - Vector2.UnitY * 2f, 0, default, 1.1f);
				SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.6f, Pitch = 0.4f }, target.Center);
				mark.barnacles = 0;
				mark.crust = 0;
			}
			else if (hone && IsBlade(proj) && hit.Crit && mark.barnacles < BarnacledHone.MaxBarnacles)
			{
				mark.barnacles++;
				mark.crust = BarnacledHone.CrustTicks;
			}

			if (mark.haftPending)
			{
				mark.haftPending = false;
				mark.frost = RimeboundWrap.FrostTicks;
				target.AddBuff(BuffID.Frostburn, RimeboundWrap.FrostTicks);
				for (int i = 0; i < 8; i++)
					Dust.NewDustPerfect(target.Center, DustID.Ice, Main.rand.NextVector2Circular(2.5f, 2.5f), 80, default, 1f).noGravity = true;
			}
			else if (mark.shatterPending)
			{
				mark.shatterPending = false;
				mark.frost = 0;
				SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.7f }, target.Center);
				for (int i = 0; i < 14; i++)
					Dust.NewDustPerfect(target.Center, DustID.Ice, Main.rand.NextVector2Circular(5f, 5f), 60, default, 1.3f);
			}
		}
	}

	/// <summary>
	/// The marks the accessories leave, per NPC: barnacles (Hone) and frost (Wrap). Each client's
	/// copy holds only its own player's marks, and they are drawn only there - the reaper who put
	/// them on is the one who needs to see them.
	/// </summary>
	public class BiomeRelicGlobalNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public int barnacles;
		public int crust;
		public int frost;

		public bool popPending;
		public bool shatterPending;
		public bool haftPending;

		public override void PostAI(NPC npc)
		{
			if (crust > 0 && --crust == 0)
				barnacles = 0;
			if (frost > 0)
			{
				frost--;
				if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(6))
				{
					Dust d = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Ice, 0f, 0f, 120, default, 0.8f);
					d.noGravity = true;
					d.velocity *= 0.3f;
				}
			}
		}

		public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			if (barnacles <= 0 && frost <= 0)
				return;
			Texture2D px = TextureAssets.MagicPixel.Value;
			Rectangle one = new(0, 0, 1, 1);
			Vector2 top = npc.Top - screenPos + new Vector2(0f, npc.gfxOffY - 10f);

			// barnacles: a row of bone-white nubs over the head, one per barnacle
			for (int i = 0; i < barnacles; i++)
			{
				Vector2 at = top + new Vector2((i - (barnacles - 1) * 0.5f) * 6f, 0f);
				at = new Vector2((int)at.X, (int)at.Y);
				spriteBatch.Draw(px, at, one, new Color(36, 40, 58), 0f, new Vector2(0.5f), 6f, SpriteEffects.None, 0f);
				spriteBatch.Draw(px, at, one, new Color(246, 242, 228), 0f, new Vector2(0.5f), 4f, SpriteEffects.None, 0f);
				spriteBatch.Draw(px, at + new Vector2(1f, 1f), one, new Color(143, 151, 160), 0f, new Vector2(0.5f), 2f, SpriteEffects.None, 0f);
			}

			// frost: a pale-blue diamond over the head that thins as it melts
			if (frost > 0)
			{
				float f = frost / (float)RimeboundWrap.FrostTicks;
				Vector2 at = top + new Vector2(0f, barnacles > 0 ? -8f : 0f);
				at = new Vector2((int)at.X, (int)at.Y);
				spriteBatch.Draw(px, at, one, new Color(163, 233, 248) * (0.4f + 0.6f * f), MathHelper.PiOver4,
					new Vector2(0.5f), 6f, SpriteEffects.None, 0f);
				spriteBatch.Draw(px, at, one, new Color(234, 252, 255) * f, MathHelper.PiOver4,
					new Vector2(0.5f), 3f, SpriteEffects.None, 0f);
			}
		}
	}
}
