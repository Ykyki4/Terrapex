using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Items.Weapons
{
	public class Wedge : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
			// This is the flag that makes vanilla treat the weapon as a spear: it drives the
			// held projectile's rotation and the player's arm. Without it the projectile has to
			// position itself, which is what put the spear at the wrong height.
			ItemID.Sets.Spears[Type] = true;
			// the use sound is tied to use time instead, see UseItem
			ItemID.Sets.SkipsInitialUseSound[Type] = true;
		}

		public override void SetDefaults()
		{
			Item.width = 44;
			Item.height = 44;
			Item.damage = 16;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 6.5f;
			Item.useTime = 26;
			Item.useAnimation = 26;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.autoReuse = true;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.UseSound = SoundID.Item1;
			Item.value = Item.sellPrice(silver: 36);
			Item.rare = ItemRarityID.Blue;
			Item.shoot = ModContent.ProjectileType<WedgeSpear>();
			Item.shootSpeed = 3.5f;
		}

		// autoReuse plus a held projectile needs a guard, or a held mouse button stacks spears
		public override bool CanUseItem(Player player)
			=> player.ownedProjectileCounts[Item.shoot] < 1;

		public override bool? UseItem(Player player)
		{
			if (!Main.dedServ && Item.UseSound.HasValue)
				Terraria.Audio.SoundEngine.PlaySound(Item.UseSound.Value, player.Center);
			return null;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ModContent.ItemType<FissuriteBar>(), 8)
				.AddIngredient(ItemID.Wood, 6)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
