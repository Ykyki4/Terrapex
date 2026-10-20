using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;
using Terrapex.Content.Items.Weapons.Reaper;
using Terrapex.Content.Projectiles.Reaper;
using Terrapex.Content.Projectiles.Scarab;

namespace Terrapex.Content.Items.Armor.Reaper
{
	/// <summary>
	/// The two biome reaper sets' state - Tidewrack's swells and Sunwrought's suns. See
	/// <c>CLASS_REAPER.md</c>, «Броня».
	///
	/// Both bonuses read the class's own signals from the outside: a strike is a hit by
	/// <see cref="ReapingSlash"/> (its ai[0] is 1 inside the edge band), and a Reap is the Harvest
	/// meter falling from full in one tick, which is the only thing that ever empties it at once.
	/// That keeps every number here on this player, on the client that owns it - the meter is only
	/// ever known there, and the projectiles it spawns sync themselves - so two reapers in one
	/// world never share or overwrite a swell.
	/// </summary>
	public class BiomeSetPlayer : ModPlayer
	{
		public bool tidewrackSet;
		public bool sunwroughtSet;

		/// <summary>Tidewrack: swells banked by edge-band strikes, spent by the next Reap.</summary>
		public int swells;
		private int swellIdle;
		private int lastSwellSlash = -1;

		private int sunCooldown;
		private int lastSunSlash = -1;

		private float prevHarvest;

		public override void ResetEffects()
		{
			tidewrackSet = false;
			sunwroughtSet = false;
		}

		public override void PostUpdate()
		{
			if (Player.whoAmI != Main.myPlayer)
				return;

			ReaperPlayer reaper = Player.GetModPlayer<ReaperPlayer>();
			bool reaped = prevHarvest >= ReaperPlayer.HarvestMax && reaper.harvest < prevHarvest - 40f;
			prevHarvest = reaper.harvest;

			if (sunCooldown > 0)
				sunCooldown--;

			if (!tidewrackSet)
			{
				swells = 0;
				return;
			}

			// unspent swells ebb away one at a time once the reaper stops landing edge strikes
			if (swells > 0 && ++swellIdle > TidewrackHood.SwellHold && swellIdle % 60 == 0)
				swells--;

			if (reaped && swells > 0 && Player.HeldItem.ModItem is ScytheItem scythe)
				BreakTide(scythe);
		}

		private void BreakTide(ScytheItem scythe)
		{
			int damage = (int)(Player.GetWeaponDamage(Player.HeldItem) * TidewrackHood.SwellShare * swells);
			float run = scythe.RangeFor(Player);
			Vector2 at = Player.Bottom - new Vector2(0f, 20f);
			IEntitySource src = Player.GetSource_ItemUse(Player.HeldItem);
			for (int dir = -1; dir <= 1; dir += 2)
				Projectile.NewProjectile(src, at, Vector2.Zero, ModContent.ProjectileType<TideBreaker>(),
					damage, TidewrackHood.WaveKnockback, Player.whoAmI, dir, run);
			SoundEngine.PlaySound(SoundID.Item96 with { Volume = 0.7f, Pitch = -0.2f }, Player.Center);
			swells = 0;
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Player.whoAmI != Main.myPlayer || proj.type != ModContent.ProjectileType<ReapingSlash>() || target.immortal)
				return;

			// Tidewrack: one swell per strike in the edge band, however many it cut
			if (tidewrackSet && proj.ai[0] == 1f && proj.identity != lastSwellSlash)
			{
				lastSwellSlash = proj.identity;
				swellIdle = 0;
				if (swells < TidewrackHood.MaxSwells)
				{
					swells++;
					SoundEngine.PlaySound(SoundID.Drip with { Volume = 0.8f, Pitch = 0.2f * swells }, Player.Center);
					for (int i = 0; i < 6; i++)
						Dust.NewDustPerfect(Player.Center, DustID.Water, Main.rand.NextVector2Circular(2.5f, 2.5f), 60, default, 1f);
				}
			}

