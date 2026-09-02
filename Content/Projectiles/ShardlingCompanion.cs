using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Content.Buffs;

namespace Terrapex.Content.Projectiles
{
	/// <summary>Plan item #132's pet. Harmless, and it keeps its own eye on you.</summary>
	public class ShardlingCompanion : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			Main.projFrames[Type] = 4;
			Main.projPet[Type] = true;
			ProjectileID.Sets.CharacterPreviewAnimations[Type] =
				ProjectileID.Sets.SimpleLoop(0, 4, 8);
		}

		public override void SetDefaults()
		{
			Projectile.width = 22;
			Projectile.height = 22;
			Projectile.aiStyle = 26;
			AIType = ProjectileID.ZephyrFish;
			Projectile.friendly = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 18000;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.netImportant = true;
		}

		public override bool PreAI()
		{
			Player owner = Main.player[Projectile.owner];
			owner.zephyrfish = false;
			if (owner.dead)
				owner.ClearBuff(ModContent.BuffType<ShardlingBuff>());
			if (owner.HasBuff<ShardlingBuff>())
				Projectile.timeLeft = 2;
			return true;
		}

		public override void PostDraw(Color lightColor)
			=> RiftDraw.Bloom(Projectile.Center,
				RiftDraw.Glow(255, 255, 255, 0.25f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f)),
				0.3f);
	}
}
