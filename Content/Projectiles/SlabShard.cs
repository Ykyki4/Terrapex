using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// What is left of a plate the Slabmaul broke: a piece of the wall, thrown where the swing
	/// was pointing.
	///
	/// It flies flat for most of its life rather than arcing straight down, because the whole
	/// reason it exists is that the tier's bosses are in the air and a weapon whose only answer
	/// is a wall on the floor has nothing to say to them.
	/// </summary>
	public class SlabShard : ModProjectile
	{
		public override string Texture => "Terrapex/Content/Projectiles/StoneChip";

		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = 3;
			Projectile.timeLeft = 150;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
		}

		public override void AI()
		{
			Projectile.rotation += 0.3f * (Projectile.velocity.X >= 0f ? 1f : -1f);

			// carries for about a second and only then remembers it is a rock
			if (Projectile.timeLeft < 90)
				Projectile.velocity.Y += 0.16f;
			Projectile.velocity.X *= 0.996f;

			Lighting.AddLight(Projectile.Center, 0.16f, 0.06f, 0.20f);
			if (Main.rand.NextBool(4))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Stone,
					-Projectile.velocity * 0.12f, 120, default, 0.9f);
				d.noGravity = true;
			}
			if (Main.rand.NextBool(10))
			{
				Dust r = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
					-Projectile.velocity * 0.08f, 130, default, 0.8f);
				r.noGravity = true;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
			=> target.AddBuff(ModContent.BuffType<Cracked>(), 180);

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			Projectile.Kill();
			return false;
		}

		public override void OnKill(int timeLeft)
		{
			SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.35f }, Projectile.Center);
			for (int i = 0; i < 5; i++)
			{
				Dust.NewDustPerfect(Projectile.Center, DustID.Stone,
					Main.rand.NextVector2Circular(2.4f, 2.4f), 110, default, 0.95f);
			}
		}
	}
}