			// Sunwrought: with the meter full, the strike rolls a sun out of the point it hit
			if (sunwroughtSet && sunCooldown <= 0 && proj.identity != lastSunSlash
				&& Player.GetModPlayer<ReaperPlayer>().HarvestFull)
			{
				lastSunSlash = proj.identity;
				sunCooldown = SunwroughtNemes.SunCooldown;
				float dir = SunDirection(Player, proj.Center);
				Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), proj.Center,
					Vector2.Zero, ModContent.ProjectileType<SunwroughtSun>(),
					Math.Max(1, (int)(proj.damage * SunwroughtNemes.SunShare)), 3f, Player.whoAmI,
					dir * SunwroughtSun.LaunchX, SunwroughtSun.LaunchY);
			}
		}

		/// <summary>A sun always rolls away from the reaper; one struck straight overhead rolls the way they face.</summary>
		public static float SunDirection(Player player, Vector2 point)
		{
			float dx = point.X - player.Center.X;
			return Math.Abs(dx) < 4f ? player.direction : Math.Sign(dx);
		}
	}

	/// <summary>
	/// The two sets' tells, drawn on the local reaper: Tidewrack's banked swells as beads of water
	/// circling the waist (count them and you know the size of the next wave), and - while
	/// Sunwrought's meter is full - the sun's whole path traced from the reticle, walked by the
	/// same <see cref="BallPhysics"/> the sun will roll by. Local player only, like the reach
	/// circle: nobody else's mouse is known.
	/// </summary>
	public class BiomeSetLayer : PlayerDrawLayer
	{
		private const int PathTicks = 96;

		public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.FrontAccFront);

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
		{
			Player p = drawInfo.drawPlayer;
			if (drawInfo.shadow != 0f || p.whoAmI != Main.myPlayer || p.dead || Main.gameMenu)
				return false;
			BiomeSetPlayer b = p.GetModPlayer<BiomeSetPlayer>();
			return (b.tidewrackSet && b.swells > 0) || (b.sunwroughtSet && p.HeldItem.ModItem is ScytheItem);
		}

		protected override void Draw(ref PlayerDrawSet drawInfo)
		{
			Player p = drawInfo.drawPlayer;
			BiomeSetPlayer b = p.GetModPlayer<BiomeSetPlayer>();
			Texture2D px = TextureAssets.MagicPixel.Value;
			float time = Main.GlobalTimeWrappedHourly;

			if (b.tidewrackSet && b.swells > 0)
			{
				Vector2 waist = p.MountedCenter + new Vector2(0f, 6f + p.gfxOffY) - Main.screenPosition;
				for (int i = 0; i < b.swells; i++)
				{
					float a = time * 2.2f + MathHelper.TwoPi * i / TidewrackHood.MaxSwells;
					// a flattened orbit, beads behind the body drawn dimmer
					Vector2 at = waist + new Vector2(MathF.Cos(a) * 20f, MathF.Sin(a) * 6f);
					float near = 0.55f + 0.45f * MathF.Sin(a);
					Add(ref drawInfo, px, at, new Color(154, 238, 221) * (0.5f + 0.5f * near), 4f);
					Add(ref drawInfo, px, at + new Vector2(-1f, -1f), new Color(232, 255, 248) * near, 2f);
				}
			}

			if (b.sunwroughtSet && p.HeldItem.ModItem is ScytheItem scythe && p.GetModPlayer<ReaperPlayer>().HarvestFull)
			{
				Vector2 point = scythe.StrikePoint(p, Main.MouseWorld, out _);
				float dir = BiomeSetPlayer.SunDirection(p, point);
				int s = SunwroughtSun.Size;
				Vector2 pos = point - new Vector2(s * 0.5f);
				Vector2 vel = new(dir * SunwroughtSun.LaunchX, SunwroughtSun.LaunchY);
				float pulse = 0.75f + 0.25f * MathF.Sin(time * 6f);
				for (int i = 0; i < PathTicks; i++)
				{
					Vector2 moved = BallPhysics.Step(ref pos, ref vel, s, s);
					pos += moved;
					if (i % 4 != 0)
						continue;
					float f = 1f - i / (float)PathTicks;
					Vector2 at = pos + new Vector2(s * 0.5f, s - 4f) - Main.screenPosition;
					Add(ref drawInfo, px, at, new Color(255, 200, 90) * (pulse * (0.3f + 0.6f * f)), 2f);
				}
			}
		}

		private static void Add(ref PlayerDrawSet drawInfo, Texture2D px, Vector2 at, Color color, float size)
		{
			// snapped to whole pixels so the dots sit on the grid like the reach circle's do
			Vector2 snapped = new((int)at.X, (int)at.Y);
			drawInfo.DrawDataCache.Add(new DrawData(px, snapped, new Rectangle(0, 0, 1, 1), color, 0f,
				new Vector2(0.5f), size, SpriteEffects.None, 0));
		}
	}
}
