using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// Plan item #95. Every swing leaves a wall of thread standing where it passed, across the
	/// line of the swing rather than along it. The sword fights for territory: weak swung at
	/// one thing, strong swung at a doorway.
	/// </summary>
	public class Warp : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 52;
			Item.height = 52;
			Item.damage = 132;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 6.5f;
			Item.useTime = 26;
			Item.useAnimation = 26;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.scale = 1.2f;
			Item.UseSound = SoundID.Item1;
			Item.value = Item.sellPrice(gold: 24);
			Item.rare = ItemRarityID.Lime;
			Item.shoot = ModContent.ProjectileType<FriendlyThread>();
			Item.shootSpeed = 1f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			// the wall stands ACROSS the swing, which is what makes it a wall and not a beam
			Vector2 aim = Vector2.Normalize(velocity);
			Vector2 at = player.MountedCenter + aim * 96f;
			Vector2 across = aim.RotatedBy(MathHelper.PiOver2) * 84f;
			FriendlyThread.Between(source, at - across, at + across, (int)(damage * 0.55f),
				player.whoAmI, 120);
			return false;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<EchoAlloy>(14)
				.AddIngredient(ItemID.ChlorophyteBar, 8)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
