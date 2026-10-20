using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Shellback;

namespace Terrapex.Content.Items.Weapons.Shellback
{
	/// <summary>
	/// The Old Shellback's magic drop: a conch blown like a horn. It breathes out one fat bubble
	/// that drifts slowly and swells; when it touches anything, or runs out of breath, it bursts
	/// into a ring of brine droplets that fall. Slow to arrive, wide when it does.
	/// </summary>
	public class BrineConch : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 28;
			Item.damage = 20;
			Item.DamageType = DamageClass.Magic;
			Item.mana = 8;
			Item.knockBack = 3f;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item85;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Blue;
			Item.shoot = ModContent.ProjectileType<ConchBubble>();
			Item.shootSpeed = 5f;
		}

		public override Vector2? HoldoutOffset() => new Vector2(-2f, 2f);
	}
}
