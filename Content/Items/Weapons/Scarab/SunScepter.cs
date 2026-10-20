using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Scarab;

namespace Terrapex.Content.Items.Weapons.Scarab
{
	/// <summary>
	/// The magic drop: a was-sceptre, forked foot and a beast's head on top. It lobs a sun bolt that
	/// falls in an arc, and where it lands a pillar of sand goes up - the Sultan's move, aimed at the
	/// floor under the enemy instead of under you. A hit on an enemy raises the pillar under it.
	/// </summary>
	public class SunScepter : ModItem
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
			Item.damage = 25;
			Item.DamageType = DamageClass.Magic;
			Item.mana = 9;
			Item.knockBack = 3f;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.noMelee = true;
			Item.autoReuse = true;
			Item.UseSound = SoundID.Item20;
			Item.shoot = ModContent.ProjectileType<ScepterBolt>();
			Item.shootSpeed = 10f;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Green;
		}
	}
}
