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

namespace Terrapex.Content.Projectiles.Reaper
{
	/// <summary>
	/// One stroke of a scythe, held. See <c>CLASS_REAPER.md</c>.
	///
	/// The hitbox is the whole scythe, not a box: a segment from the hands out to the tip. Its
	/// outer part is the edge, and an edge hit always crits - that is the whole class in one rule,
	/// because it makes distance the skill. Whatever is pressed against the reaper is hit by the
	/// haft for ordinary damage.
	///
	/// ai[0] is the sweep sign, ai[1] the reach, ai[2] the mode. The aim is the velocity it was
	/// born with, which is why the projectile pins its Center every tick instead of moving.
	/// </summary>
	public class ScytheSwing : ModProjectile
	{
		public const float ModeSwing = 0f;
		public const float ModeReap = 1f;
		public const float ModeIaido = 2f;

		/// <summary>Swing arc in radians: 270 degrees, centred on the aim.</summary>
		private const float SwingArc = MathF.PI * 1.5f;
		private const int ReapTicks = 26;
		private const float ReapReach = 1.6f;
		public const float ReapDamage = 3f;

		/// <summary>
		/// Hits register from this far out. Everything inside the edge is the haft and hits for
		/// ordinary damage - the first version started the hitbox at 35%, so an enemy pressed
		/// against the reaper took nothing at all, which made the long blades worse, not better.
		/// </summary>
		private const float HitStart = 0.12f;
		private const float BladeWidth = 20f;

		private const int Ghosts = 6;

		private float Sign => Projectile.ai[0];
		private float BaseReach => Projectile.ai[1];
		private float Mode => Projectile.ai[2];
		private bool IsReap => Mode == ModeReap;

		private ref float Timer => ref Projectile.localAI[0];
		private ref float Duration => ref Projectile.localAI[1];

		private float aimAngle;
		private float prevAngle;
		private readonly float[] ghosts = new float[Ghosts];
		private bool lastWasEdge;

		public override string Texture => "Terrapex/Content/Projectiles/RiftFlare";

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = ModContent.GetInstance<ReapingDamage>();
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.ownerHitCheck = true;
			Projectile.usesLocalNPCImmunity = true;
			// one hit per target per stroke; set properly once the duration is known
			Projectile.localNPCHitCooldown = 60;
			Projectile.timeLeft = 600;
			Projectile.hide = false;
		}

		private float Reach => BaseReach * (IsReap ? ReapReach : 1f);
		private float Arc => IsReap ? MathF.Tau : SwingArc;

		private float AngleAt(float t)
		{
			// ease in and out: a wind-up, the cut, the follow-through
			float e = t < 0.5f ? 4f * t * t * t : 1f - MathF.Pow(-2f * t + 2f, 3f) / 2f;
			int facing = Math.Sign(MathF.Cos(aimAngle)) >= 0 ? 1 : -1;
			float dir = Sign * facing;
			return aimAngle + dir * (-Arc / 2f + Arc * e);
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead || owner.HeldItem.ModItem is not ScytheItem)
			{
				Projectile.Kill();
				return;
			}

			if (Timer == 0f)
			{
				aimAngle = Projectile.velocity.ToRotation();
				Duration = IsReap ? ReapTicks : Math.Max(8, owner.itemAnimationMax);
				Projectile.localNPCHitCooldown = (int)Duration + 1;
				owner.ChangeDir(MathF.Cos(aimAngle) >= 0f ? 1 : -1);
				prevAngle = AngleAt(0f);
				for (int i = 0; i < Ghosts; i++)
					ghosts[i] = prevAngle;
				if (IsReap)
					SoundEngine.PlaySound(SoundID.Item113 with { Pitch = -0.3f }, owner.Center);
				else if (Mode == ModeIaido)
					SoundEngine.PlaySound(SoundID.Item1 with { Pitch = 0.6f }, owner.Center);
			}

			for (int i = Ghosts - 1; i > 0; i--)
				ghosts[i] = ghosts[i - 1];
			ghosts[0] = Projectile.rotation;

			prevAngle = Projectile.rotation;
			float t = Timer / Duration;
			Projectile.rotation = AngleAt(Math.Min(1f, t));
			if (Timer == 0f)
				prevAngle = Projectile.rotation;

