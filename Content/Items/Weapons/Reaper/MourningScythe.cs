using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles.Reaper;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons.Reaper
{
	/// <summary>
	/// Mourning Wood's scythe, from the Pumpkin Moon: a gnarled branch with a lantern pumpkin
	/// grinning at the neck. After the cut, two embers leap out of the point and set what they
	/// touch alight.
	/// </summary>
	public class MourningScythe : ScytheItem
	{
		public override float Reach => 112f;

		public override float RangeTiles => 21f;

		public override Color SlashColor => new(255, 150, 60);

		protected override void SetScytheDefaults()
		{
			Item.width = 52;
			Item.height = 52;
			Item.damage = 100;
			Item.knockBack = 6f;
			Item.useTime = 24;
			Item.useAnimation = 24;
			Item.value = Item.sellPrice(gold: 8);
			Item.rare = ItemRarityID.Yellow;
		}

		/// <summary>Two embers out of the point, either side of the stroke.</summary>
		public override void AfterStrike(Player player, Projectile slash, Vector2 aim, bool edge)
		{
			for (int i = -1; i <= 1; i += 2)
				Projectile.NewProjectile(slash.GetSource_FromThis(), slash.Center,
					aim.RotatedBy(i * 0.5f) * 10f, ModContent.ProjectileType<ReapBolt>(),
					(int)(slash.damage * 0.45f), slash.knockBack * 0.3f, player.whoAmI, ReapBolt.Fire, ReapBolt.FromSwing);
		}

		public override void OnScytheHit(Player player, Projectile stroke, NPC target, NPC.HitInfo hit,
			bool edge, bool reap)
			=> target.AddBuff(BuffID.OnFire3, 180);
	}
}
