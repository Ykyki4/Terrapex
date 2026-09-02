using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.GlobalNPCs;
using Terrapex.Common.Players;

namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// The cut the Stitch leaves behind it. It has no sprite of its own — it is the seam
	/// between where the player was and where they are, drawn out of MagicPixel in the default
	/// batch, where a zero alpha adds light.
	/// </summary>
	public class StitchSlash : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_1";

		private const float Width = 26f;
		private const int Life = 18;

		private ref float Length => ref Projectile.ai[0];

		public override void SetDefaults()
		{
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = -1;
			Projectile.timeLeft = Life;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void AI()
		{
			if (Projectile.timeLeft != Life)
				return;

			// the seam is laid once, on the tick it is born
			for (int i = 0; i <= 20; i++)
			{
				Vector2 at = Projectile.Center + Projectile.velocity * (Length * i / 20f);
				Dust d = Dust.NewDustPerfect(at + Main.rand.NextVector2Circular(5f, 5f),
					DustID.Vortex, Main.rand.NextVector2Circular(1.2f, 1.2f), 110, default, 1f);
				d.noGravity = true;
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			float point = 0f;
			Vector2 end = Projectile.Center + Projectile.velocity * Length;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
				Projectile.Center, end, Width, ref point);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			NPC other = Main.player[Projectile.owner].GetModPlayer<TerrapexPlayer>().TakeSeamTarget(target);
			if (other != null)
				BoundGlobalNPC.Bind(other, target);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D px = TextureAssets.MagicPixel.Value;
			Rectangle src = new Rectangle(0, 0, 1, 1);
			Vector2 at = Projectile.Center - Main.screenPosition;
			float rot = Projectile.velocity.ToRotation();

			// squared, so the cut snaps shut instead of dissolving
			float fade = Projectile.timeLeft / (float)Life;
			fade *= fade;

			Main.EntitySpriteDraw(px, at, src, new Color(18, 124, 120, 0) * (0.55f * fade), rot,
				new Vector2(0f, 0.5f), new Vector2(Length, 14f), SpriteEffects.None, 0);
			Main.EntitySpriteDraw(px, at, src, new Color(53, 201, 184, 0) * (0.80f * fade), rot,
				new Vector2(0f, 0.5f), new Vector2(Length, 5f), SpriteEffects.None, 0);
			Main.EntitySpriteDraw(px, at, src, new Color(216, 255, 247, 0) * fade, rot,
				new Vector2(0f, 0.5f), new Vector2(Length, 1.6f), SpriteEffects.None, 0);
			return false;
		}
	}
}
