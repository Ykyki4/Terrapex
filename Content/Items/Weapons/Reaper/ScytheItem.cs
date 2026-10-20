using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;
using Terrapex.Content.Projectiles.Reaper;

namespace Terrapex.Content.Items.Weapons.Reaper
{
	/// <summary>
	/// Every scythe. The item says how long the blade is, how far its reach goes and what its Reap
	/// does; the stroke itself lives in <see cref="ScytheSwing"/> and the strike at range in
	/// <see cref="ReapingSlash"/>, so the whole class plays by one set of rules.
	///
	/// A click does two things. The scythe swings round the player as before, and a cut lands
	/// where the cursor is - anywhere inside the reach, a circle drawn round the player while a
	/// scythe is held (<see cref="ReapRangeLayer"/>). The further out the click, the harder the
	/// cut: x0.5 at the player's feet, x1.5 at the rim, and the outer band of the circle always
	/// crits. A click past the circle lands on its rim.
	///
	/// Sprites follow the mod's convention for swung weapons: drawn as held facing right, haft
	/// from the bottom-left corner to the top-right, blade hanging off the top. The swing draws
	/// the item texture with its grip at the bottom-left, so a sprite drawn any other way lands
	/// off the arc.
	/// </summary>
	public abstract class ScytheItem : ModItem
	{
		/// <summary>Damage multiplier for a strike at the player's feet and at the rim of the reach.</summary>
		public const float NearMultiplier = 0.5f;
		public const float RimMultiplier = 1.5f;

		/// <summary>From this share of the reach outward a strike is on the edge: it always crits.</summary>
		public const float EdgeBand = 0.85f;

		/// <summary>Grip to tip, in pixels. Where the blade's edge begins follows from it.</summary>
		public abstract float Reach { get; }

		/// <summary>
		/// The radius of the reach, in tiles. It grows with the tier on crafted scythes; the found
		/// and dropped ones set their own to suit what they do at the point.
		/// </summary>
		public virtual float RangeTiles => 12f;

		public float RangePixels => RangeTiles * 16f;

		/// <summary>The reach as this player has it, after the Long Haft and anything like it.</summary>
		public float RangeFor(Player player) => RangePixels * player.GetModPlayer<ReaperPlayer>().rangeMult;

		/// <summary>The afterimage, strike and Reap tint.</summary>
		public virtual Color SlashColor => new(232, 207, 148);

		/// <summary>Damage, use time and knockback - the three numbers that differ per scythe.</summary>
		protected abstract void SetScytheDefaults();

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.DamageType = ModContent.GetInstance<ReapingDamage>();
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.autoReuse = true;
			Item.shoot = ModContent.ProjectileType<ScytheSwing>();
			Item.shootSpeed = 1f;
			Item.UseSound = SoundID.Item71;
			SetScytheDefaults();
		}

		public override bool CanUseItem(Player player)
			=> player.ownedProjectileCounts[ModContent.ProjectileType<ScytheSwing>()] == 0;

