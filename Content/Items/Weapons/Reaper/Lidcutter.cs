using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Reaper;

namespace Terrapex.Content.Items.Weapons.Reaper
{
	/// <summary>
	/// The Dormant Eye's scythe: a sliver of its lid, still trying to close. Its edge cracks what it
	/// cuts - the mod's oldest verb, the Cracked debuff - and the Reap cracks everything it touches for
	/// twice as long. Crack it first, then hit it, and the scythe does both.
	/// </summary>
	public class Lidcutter : ScytheItem
	{
		public override float Reach => 92f;

		public override float RangeTiles => 13f;

		public override Color SlashColor => new(201, 189, 209);

		/// <summary>
		/// After the cut, a disc of the lid flies on from the point, turns, and comes back to the
		/// hand - through everything standing between, cracking all of it.
		/// </summary>
		public override void AfterStrike(Player player, Projectile slash, Vector2 aim, bool edge)
			=> Projectile.NewProjectile(slash.GetSource_FromThis(), slash.Center, aim * LidDisc.Speed,
				ModContent.ProjectileType<LidDisc>(), (int)(slash.damage * 0.6f), slash.knockBack * 0.5f,
				player.whoAmI, 0f, 12f);

		protected override void SetScytheDefaults()
		{
			Item.width = 40;
			Item.height = 40;
			Item.damage = 20;
			Item.knockBack = 5f;
			Item.useTime = 26;
			Item.useAnimation = 26;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Green;
		}

		public override void OnScytheHit(Player player, Projectile stroke, NPC target, NPC.HitInfo hit,
			bool edge, bool reap)
		{
			if (edge)
				target.AddBuff(ModContent.BuffType<Buffs.Cracked>(), reap ? 360 : 180);
		}
	}
}
