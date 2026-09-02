using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Projectiles
{
	/// <summary>The Shuttle. It ties each target it bites to the one before it.</summary>
	public class ShuttleYoyo : ModProjectile
	{
		/// <summary>The last thing it bit, so the next bite has something to tie to.</summary>
		private ref float LastBit => ref Projectile.localAI[0];

		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.YoyosLifeTimeMultiplier[Type] = 14f;
			ProjectileID.Sets.YoyosMaximumRange[Type] = 360f;
			ProjectileID.Sets.YoyosTopSpeed[Type] = 15.5f;
		}

		public override void SetDefaults()
		{
			Projectile.CloneDefaults(ProjectileID.Yelets);
			Projectile.width = 20;
			Projectile.height = 20;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source) => LastBit = -1f;

		public override void AI()
		{
			Lighting.AddLight(Projectile.Center, 0.18f, 0.44f, 0.42f);
			if (Main.rand.NextBool(4))
			{
				Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Vortex,
					Main.rand.NextVector2Circular(1f, 1f), 140, default, 0.75f);
				d.noGravity = true;
			}
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (Projectile.owner != Main.myPlayer)
				return;

			int last = (int)LastBit;
			if (last >= 0 && last < Main.maxNPCs && last != target.whoAmI && Main.npc[last].active)
			{
				FriendlyThread.Between(Projectile.GetSource_FromThis(), Main.npc[last].Center,
					target.Center, (int)(Projectile.damage * 0.4f), Projectile.owner, 180);
			}
			LastBit = target.whoAmI;
		}
	}
}
