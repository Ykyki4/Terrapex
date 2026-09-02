using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// Plan item #62. The longest whip in the mod: a plate on the end of a tether, swung the
	/// way the Keeper throws one.
	/// </summary>
	public class OrbitLash : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 38;
			Item.height = 38;
			Item.damage = 33;
			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.knockBack = 3f;
			Item.useTime = 34;
			Item.useAnimation = 34;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.shoot = ModContent.ProjectileType<OrbitLashWhip>();
			Item.shootSpeed = 5.2f;
			Item.value = Item.sellPrice(gold: 5);
			Item.rare = ItemRarityID.Pink;
			Item.UseSound = SoundID.Item152;
		}
	}
}