			Projectile.Center = owner.MountedCenter;
			Projectile.velocity = aimAngle.ToRotationVector2();
			owner.heldProj = Projectile.whoAmI;
			owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full,
				Projectile.rotation - MathHelper.PiOver2);
			if (owner.itemAnimation < 2)
				owner.itemAnimation = owner.itemTime = 2;

			Vector2 tip = owner.MountedCenter + Projectile.rotation.ToRotationVector2() * Reach;
			Lighting.AddLight(tip, SlashColor(owner).ToVector3() * (IsReap ? 0.6f : 0.25f));

			Timer++;
			if (Timer > Duration)
				Projectile.Kill();
		}

		private static Color SlashColor(Player owner)
			=> owner.HeldItem.ModItem is ScytheItem s ? s.SlashColor : Color.White;

		// ------------------------------------------------------------------ hitting

		private bool BladeHits(Rectangle box, float from, float angle)
		{
			Vector2 c = Main.player[Projectile.owner].MountedCenter;
			Vector2 dir = angle.ToRotationVector2();
			float _ = 0f;
			return Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(),
				c + dir * Reach * from, c + dir * Reach, BladeWidth * (IsReap ? 1.6f : 1f), ref _);
		}

		/// <summary>
		/// The blade turns up to ~17 degrees a tick at the fast middle of a stroke, which at a long
		/// reach leaves gaps a small enemy fits through, so the half-step is checked as well.
		/// </summary>
		private bool SweepHits(Rectangle box, float from)
		{
			float mid = prevAngle + MathHelper.WrapAngle(Projectile.rotation - prevAngle) * 0.5f;
			return BladeHits(box, from, Projectile.rotation) || BladeHits(box, from, mid)
				|| BladeHits(box, from, prevAngle);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
			=> SweepHits(targetHitbox, HitStart);

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			ReaperPlayer reaper = Main.player[Projectile.owner].GetModPlayer<ReaperPlayer>();
			lastWasEdge = Mode != ModeSwing || SweepHits(target.Hitbox, 1f - reaper.edgeShare);
			if (lastWasEdge)
				modifiers.SetCrit();
			modifiers.HitDirectionOverride = Math.Sign(target.Center.X - Main.player[Projectile.owner].Center.X);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			Player owner = Main.player[Projectile.owner];
			if (Projectile.owner == Main.myPlayer)
			{
				if (!IsReap && !target.immortal)
					owner.GetModPlayer<ReaperPlayer>().AddHarvest(
						lastWasEdge ? ReaperPlayer.HarvestPerEdgeHit : ReaperPlayer.HarvestPerHit);
				if (owner.HeldItem.ModItem is ScytheItem scythe)
					scythe.OnScytheHit(owner, Projectile, target, hit, lastWasEdge, IsReap);
			}

			if (lastWasEdge)
			{
				// the edge is announced, not implied: a bright cut and a ring of sparks
				Color c = SlashColor(owner);
				for (int i = 0; i < 8; i++)
				{
					Dust d = Dust.NewDustPerfect(target.Center, DustID.TintableDustLighted,
						Main.rand.NextVector2Circular(4f, 4f), 0, c, 1.1f);
					d.noGravity = true;
				}
			}
		}

		public override void CutTiles()
		{
			Vector2 c = Main.player[Projectile.owner].MountedCenter;
			Utils.PlotTileLine(c, c + Projectile.rotation.ToRotationVector2() * Reach, BladeWidth,
				DelegateMethods.CutTiles);
		}

		// ------------------------------------------------------------------ drawing

		public override bool PreDraw(ref Color lightColor)
		{
			Player owner = Main.player[Projectile.owner];
			if (owner.HeldItem.ModItem is not ScytheItem)
				return false;

			Texture2D tex = TextureAssets.Item[owner.HeldItem.type].Value;
			Color slash = SlashColor(owner);
			int facing = Math.Sign(MathF.Cos(aimAngle)) >= 0 ? 1 : -1;
			bool mirrored = Sign * facing < 0f;

			// the sprite's tip is its top-right corner, so its diagonal is the reach
			float scale = Reach / new Vector2(tex.Width, tex.Height).Length();
			Vector2 origin = mirrored ? new Vector2(tex.Width, tex.Height) : new Vector2(0f, tex.Height);
			SpriteEffects fx = mirrored ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			float spriteTurn = mirrored ? MathHelper.PiOver4 * 3f : MathHelper.PiOver4;
			Vector2 at = owner.MountedCenter - Main.screenPosition + new Vector2(0f, owner.gfxOffY);

			// afterimages carry the motion; glow colours have A = 0 in the default batch
			for (int i = Ghosts - 1; i >= 0; i--)
			{
				float f = 1f - (i + 1f) / (Ghosts + 1f);
				Color g = new Color(slash.R, slash.G, slash.B, 0) * (f * (IsReap ? 0.55f : 0.32f));
				Main.EntitySpriteDraw(tex, at, null, g, ghosts[i] + spriteTurn, origin, scale, fx);
			}

			Color body = IsReap ? new Color(slash.R, slash.G, slash.B, 60) * 0.9f : lightColor;
			Main.EntitySpriteDraw(tex, at, null, body, Projectile.rotation + spriteTurn, origin, scale, fx);
			if (IsReap || Mode == ModeIaido)
			{
				Main.EntitySpriteDraw(tex, at, null, new Color(255, 255, 255, 0) * 0.35f,
					Projectile.rotation + spriteTurn, origin, scale, fx);
			}
			return false;
		}
	}

	/// <summary>
	/// The Reap's spin: the same held stroke, a type of its own. The Reap can now be fired in the
	/// middle of an ordinary swing, and the item gates new swings on how many ScytheSwings the
	/// player owns - sharing the type, a Reap would have blocked the next swing, and a swing the
	/// Reap.
	/// </summary>
	public class ScytheReapSpin : ScytheSwing
	{
	}
}
