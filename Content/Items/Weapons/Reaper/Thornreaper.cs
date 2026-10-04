using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Reaper;

namespace Terrapex.Content.Items.Weapons.Reaper
{
	/// <summary>
	/// The jungle's scythe: spores, stingers and vine. Every hit poisons, edge or not, which makes it
	/// the one pre-hardmode scythe that rewards a long fight more than a crowd. The Reap throws a ring
	/// of thorns.
	/// </summary>
	public class Thornreaper : ScytheItem
	{
		public override float Reach => 96f;

		public override float RangeTiles => 15f;

		public override Color SlashColor => new(126, 200, 80);

		protected override void SetScytheDefaults()
		{
			Item.width = 40;
			Item.height = 40;
			Item.damage = 25;
			Item.knockBack = 5f;
			Item.useTime = 27;
			Item.useAnimation = 27;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Orange;
		}

		public override void Reap(Player player, IEntitySource source, Vector2 center,
			Vector2 aim, int damage, float knockback)
		{
			ReapSpin(player, source, center, aim, damage, knockback);
			BoltRing(player, source, center, 8, 9f, ReapBolt.Thorn, damage, knockback * 0.5f);
		}

		public override void OnScytheHit(Player player, Projectile stroke, NPC target, NPC.HitInfo hit,
			bool edge, bool reap)
			=> target.AddBuff(BuffID.Poisoned, 240);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.JungleSpores, 12)
				.AddIngredient(ItemID.Stinger, 6)
				.AddIngredient(ItemID.Vine, 2)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
