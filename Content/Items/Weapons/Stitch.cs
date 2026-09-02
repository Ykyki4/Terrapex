using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// Plan item #80. A short blink that leaves a cut along the path it took. It is the tier's
	/// mobility tool as much as its magic weapon, and the damage sits on the *line*, so it
	/// rewards blinking through a group rather than to an empty spot.
	/// </summary>
	public class Stitch : ModItem
	{
		/// <summary>Sixteen tiles. Far enough to cross a fight, short enough not to be a Rod.</summary>
		public const float Reach = 16f * 16f;

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 38;
			Item.height = 38;
			Item.damage = 62;
			Item.DamageType = DamageClass.Magic;
			Item.mana = 14;
			Item.knockBack = 4f;
			Item.useTime = 40;
			Item.useAnimation = 40;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.noMelee = true;
			Item.autoReuse = false;
			Item.UseSound = SoundID.Item8;
			Item.value = Item.sellPrice(gold: 12);
			Item.rare = ItemRarityID.Yellow;
		}

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI != Main.myPlayer)
				return true;

			Vector2 from = player.Center;
			Vector2 aim = Main.MouseWorld - from;
			if (aim.LengthSquared() < 4f)
				return true;
			if (aim.Length() > Reach)
				aim = Vector2.Normalize(aim) * Reach;

			// walk the destination back out of any wall it landed in rather than refusing the
			// cast — a blink that silently does nothing reads as a broken weapon
			Vector2 dest = from + aim;
			for (int i = 0; i < 12; i++)
			{
				if (!Collision.SolidCollision(dest - player.Size * 0.5f, player.width, player.height))
					break;
				dest = Vector2.Lerp(from, dest, 0.82f);
			}

			Vector2 top = dest - new Vector2(player.width * 0.5f, player.height * 0.5f);
			player.Teleport(top, 0);
			NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, player.whoAmI, top.X, top.Y, 0);

			Vector2 dir = dest - from;
			float len = dir.Length();
			if (len > 1f)
			{
				Projectile.NewProjectile(player.GetSource_ItemUse(Item), from, Vector2.Normalize(dir),
					ModContent.ProjectileType<StitchSlash>(), Item.damage, Item.knockBack,
					player.whoAmI, len);
			}
			return true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<VoidCloth>(6)
				.AddIngredient(ItemID.HallowedBar, 12)
				.AddIngredient(ItemID.SoulofFright, 8)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
