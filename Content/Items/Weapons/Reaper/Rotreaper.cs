using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Reaper;

namespace Terrapex.Content.Items.Weapons.Reaper
{
	/// <summary>
	/// Corruption: demonite and shadow scale. What it kills does not fall so much as come apart -
	/// every kill leaves a cloud of rot hanging where the body was, and the Reap throws four of them.
	/// </summary>
	public class Rotreaper : ScytheItem
	{
		public override float Reach => 90f;

		public override float RangeTiles => 13f;

		public override Color SlashColor => new(155, 111, 194);

		protected override void SetScytheDefaults()
		{
			Item.width = 40;
			Item.height = 40;
			Item.damage = 19;
			Item.knockBack = 5f;
			Item.useTime = 27;
			Item.useAnimation = 27;
			Item.value = Item.sellPrice(silver: 60);
			Item.rare = ItemRarityID.Blue;
		}

		public override void Reap(Player player, IEntitySource source, Vector2 center,
			Vector2 aim, int damage, float knockback)
		{
			ReapSpin(player, source, center, aim, damage, knockback);
			BoltFan(player, source, center, aim, 4, 1.2f, 6f, ReapBolt.Rot, damage / 2, 0f);
		}

		public override void OnScytheHit(Player player, Projectile stroke, NPC target, NPC.HitInfo hit,
			bool edge, bool reap)
		{
			if (target.life <= 0 && !target.SpawnedFromStatue)
				Projectile.NewProjectile(stroke.GetSource_FromThis(), target.Center, Vector2.Zero,
					ModContent.ProjectileType<ReapBolt>(), stroke.damage / 3, 0f, player.whoAmI, ReapBolt.Rot);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.DemoniteBar, 10)
				.AddIngredient(ItemID.ShadowScale, 6)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
