using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Weapons.Reaper;
using Terrapex.Content.Projectiles.Rime;

namespace Terrapex.Content.Items.Weapons.Rime
{
	/// <summary>
	/// The Rime's scythe, T2: a jagged blade of glacier ice on a pale birch snath. After its cut,
	/// an icicle glints over the point and drops through it - three on the edge. The Reap brings
	/// a row of six down round the reaper. The Rime's icefall, with the reaper choosing where.
	/// </summary>
	public class FrostfellScythe : ScytheItem
	{
		public const float IcicleShare = 0.5f;
		public const float DropHeight = 240f;

		public override float Reach => 94f;

		public override float RangeTiles => 14f;

		public override Color SlashColor => new(160, 230, 255);

		protected override void SetScytheDefaults()
		{
			Item.width = 48;
			Item.height = 48;
			Item.damage = 33;
			Item.knockBack = 5f;
			Item.useTime = 26;
			Item.useAnimation = 26;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Orange;
		}

		public override void AfterStrike(Player player, Projectile slash, Vector2 aim, bool edge)
		{
			int count = edge ? 3 : 1;
			for (int i = 0; i < count; i++)
			{
				float x = slash.Center.X + (i - (count - 1) / 2f) * 30f;
				Drop(slash.GetSource_FromThis(), player, new Vector2(x, slash.Center.Y),
					(int)(slash.damage * IcicleShare), slash.knockBack * 0.4f);
			}
		}

		public override void Reap(Player player, IEntitySource source, Vector2 center, Vector2 aim, int damage, float knockback)
		{
			ReapSpin(player, source, center, aim, damage, knockback);
			foreach (float x in new[] { -170f, -110f, -50f, 50f, 110f, 170f })
				Drop(source, player, center + new Vector2(x, 10f), (int)(damage * 0.6f), knockback * 0.5f);
		}

		private static void Drop(IEntitySource source, Player player, Vector2 point, int damage, float knockback)
			=> Projectile.NewProjectile(source, point - new Vector2(0f, DropHeight), Vector2.Zero,
				ModContent.ProjectileType<FrostfellIcicle>(), damage, knockback, player.whoAmI, 0f, point.Y);

		public override void OnScytheHit(Player player, Projectile stroke, NPC target, NPC.HitInfo hit, bool edge, bool reap)
		{
			if (edge || reap)
				target.AddBuff(BuffID.Frostburn, 180);
		}
	}
}
