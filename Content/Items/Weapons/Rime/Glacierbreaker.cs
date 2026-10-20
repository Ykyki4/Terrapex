using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Rime;

namespace Terrapex.Content.Items.Weapons.Rime
{
	/// <summary>
	/// The Rime's melee drop: a block of glacier ice on a haft, swung like a maul. A blow that
	/// lands frostburns and knocks three splinters off the head, out past the target - the boss's
	/// thrown block, shattering on someone else for once. The splinters come once per swing.
	/// </summary>
	public class Glacierbreaker : ModItem
	{
		public const int Splinters = 3;
		public const float SplinterShare = 0.4f;

		private uint lastShatter;

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 52;
			Item.height = 52;
			Item.damage = 38;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 7.5f;
			Item.useTime = 34;
			Item.useAnimation = 34;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.UseSound = SoundID.Item1;
			Item.scale = 1.1f;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Orange;
		}

		public override void MeleeEffects(Player player, Rectangle hitbox)
		{
			if (Main.rand.NextBool(3))
				Dust.NewDustDirect(hitbox.TopLeft(), hitbox.Width, hitbox.Height, DustID.Snow, 0f, 0f, 100, default, 1f).noGravity = true;
		}

		public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.Frostburn, 180);
			if (Main.GameUpdateCount - lastShatter < (uint)Item.useAnimation)
				return;
			lastShatter = Main.GameUpdateCount;
			Vector2 away = (target.Center - player.Center).SafeNormalize(Vector2.UnitX * player.direction);
			for (int i = 0; i < Splinters; i++)
			{
				float t = i / (Splinters - 1f) - 0.5f;
				Projectile.NewProjectile(player.GetSource_ItemUse(Item), target.Center,
					away.RotatedBy(t * 1.1f) * 7f - Vector2.UnitY * 2f, ModContent.ProjectileType<RimeSplinter>(),
					(int)(damageDone * SplinterShare), 2f, player.whoAmI, 1f);
			}
		}
	}
}
