using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Rime;

namespace Terrapex.Content.Items.Weapons.Rime
{
	/// <summary>
	/// The Rime's magic drop: a birch staff with a snowball held in an ice claw. It lobs the
	/// snowball, which lands and rolls on along the floor, growing as it goes - up to nearly
	/// twice the size and twice the damage - through six enemies or until a wall stops it.
	/// </summary>
	public class SnowcapStaff : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
			Item.staff[Type] = true;
		}

		public override void SetDefaults()
		{
			Item.width = 44;
			Item.height = 44;
			Item.damage = 32;
			Item.DamageType = DamageClass.Magic;
			Item.mana = 11;
			Item.knockBack = 5f;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item28;
			Item.shoot = ModContent.ProjectileType<RollingSnowball>();
			Item.shootSpeed = 8f;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Orange;
		}
	}
}
