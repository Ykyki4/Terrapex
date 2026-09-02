using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Projectiles
{
	// Built the way ExampleMod's spear is, because hand-rolling the hold-out was what kept
	// putting the spear at the wrong height. Three things carry it, and all three come from
	// vanilla rather than from us:
	//
	//   * CloneDefaults(ProjectileID.Spear) for the size, scale, hide and ownerHitCheck flags,
	//   * ItemID.Sets.Spears on the item, which is what makes vanilla drive rotation and the
	//     player's arm for a held spear,
	//   * the sprite pointing up-LEFT. That is the convention the 45/135 degree rotations below
	//     assume; a sprite pointing up-right needs its own offsets and lands off the aim line.
	//
	// Position is plain MountedCenter, no RotatedRelativePoint and no gfxOffY. Vanilla already
	// puts held projectiles where the hand is; adding our own offset on top is what pushed it low.
	public class WedgeSpear : ModProjectile
	{
		private const float RangeMin = 26f;    // how far out the butt sits at the start
		private const float RangeMax = 116f;   // full extension

		public override void SetDefaults()
		{
			Projectile.CloneDefaults(ProjectileID.Spear);
			Projectile.DamageType = DamageClass.Melee;
			// The sprite is 60x60 and carries the size itself, so no fractional scaling.
			Projectile.scale = 1f;
			Projectile.width = 26;
			Projectile.height = 26;
		}

		public override bool PreAI()
		{
			Player player = Main.player[Projectile.owner];
			int duration = player.itemAnimationMax;

			player.heldProj = Projectile.whoAmI;

			if (Projectile.timeLeft > duration)
				Projectile.timeLeft = duration;

			// velocity is not movement here, it stores the direction of the thrust
			Projectile.velocity = Vector2.Normalize(Projectile.velocity);

			float half = duration * 0.5f;
			float progress = Projectile.timeLeft < half
				? Projectile.timeLeft / half
				: (duration - Projectile.timeLeft) / half;

			Projectile.Center = player.MountedCenter + Vector2.SmoothStep(
				Projectile.velocity * RangeMin, Projectile.velocity * RangeMax, progress);

			// vanilla hands us a base rotation each frame; these bring the up-left sprite onto it
			if (Projectile.spriteDirection == -1)
				Projectile.rotation += MathHelper.ToRadians(45f);
			else
				Projectile.rotation += MathHelper.ToRadians(135f);

			if (!Main.dedServ && Main.rand.NextBool(6))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(7f, 7f),
					DustID.PurpleTorch, Vector2.Zero, 130, default, 0.85f);
				d.noGravity = true;
			}

			return false;
		}

		public override void CutTiles()
		{
			// let the spear mow grass the way vanilla spears do
			Vector2 dir = Vector2.Normalize(Projectile.velocity);
			Utils.PlotTileLine(Projectile.Center - dir * 24f, Projectile.Center + dir * 24f,
				Projectile.width * Projectile.scale, Terraria.DelegateMethods.CutTiles);
		}
	}
}
