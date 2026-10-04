using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;

namespace Terrapex.Content.Projectiles.Reaper
{
	/// <summary>
	/// The Kusarigama's Reap: the sickle thrown on its chain. It flies at the cursor, hooks the
	/// first enemy that can be moved, and hauls it back to blade's length - then comes home. Every
	/// hit it lands is an edge hit, because a Reap is the meter spent.
	///
	/// Bosses and anything with no knockback are cut but not hooked: dragging the Eye of Cthulhu
	/// across the screen on a chain would be funny exactly once.
	/// </summary>
	public class KusarigamaHead : ModProjectile
	{
		public const float Speed = 17f;

		/// <summary>
		/// ai[1] at spawn: the everyday strike. The Kusarigama's cut spawns the sickle at the clicked
		/// point already retracting (ai[0] = Back), so the chain reads as having reached that far and
		/// being hauled in. It cuts on the way, never hooks, never crits by itself, and leaves the
		/// player free to keep swinging - the hook and the all-edge damage belong to the Reap.
		/// </summary>
		public const float QuickThrow = -2f;

		private bool Quick => Hooked == QuickThrow;
		private const float QuickRange = 260f;
		private const float MaxRange = 440f;
		private const int PullTicks = 18;
		private const float HoldDistance = 90f;

		/// <summary>States in ai[0]. Back is public: the Kusarigama's strike spawns the sickle at the
		/// point already retracting.</summary>
		public const float Out = 0f, Pulling = 1f, Back = 2f;

		private ref float State => ref Projectile.ai[0];
		private ref float Hooked => ref Projectile.ai[1];
		private ref float Timer => ref Projectile.ai[2];

		public override string Texture => "Terrapex/Content/Items/Weapons/Reaper/Kusarigama";

		public override void SetDefaults()
		{
			Projectile.width = 26;
			Projectile.height = 26;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = ModContent.GetInstance<ReapingDamage>();
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
			Projectile.timeLeft = 240;
			Hooked = -1f;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			if (!owner.active || owner.dead)
			{
				Projectile.Kill();
				return;
			}
			Vector2 toOwner = owner.MountedCenter - Projectile.Center;
			if (!Quick)
			{
				owner.itemAnimation = owner.itemTime = 2;
				owner.heldProj = Projectile.whoAmI;
				owner.ChangeDir(toOwner.X > 0f ? -1 : 1);
				owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full,
					(-toOwner).ToRotation() - MathHelper.PiOver2);
			}
			Timer++;

			if (State == Out)
			{
				if (toOwner.Length() > (Quick ? QuickRange : MaxRange) || Timer > (Quick ? 40f : 30f))
					State = Back;
			}
			else if (State == Pulling)
			{
				NPC npc = Main.npc[(int)Hooked];
				if (!npc.active || Timer > PullTicks)
				{
					State = Back;
				}
				else
				{
					Projectile.Center = npc.Center;
					Vector2 pull = owner.MountedCenter - npc.Center;
					if (pull.Length() > HoldDistance && Projectile.owner == Main.myPlayer)
					{
						npc.velocity = Vector2.Normalize(pull) * 15f * npc.knockBackResist + npc.velocity * 0.2f;
						npc.netUpdate = true;
					}
					else if (pull.Length() <= HoldDistance)
						State = Back;
				}
			}

			if (State == Back)
			{
				float d = toOwner.Length();
				if (d < 30f)
				{
					Projectile.Kill();
					return;
				}
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, toOwner / d * 22f, 0.25f);
			}
			else if (State == Pulling)
				Projectile.velocity = Vector2.Zero;

			Projectile.rotation += 0.55f * Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X);
			Lighting.AddLight(Projectile.Center, 0.45f, 0.18f, 0.1f);
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
		{
			if (!Quick)
				modifiers.SetCrit();
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Quick)
			{
				ReapingSlash.RangedHit(Projectile, target, hit);
				return;
			}
			if (State != Out || target.boss || target.knockBackResist <= 0f)
				return;
			State = Pulling;
			Hooked = target.whoAmI;
			Timer = 0f;
			Projectile.netUpdate = true;
			SoundEngine.PlaySound(SoundID.Item37 with { Pitch = -0.2f }, target.Center);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Player owner = Main.player[Projectile.owner];
			Vector2 from = owner.MountedCenter;
			Vector2 to = Projectile.Center;
			Vector2 span = to - from;
			float len = span.Length();
			float rot = span.ToRotation();
			Texture2D px = TextureAssets.MagicPixel.Value;

			// the chain: alternating iron links on a 6 px pitch, lit by the world like the blade
			Color dark = Lighting.GetColor((from + to).ToTileCoordinates(), new Color(58, 54, 84));
			Color lit = Lighting.GetColor((from + to).ToTileCoordinates(), new Color(143, 163, 196));
			for (float s = 8f; s < len - 6f; s += 6f)
			{
				Vector2 p = from + span / len * s - Main.screenPosition;
				bool odd = ((int)(s / 6f) & 1) == 1;
				Main.EntitySpriteDraw(px, p, new Rectangle(0, 0, 1, 1), odd ? lit : dark, rot,
					new Vector2(0.5f, 0.5f), new Vector2(4f, odd ? 2f : 4f), SpriteEffects.None);
			}

			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Main.EntitySpriteDraw(tex, to - Main.screenPosition, null, lightColor, Projectile.rotation,
				tex.Size() / 2f, 0.7f, SpriteEffects.None);
			return false;
		}
	}
}
