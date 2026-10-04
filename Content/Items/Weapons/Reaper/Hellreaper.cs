using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Reaper;

namespace Terrapex.Content.Items.Weapons.Reaper
{
	/// <summary>
	/// Hellstone, the last thing a reaper forges before the Wall. The longest blade in pre-hardmode, and
	/// every hit sets the target alight. The Reap is the first that leaves the reaper's hands for good:
	/// three burning crescents thrown at the cursor.
	/// </summary>
	public class Hellreaper : ScytheItem
	{
		public override float Reach => 110f;

		public override float RangeTiles => 17f;

		public override Color SlashColor => new(255, 144, 64);

		protected override void SetScytheDefaults()
		{
			Item.width = 48;
			Item.height = 48;
			Item.damage = 36;
			Item.knockBack = 6f;
			Item.useTime = 28;
			Item.useAnimation = 28;
			Item.value = Item.sellPrice(gold: 1, silver: 50);
			Item.rare = ItemRarityID.Orange;
		}

		public override void Reap(Player player, IEntitySource source, Vector2 center,
			Vector2 aim, int damage, float knockback)
		{
			ReapSpin(player, source, center, aim, damage, knockback);
			BoltFan(player, source, center, aim, 3, 0.5f, 14f, ReapBolt.Fire, (int)(damage * 1.4f), knockback);
		}

		public override void OnScytheHit(Player player, Projectile stroke, NPC target, NPC.HitInfo hit,
			bool edge, bool reap)
			=> target.AddBuff(BuffID.OnFire, 180);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.HellstoneBar, 18)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
