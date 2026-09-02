using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Players;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// The shell the Riftsteel set and the Carapace Charm both grant. It is the Keeper's own
	/// mechanic handed to the player: plates orbit, eat one shot each, and the whole shell
	/// comes back together after a cooldown rather than trickling in one at a time.
	/// </summary>
	public class GuardPlate : ModProjectile
	{
		public override string Texture => "Terrapex/Content/Projectiles/WardenPlateMinion";

		private const float Radius = 46f;
		private const float Spin = 0.035f;

		/// <summary>Which seat of the ring this plate holds.</summary>
		private ref float Slot => ref Projectile.ai[0];
		private ref float Total => ref Projectile.ai[1];

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Type] = 6;
			ProjectileID.Sets.TrailingMode[Type] = 3;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			// the shell is the set bonus, and a player who cannot see it is up has no reason
			// to play around it. Each seat breathes on its own phase so the ring turns.
			RiftDraw.Trail(Projectile, f => RiftDraw.Glow(120, 50, 180, f * 0.30f),
				f => 0.55f + 0.40f * f);
			float pulse = 0.20f + 0.05f * (float)Math.Sin(Main.GameUpdateCount * 0.08f + Slot);
			RiftDraw.Bloom(Projectile.Center, RiftDraw.Glow(180, 90, 230, 0.35f), pulse);
			RiftDraw.Head(Projectile, new Color(210, 180, 240));
			return false;
		}

		public override void SetDefaults()
		{
			Projectile.width = 22;
			Projectile.height = 22;
			Projectile.aiStyle = -1;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.damage = 0;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 2;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.netImportant = true;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			TerrapexPlayer mp = owner.GetModPlayer<TerrapexPlayer>();

			if (owner.dead || !owner.active || mp.guardPlates <= 0)
			{
				Projectile.Kill();
				return;
			}
			Projectile.timeLeft = 2;

			float total = Math.Max(1f, Total);
			float angle = Main.GameUpdateCount * Spin + Slot / total * MathHelper.TwoPi;
			Projectile.Center = owner.MountedCenter + angle.ToRotationVector2() * Radius;
			Projectile.rotation += 0.06f;

			if (Projectile.owner == Main.myPlayer)
				Block(owner, mp);

			Lighting.AddLight(Projectile.Center, 0.2f, 0.08f, 0.26f);
		}

		private void Block(Player owner, TerrapexPlayer mp)
		{
			Rectangle box = Projectile.Hitbox;
			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				Projectile shot = Main.projectile[i];
				if (!shot.active || !shot.hostile || shot.damage <= 0 || !shot.Hitbox.Intersects(box))
					continue;

				shot.Kill();
				mp.BreakGuardPlates();

				Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit42, Projectile.Center);
				for (int k = 0; k < 16; k++)
				{
					Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Stone,
						Main.rand.NextVector2Circular(4.5f, 4.5f), 80, default, 1.25f);
					d.noGravity = true;
				}
				// an outward ring on top of the rubble: the plate dies on the same tick, so
				// without this a block leaves nothing behind to say where the shot went
				for (int k = 0; k < 12; k++)
				{
					Vector2 out2 = (MathHelper.TwoPi * k / 12f).ToRotationVector2();
					Dust r = Dust.NewDustPerfect(Projectile.Center + out2 * 8f, DustID.PurpleTorch,
						out2 * 3.2f, 90, default, 1.15f);
					r.noGravity = true;
				}
				return;
			}
		}

		public override Color? GetAlpha(Color lightColor) => new Color(210, 180, 240);
	}
}
