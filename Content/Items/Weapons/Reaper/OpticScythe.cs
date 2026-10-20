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
	/// The Twins' scythe: two blades on one haft, a red eye and a green one. Every cut has a twin -
	/// a second cut lands mirrored through the reaper, on the other side, at three fifths of the
	/// damage. The echo does not echo again.
	/// </summary>
	public class OpticScythe : ScytheItem
	{
		public override float Reach => 106f;

		public override float RangeTiles => 19f;

		public override Color SlashColor => new(255, 110, 110);

		protected override void SetScytheDefaults()
		{
			Item.width = 52;
			Item.height = 52;
			Item.damage = 62;
			Item.knockBack = 5f;
			Item.useTime = 24;
			Item.useAnimation = 24;
			Item.value = Item.sellPrice(gold: 5);
			Item.rare = ItemRarityID.LightPurple;
		}

		/// <summary>The twin cut, mirrored through the player.</summary>
		public override void AfterStrike(Player player, Projectile slash, Vector2 aim, bool edge)
		{
			Vector2 mirror = player.MountedCenter * 2f - slash.Center;
			Projectile.NewProjectile(slash.GetSource_FromThis(), mirror, Vector2.Zero,
				ModContent.ProjectileType<ReapingSlash>(), (int)(slash.damage * 0.6f), slash.knockBack,
				player.whoAmI, edge ? 1f : 0f, slash.ai[1] + MathHelper.Pi, ReapingSlash.Echo * System.Math.Sign(slash.ai[2] == 0f ? 1f : slash.ai[2]));
		}
	}
}
