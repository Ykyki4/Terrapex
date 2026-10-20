using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Weapons.Reaper;
using Terrapex.Content.Projectiles.Shellback;

namespace Terrapex.Content.Items.Weapons.Shellback
{
	/// <summary>
	/// The Old Shellback's scythe: a blade cut from a ship's rib, barnacles still on its back.
	/// T1. After the cut a whirlpool opens on the point for a little under a second, dragging what
	/// stands near it into the middle and wearing at it - a reach weapon that gathers the crowd
	/// its next cut wants.
	/// </summary>
	public class Undertow : ScytheItem
	{
		public const float WhirlShare = 0.3f;

		public override float Reach => 84f;

		public override float RangeTiles => 13f;

		public override Color SlashColor => new(120, 220, 210);

		public override void AfterStrike(Player player, Projectile slash, Vector2 aim, bool edge)
			=> Projectile.NewProjectile(slash.GetSource_FromThis(), slash.Center, Vector2.Zero,
				ModContent.ProjectileType<UndertowWhirl>(), (int)(slash.damage * WhirlShare), 0f, player.whoAmI);

		protected override void SetScytheDefaults()
		{
			Item.width = 44;
			Item.height = 44;
			Item.damage = 20;
			Item.knockBack = 4.5f;
			Item.useTime = 26;
			Item.useAnimation = 26;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Blue;
		}
	}
}
