using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Reaper;

namespace Terrapex.Content.Items.Weapons.Reaper
{
	/// <summary>
	/// Found in ice chests, and carried by ice slimes and Eskimo zombies so a world generated before
	/// it existed can still find one. Small and quick; its edge burns with frost, and the Reap throws
	/// a ring of icicles.
	/// </summary>
	public class FrostbiteSickle : ScytheItem
	{
		public override float Reach => 72f;

		public override float RangeTiles => 10f;

		public override Color SlashColor => new(176, 232, 255);

		/// <summary>
		/// After the cut, three icicles burst out of the point in a fan, carrying on the way the
		/// stroke was going; each sticks in and shatters a moment later.
		/// </summary>
		public override void AfterStrike(Player player, Projectile slash, Vector2 aim, bool edge)
		{
			for (int i = -1; i <= 1; i++)
				Projectile.NewProjectile(slash.GetSource_FromThis(), slash.Center,
					aim.RotatedBy(i * 0.4f) * FrostIcicle.Speed * 0.8f, ModContent.ProjectileType<FrostIcicle>(),
					(int)(slash.damage * 0.4f), slash.knockBack * 0.4f, player.whoAmI);
		}

		protected override void SetScytheDefaults()
		{
			Item.width = 32;
			Item.height = 32;
			Item.damage = 13;
			Item.knockBack = 3.5f;
			Item.useTime = 22;
			Item.useAnimation = 22;
			Item.value = Item.sellPrice(silver: 50);
			Item.rare = ItemRarityID.Blue;
		}

		public override void Reap(Player player, IEntitySource source, Vector2 center,
			Vector2 aim, int damage, float knockback)
		{
			ReapSpin(player, source, center, aim, damage, knockback);
			BoltRing(player, source, center, 6, 8f, ReapBolt.Ice, damage, knockback * 0.5f);
		}

		public override void OnScytheHit(Player player, Projectile stroke, NPC target, NPC.HitInfo hit,
			bool edge, bool reap)
		{
			if (edge)
				target.AddBuff(BuffID.Frostburn, 180);
		}
	}
}
