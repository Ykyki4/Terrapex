using Terraria.ModLoader;
using Terrapex.Common.Reaper;

namespace Terrapex.Content.Projectiles.Reaper
{
	/// <summary>
	/// The Fissurite Scythe's Reap ring. The Scree Tome's stone, thrown by a reaper: same arc, same
	/// break, own damage class - a second sprite for one rock would only drift from the first.
	/// </summary>
	public class ReapStone : ScreeStone
	{
		public override string Texture => "Terrapex/Content/Projectiles/ScreeStone";

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.DamageType = ModContent.GetInstance<ReapingDamage>();
		}
	}

	/// <summary>The Glass Reaper's Reap: the T1 sword's crescent, on the reaper's class.</summary>
	public class ReapGlass : GlassEdge
	{
		public override string Texture => "Terrapex/Content/Projectiles/GlassEdge";

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.DamageType = ModContent.GetInstance<ReapingDamage>();
			Projectile.penetrate = 5;
			Projectile.timeLeft = 60;
		}
	}
}
