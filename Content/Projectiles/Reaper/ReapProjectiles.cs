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

	/// <summary>
	/// The Shuttlecut's thread: the player's own FriendlyThread, on the reaper's class. Strung by
	/// <see cref="Between"/> from a cut to an enemy; it stands where it was strung and cuts what
	/// crosses it.
	/// </summary>
	public class ReapThread : FriendlyThread
	{
		public const int Life = 90;

		public override string Texture => "Terraria/Images/Projectile_1";

		public override void SetDefaults()
		{
			base.SetDefaults();
			Projectile.DamageType = ModContent.GetInstance<ReapingDamage>();
			Projectile.timeLeft = Life;
		}

		public static new void Between(Terraria.DataStructures.IEntitySource source, Microsoft.Xna.Framework.Vector2 a,
			Microsoft.Xna.Framework.Vector2 b, int damage, int owner)
		{
			Microsoft.Xna.Framework.Vector2 dir = b - a;
			float half = dir.Length() * 0.5f;
			if (half < 4f)
				return;
			Terraria.Projectile.NewProjectile(source, (a + b) * 0.5f, Microsoft.Xna.Framework.Vector2.Normalize(dir),
				ModContent.ProjectileType<ReapThread>(), damage, 0f, owner, half);
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