		/// <summary>
		/// Spends a full Harvest on this scythe's Reap. Called by <see cref="ReaperPlayer"/> when the
		/// right button goes down - not through the item's alt use, because Terraria will not start an
		/// alt use while the left button is held, and a Reap you cannot fire in the middle of a fight
		/// is a Reap nobody fires.
		/// </summary>
		public void TryReap(Player player)
		{
			ReaperPlayer reaper = player.GetModPlayer<ReaperPlayer>();
			if (!reaper.HarvestFull)
				return;
			Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
			player.ChangeDir(aim.X >= 0f ? 1 : -1);
			reaper.SpendHarvest();
			reaper.DrawBlade();
			int damage = (int)(player.GetWeaponDamage(Item) * reaper.reapDamage);
			Reap(player, player.GetSource_ItemUse(Item), player.MountedCenter, aim,
				damage, player.GetWeaponKnockback(Item, Item.knockBack));

			// the Crow Charm: every Reap lets a flock out in a ring
			if (reaper.crowCharm)
			{
				for (int i = 0; i < Content.Items.Accessories.CrowCharm.Crows; i++)
					Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.MountedCenter,
						Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / Content.Items.Accessories.CrowCharm.Crows) * 9f,
						ModContent.ProjectileType<ReapCrow>(), damage / 2, 2f, player.whoAmI);
			}
		}

		/// <summary>
		/// Where a strike lands for a cursor at <paramref name="mouse"/>, and how far out it is as a
		/// share of the reach (0 at the feet, 1 at the rim). Shared with the range layer, so the
		/// reticle the player sees is exactly where the cut will land.
		/// </summary>
		public Vector2 StrikePoint(Player player, Vector2 mouse, out float share)
		{
			Vector2 center = player.MountedCenter;
			Vector2 to = mouse - center;
			float range = RangeFor(player);
			float len = to.Length();
			if (len > range)
			{
				to *= range / len;
				len = range;
			}
			share = range > 0f ? len / range : 0f;
			return center + to;
		}

		public static float DistanceMultiplier(float share) => MathHelper.Lerp(NearMultiplier, RimMultiplier, share);

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			ReaperPlayer reaper = player.GetModPlayer<ReaperPlayer>();
			Vector2 point = StrikePoint(player, Main.MouseWorld, out float share);
			Vector2 aim = (point - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);

			// the sweep direction alternates, overhead then underarm, so held fire reads as a
			// reaper working a field rather than one stroke on repeat
			reaper.swingFlip = !reaper.swingFlip;
			float mode = reaper.IaidoReady ? ScytheSwing.ModeIaido : ScytheSwing.ModeSwing;
			reaper.DrawBlade();
			Projectile.NewProjectile(source, player.MountedCenter, aim, type, damage, knockback,
				player.whoAmI, reaper.swingFlip ? -1f : 1f, Reach, mode);

			int struck = (int)(damage * DistanceMultiplier(share));
			StrikeAt(player, source, point, share, struck, knockback, share >= EdgeBand, reaper.swingFlip);
			return false;
		}

		/// <summary>
		/// What lands at the point: the shared cut, for every scythe. Unique scythes add to it
		/// through <see cref="AfterStrike"/> rather than replacing it, so the circle always means
		/// the same thing. <paramref name="damage"/> already carries the distance multiplier.
		/// </summary>
		public virtual void StrikeAt(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 point,
			float share, int damage, float knockback, bool edge, bool flip)
			=> Slash(player, source, point, damage, knockback, edge, flip);

		/// <summary>
		/// What a scythe does once its cut at the point has finished - every scythe cuts the same
		/// way, and the ones that were found, dropped or bought then let something of their own out
		/// of that spot. Called on the owner's client when the cut ends; <paramref name="slash"/>
		/// carries the distance-scaled damage, <paramref name="aim"/> the direction of the stroke.
		/// </summary>
		public virtual void AfterStrike(Player player, Projectile slash, Vector2 aim, bool edge)
		{
		}

		/// <summary>The shared cut at a point, for scythes that add to it rather than replace it.</summary>
		protected static void Slash(Player player, IEntitySource source, Vector2 point, int damage,
			float knockback, bool edge, bool flip)
		{
			float start = (point - player.MountedCenter).ToRotation() - MathHelper.PiOver2 * (flip ? -1f : 1f);
			Projectile.NewProjectile(source, point, Vector2.Zero, ModContent.ProjectileType<ReapingSlash>(),
				damage, knockback * 0.6f, player.whoAmI, edge ? 1f : 0f, start, flip ? -1f : 1f);
		}

		/// <summary>
		/// What a full Harvest buys. The default is a spectral copy of this scythe, longer, one
		/// full turn, every hit on the edge. Scythes with their own idea override it.
		/// </summary>
		public virtual void Reap(Player player, IEntitySource source, Vector2 center,
			Vector2 aim, int damage, float knockback)
			=> ReapSpin(player, source, center, aim, damage, knockback);

		/// <summary>The default Reap's spin, for scythes that add to it rather than replace it.</summary>
		protected void ReapSpin(Player player, IEntitySource source, Vector2 center,
			Vector2 aim, int damage, float knockback)
			=> Projectile.NewProjectile(source, center, aim, ModContent.ProjectileType<ScytheReapSpin>(),
				(int)(damage * ScytheSwing.ReapDamage), knockback * 1.5f, player.whoAmI, 1f, Reach, ScytheSwing.ModeReap);

		/// <summary><paramref name="count"/> bolts of one style, evenly round the reaper.</summary>
		protected static void BoltRing(Player player, IEntitySource source, Vector2 center,
			int count, float speed, int style, int damage, float knockback)
		{
			for (int i = 0; i < count; i++)
			{
				Vector2 v = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / count) * speed;
				Projectile.NewProjectile(source, center, v, ModContent.ProjectileType<ReapBolt>(),
					damage, knockback, player.whoAmI, style);
			}
		}

		/// <summary><paramref name="count"/> bolts of one style in a fan of <paramref name="spread"/> radians.</summary>
		protected static void BoltFan(Player player, IEntitySource source, Vector2 center,
			Vector2 aim, int count, float spread, float speed, int style, int damage, float knockback)
		{
			for (int i = 0; i < count; i++)
			{
				float t = count == 1 ? 0f : i / (count - 1f) - 0.5f;
				Projectile.NewProjectile(source, center, aim.RotatedBy(t * spread) * speed,
					ModContent.ProjectileType<ReapBolt>(), damage, knockback, player.whoAmI, style);
			}
		}

		/// <summary>
		/// What this scythe does when it lands, beyond the damage - a debuff, a bee, a soul. Called
		/// on the owner's client only, once per target per stroke, for the blade, the strike at range
		/// and the Reap alike. <paramref name="edge"/> is true only for the blade's own edge.
		/// </summary>
		public virtual void OnScytheHit(Player player, Projectile stroke, NPC target, NPC.HitInfo hit,
			bool edge, bool reap)
		{
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips)
		{
			int edge = (int)System.Math.Round(Main.LocalPlayer.GetModPlayer<ReaperPlayer>().edgeShare * 100f);
			tooltips.Add(new TooltipLine(Mod, "ScytheStrike",
				Language.GetTextValue("Mods.Terrapex.Common.ScytheStrike",
					(int)(RangeTiles * Main.LocalPlayer.GetModPlayer<ReaperPlayer>().rangeMult))));
			tooltips.Add(new TooltipLine(Mod, "ScytheEdge",
				Language.GetTextValue("Mods.Terrapex.Common.ScytheEdge", edge)));
			tooltips.Add(new TooltipLine(Mod, "ScytheReap",
				Language.GetTextValue("Mods.Terrapex.Common.ScytheReap")));
		}
	}
}
