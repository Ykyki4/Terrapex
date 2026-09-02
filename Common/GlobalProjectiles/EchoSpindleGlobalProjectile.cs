using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Accessories;

namespace Terrapex.Common.GlobalProjectiles
{
	/// <summary>
	/// The Echo Spindle: a minion hit sometimes lands a second time.
	///
	/// The echo is dealt directly rather than by spawning a copy, because a duplicated minion
	/// projectile would also duplicate everything hanging off it — whip tags, on-hit effects,
	/// the lot — and a quarter-chance accessory is not supposed to double a build.
	/// </summary>
	public class EchoSpindleGlobalProjectile : GlobalProjectile
	{
		private static bool echoing;

		public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (echoing || projectile.owner != Main.myPlayer || damageDone <= 0)
				return;
			if (!projectile.minion && !projectile.sentry
				&& !projectile.DamageType.CountsAsClass(DamageClass.Summon))
				return;

			Player owner = Main.player[projectile.owner];
			if (!owner.active || !owner.GetModPlayer<TerrapexPlayer>().echoSpindle)
				return;
			if (Main.rand.NextFloat() > EchoSpindle.Chance)
				return;

			echoing = true;
			target.SimpleStrikeNPC((int)(damageDone * EchoSpindle.Share), hit.HitDirection);
			echoing = false;

			for (int i = 0; i < 8; i++)
			{
				Dust d = Dust.NewDustPerfect(target.Center, DustID.Vortex,
					Main.rand.NextVector2Circular(3f, 3f), 110, default, 1f);
				d.noGravity = true;
			}
		}
	}
}
