using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Reaper;
using Terrapex.Content.Items.Weapons.Reaper;

namespace Terrapex.Content.Projectiles.Reaper
{
	/// <summary>
	/// The reaper's strike at a distance: a cut that appears where the player clicked, inside the
	/// scythe's reach, and sweeps through that spot. See <c>CLASS_REAPER.md</c>.
	///
	/// It replaced a wave that simply flew at the cursor. That made the class casual, but it made it
	/// a beam sword, and the user's own idea was better: the reach is a circle drawn round the
	/// player, the click places the cut anywhere inside it, and the further out it lands the harder
	/// it hits - the outer band of the circle is the reach's own edge, and it always crits. It is the
	/// blade's rule (the edge is where the damage lives) moved from the length of a scythe to the
	/// length of an arm's sweep.
	///
	/// The distance multiplier is baked into the damage at spawn, on the owner's client, which is
	/// the only one that knows where the mouse was. ai[0] is 1 inside the edge band; ai[1] is the
	/// sweep's starting angle; the sign of ai[2] is its direction.
	/// </summary>
	public class ReapingSlash : ModProjectile
	{
		private const int Life = 16;

		/// <summary>
		/// |ai[2]| for a cut that is itself an after-effect (the Optic Scythe's twin): it cuts like any
		/// other but does not call AfterStrike, or every echo would echo again.
		/// </summary>
		public const float Echo = 2f;
		private const int ActiveFrom = 2;
		private const int ActiveTo = 11;
		private const float Radius = 46f;

		private bool Edge => Projectile.ai[0] == 1f;
		private ref float Timer => ref Projectile.localAI[0];
		private Color tint = Color.White;

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = ModContent.GetInstance<ReapingDamage>();
			Projectile.penetrate = -1;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void AI()
		{
			if (Timer == 0f)
			{
				Player owner = Main.player[Projectile.owner];
				if (owner.HeldItem.ModItem is ScytheItem scythe)
					tint = scythe.SlashColor;
				Terraria.Audio.SoundEngine.PlaySound((Edge ? SoundID.Item71 with { Pitch = 0.5f } : SoundID.Item71 with { Pitch = 0.15f, Volume = 0.6f }),
					Projectile.Center);
			}
			Timer++;
			Projectile.velocity = Vector2.Zero;
			float t = Timer / Life;
			float dir = Projectile.ai[2] >= 0f ? 1f : -1f;
			Projectile.rotation = Projectile.ai[1] + dir * MathHelper.Pi * (t * (2f - t));
			Lighting.AddLight(Projectile.Center, tint.ToVector3() * (Edge ? 0.6f : 0.35f));
		}

		public override bool? CanDamage() => Timer >= ActiveFrom && Timer <= ActiveTo;

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
			return Vector2.DistanceSquared(nearest, Projectile.Center) <= Radius * Radius;
		}

		/// <summary>
		/// On the edge band - or, under the Darner reaper head, on any stitched enemy: the thread
		/// tells the reaper where to aim, so a stitched target is always struck as if at the rim.
		/// </summary>
		private bool StrikesEdge(NPC target)
			=> Edge || (Main.player[Projectile.owner].GetModPlayer<Common.Players.TerrapexPlayer>().darnerReaper
				&& target.GetGlobalNPC<Common.GlobalNPCs.BoundGlobalNPC>().IsBound);

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (StrikesEdge(target))
				modifiers.SetCrit();
			modifiers.HitDirectionOverride = Math.Sign(target.Center.X - Main.player[Projectile.owner].Center.X);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			bool edge = StrikesEdge(target);
			RangedHit(Projectile, target, hit, edge);
			if (edge)
			{
				for (int i = 0; i < 8; i++)
				{
					Dust d = Dust.NewDustPerfect(target.Center, DustID.TintableDustLighted,
						Main.rand.NextVector2Circular(4f, 4f), 0, new Color(255, 214, 120), 1.1f);
					d.noGravity = true;
				}
			}
		}

		/// <summary>
		/// What anything a scythe strikes at range does on a hit, this cut or a unique scythe's own
		/// thing: banks Harvest (double inside the edge band) and runs the scythe's OnScytheHit as a
		/// non-blade hit. Owner's client only.
		/// </summary>
		public static void RangedHit(Projectile projectile, NPC target, NPC.HitInfo hit, bool edge = false)
		{
			if (projectile.owner != Main.myPlayer)
				return;
			Player owner = Main.player[projectile.owner];
			if (!target.immortal)
				owner.GetModPlayer<ReaperPlayer>().AddHarvest(
					edge ? ReaperPlayer.HarvestPerStrikeHit * 2f : ReaperPlayer.HarvestPerStrikeHit);
			if (owner.HeldItem.ModItem is ScytheItem scythe)
				scythe.OnScytheHit(owner, projectile, target, hit, false, false);
		}

		public override void OnKill(int timeLeft)
		{
			if (Projectile.owner != Main.myPlayer)
				return;
			Player owner = Main.player[Projectile.owner];
			if (owner.HeldItem.ModItem is ScytheItem scythe && Math.Abs(Projectile.ai[2]) < Echo)
			{
				Vector2 aim = (Projectile.Center - owner.MountedCenter).SafeNormalize(Vector2.UnitX * owner.direction);
				scythe.AfterStrike(owner, Projectile, aim, Edge);
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Vector2 origin = new(tex.Width * 0.25f, tex.Height / 2f);
			Vector2 at = Projectile.Center - Main.screenPosition;
			float t = Timer / Life;
			float fade = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
			float scale = (Edge ? 1.25f : 1f) * MathHelper.Lerp(0.8f, 1.05f, t);
			float dir = Projectile.ai[2] >= 0f ? 1f : -1f;
			Color glow = new Color(tint.R, tint.G, tint.B, 0) * fade;

			// the sweep's own trail: the crescent drawn at the angles it just passed through
			for (int i = 4; i >= 1; i--)
			{
				float back = Projectile.rotation - dir * i * 0.22f;
				Main.EntitySpriteDraw(tex, at, null, glow * (0.18f * (5 - i)), back, origin, scale, SpriteEffects.None);
			}
			Main.EntitySpriteDraw(tex, at, null, glow * 0.95f, Projectile.rotation, origin, scale, SpriteEffects.None);
			Main.EntitySpriteDraw(tex, at, null, new Color(255, 255, 255, 0) * (0.45f * fade),
				Projectile.rotation, origin, scale * 0.85f, SpriteEffects.None);
			if (Edge)
				RiftDraw.Ring(Projectile.Center, new Color(255, 214, 120, 0) * (0.6f * fade), 0.5f + 0.4f * t);
			return false;
		}
	}
}
