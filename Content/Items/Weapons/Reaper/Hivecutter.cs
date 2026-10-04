using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Reaper;

namespace Terrapex.Content.Items.Weapons.Reaper
{
	/// <summary>
	/// The Queen Bee's scythe. Edge hits shake a bee loose, and the Reap empties the hive: eight of
	/// them. The bees are vanilla's, with the player's own bee type and damage, so the Hive Pack works on
	/// them as it should.
	/// </summary>
	public class Hivecutter : ScytheItem
	{
		public override float Reach => 88f;

		public override float RangeTiles => 13f;

		public override Color SlashColor => new(255, 214, 90);

		/// <summary>
		/// After the cut, the point is splashed with honey and three bees get out of it. They are
		/// the player's own bees, so the Hive Pack upgrades them.
		/// </summary>
		public override void AfterStrike(Player player, Projectile slash, Vector2 aim, bool edge)
		{
			for (int i = 0; i < 10; i++)
				Dust.NewDustPerfect(slash.Center, DustID.Honey2, Main.rand.NextVector2Circular(3f, 3f), 80, default, 1.1f);
			for (int i = 0; i < 3; i++)
				Projectile.NewProjectile(slash.GetSource_FromThis(), slash.Center, Main.rand.NextVector2Circular(4f, 4f),
					player.beeType(), player.beeDamage(slash.damage / 2), player.beeKB(0f), player.whoAmI);
		}

		protected override void SetScytheDefaults()
		{
			Item.width = 40;
			Item.height = 40;
			Item.damage = 22;
			Item.knockBack = 4.5f;
			Item.useTime = 25;
			Item.useAnimation = 25;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Orange;
		}

		public override void Reap(Player player, IEntitySource source, Vector2 center,
			Vector2 aim, int damage, float knockback)
		{
			ReapSpin(player, source, center, aim, damage, knockback);
			for (int i = 0; i < 8; i++)
				Projectile.NewProjectile(source, center, Main.rand.NextVector2Circular(6f, 6f), player.beeType(),
					player.beeDamage(damage / 3), player.beeKB(0f), player.whoAmI);
		}

		public override void OnScytheHit(Player player, Projectile stroke, NPC target, NPC.HitInfo hit,
			bool edge, bool reap)
		{
			if (edge && !reap)
				Projectile.NewProjectile(stroke.GetSource_FromThis(), target.Center, Main.rand.NextVector2Circular(4f, 4f),
					player.beeType(), player.beeDamage(stroke.damage / 3), player.beeKB(0f), player.whoAmI);
		}
	}
}
