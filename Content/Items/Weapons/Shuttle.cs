using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// Plan item #100. A yo-yo that pays out thread: each new target it bites is tied to the
	/// last one, and the line between them keeps cutting after the yo-yo has moved on.
	/// </summary>
	public class Shuttle : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
			ItemID.Sets.Yoyo[Type] = true;
			ItemID.Sets.GamepadExtraRange[Type] = 15;
		}

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 26;
			Item.damage = 96;
			Item.DamageType = DamageClass.MeleeNoSpeed;
			Item.knockBack = 2.5f;
			Item.useTime = 25;
			Item.useAnimation = 25;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.channel = true;
			Item.UseSound = SoundID.Item1;
			Item.value = Item.sellPrice(gold: 22);
			Item.rare = ItemRarityID.Lime;
			Item.shoot = ModContent.ProjectileType<ShuttleYoyo>();
			Item.shootSpeed = 16f;
		}
	}
}
