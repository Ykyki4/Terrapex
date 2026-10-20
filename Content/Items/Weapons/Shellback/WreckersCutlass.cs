using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Shellback;

namespace Terrapex.Content.Items.Weapons.Shellback
{
	/// <summary>
	/// The Old Shellback's melee drop: a boarding cutlass grown over with barnacles. Every blow
	/// that lands knocks two of them loose, and they tumble off the target as shrapnel - a sword
	/// that pays for hitting crowds packed close.
	/// </summary>
	public class WreckersCutlass : ModItem
	{
		public const int Shrapnel = 2;
		public const float ShrapnelShare = 0.35f;

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 40;
			Item.height = 40;
			Item.damage = 21;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 4.5f;
			Item.useTime = 19;
			Item.useAnimation = 19;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.UseSound = SoundID.Item1;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Blue;
			Item.scale = 1.1f;
		}

		public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
		{
			int damage = System.Math.Max(1, (int)(player.GetWeaponDamage(Item) * ShrapnelShare));
			for (int i = 0; i < Shrapnel; i++)
			{
				Vector2 v = new(Main.rand.NextFloat(-3.5f, 3.5f), Main.rand.NextFloat(-6f, -4f));
				Projectile.NewProjectile(player.GetSource_OnHit(target), target.Center, v,
					ModContent.ProjectileType<BarnacleShrapnel>(), damage, 1f, player.whoAmI);
			}
		}
	}
}
