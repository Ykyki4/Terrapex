using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;
using Terrapex.Content.Projectiles.Shellback;

namespace Terrapex.Content.Items.Weapons.Shellback
{
	/// <summary>
	/// The Old Shellback's summon drop: a crab pot off the wreck, with something still living in
	/// it. Each use lets out a hermit crab that scuttles along the ground and pinches.
	/// </summary>
	public class RustedCrabPot : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
			ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
			ItemID.Sets.LockOnIgnoresCollision[Type] = true;
		}

		public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 30;
			Item.damage = 12;
			Item.DamageType = DamageClass.Summon;
			Item.mana = 10;
			Item.knockBack = 2.5f;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item44;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Blue;
			Item.buffType = ModContent.BuffType<PotCrabBuff>();
			Item.shoot = ModContent.ProjectileType<PotCrabMinion>();
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			player.AddBuff(Item.buffType, 2);
			var proj = Projectile.NewProjectileDirect(source, player.Center, Vector2.Zero,
				type, damage, knockback, player.whoAmI);
			proj.originalDamage = Item.damage;
			return false;
		}
	}
}
