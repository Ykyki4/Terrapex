using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Reaper;

namespace Terrapex.Content.Items.Weapons.Reaper
{
	/// <summary>
	/// Blood Moon. Dropped by the zombies and drippers that walk on that night - the class's first
	/// weapon you find rather than make. A kill lets a soul out, and the soul goes looking for the
	/// next one; the Reap lets out four at once.
	/// </summary>
	public class Gravedigger : ScytheItem
	{
		public override float Reach => 84f;

		public override float RangeTiles => 12f;

		public override Color SlashColor => new(196, 236, 214);

		/// <summary>
		/// After the cut, a soul climbs out of the ground at the point and goes looking for
		/// someone.
		/// </summary>
		public override void AfterStrike(Player player, Projectile slash, Vector2 aim, bool edge)
			=> Projectile.NewProjectile(slash.GetSource_FromThis(), slash.Center, new Vector2(0f, -4f),
				ModContent.ProjectileType<ReapBolt>(), (int)(slash.damage * 0.5f), slash.knockBack * 0.3f,
				player.whoAmI, ReapBolt.Soul, ReapBolt.FromSwing);

		protected override void SetScytheDefaults()
		{
			Item.width = 40;
			Item.height = 40;
			Item.damage = 14;
			Item.knockBack = 4.5f;
			Item.useTime = 28;
			Item.useAnimation = 28;
			Item.value = Item.sellPrice(silver: 50);
			Item.rare = ItemRarityID.Blue;
		}

		public override void Reap(Player player, IEntitySource source, Vector2 center,
			Vector2 aim, int damage, float knockback)
		{
			ReapSpin(player, source, center, aim, damage, knockback);
			BoltRing(player, source, center, 4, 5f, ReapBolt.Soul, damage, knockback * 0.5f);
		}

		public override void OnScytheHit(Player player, Projectile stroke, NPC target, NPC.HitInfo hit,
			bool edge, bool reap)
		{
			if (target.life <= 0 && !target.SpawnedFromStatue)
				Projectile.NewProjectile(stroke.GetSource_FromThis(), target.Center, -Vector2.UnitY * 3f,
					ModContent.ProjectileType<ReapBolt>(), stroke.damage / 2, 0f, player.whoAmI, ReapBolt.Soul);
		}
	}
}
