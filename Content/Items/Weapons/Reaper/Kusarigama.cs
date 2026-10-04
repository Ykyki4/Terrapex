using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Reaper;

namespace Terrapex.Content.Items.Weapons.Reaper
{
	/// <summary>
	/// T2, sold by the Ronin. A sickle on a chain: its Reap throws the blade at the cursor, hooks
	/// the first thing it bites and drags it back into reach - which for a class that lives at
	/// blade's length is the one move that lets you choose the distance instead of finding it.
	/// </summary>
	public class Kusarigama : ScytheItem
	{
		public override float Reach => 100f;

		public override float RangeTiles => 14f;

		public override Color SlashColor => new(242, 96, 63);

		/// <summary>
		/// After the cut, the chain is seen to have reached the point: the sickle is pulled back
		/// out of it into the hand, cutting everything on the way. The hook is kept for the Reap.
		/// </summary>
		public override void AfterStrike(Player player, Projectile slash, Vector2 aim, bool edge)
			=> Projectile.NewProjectile(slash.GetSource_FromThis(), slash.Center, -aim * 6f,
				ModContent.ProjectileType<KusarigamaHead>(), (int)(slash.damage * 0.7f), slash.knockBack * 0.5f,
				player.whoAmI, KusarigamaHead.Back, KusarigamaHead.QuickThrow);

		protected override void SetScytheDefaults()
		{
			Item.width = 44;
			Item.height = 44;
			Item.damage = 34;
			Item.knockBack = 5.5f;
			Item.useTime = 24;
			Item.useAnimation = 24;
			Item.value = Item.buyPrice(gold: 8);
			Item.rare = ItemRarityID.Orange;
		}

		public override void Reap(Player player, IEntitySource source, Vector2 center,
			Vector2 aim, int damage, float knockback)
		{
			Projectile.NewProjectile(source, center, aim * KusarigamaHead.Speed,
				ModContent.ProjectileType<KusarigamaHead>(), (int)(damage * 2.2f), knockback, player.whoAmI);
		}
	}
}
