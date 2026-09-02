using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// Plan item #38. The tier's melee entry, and the first thing the player makes out of
	/// the dungeon: a femur split lengthwise and packed with riftglass.
	/// </summary>
	public class Bonefissure : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 56;
			Item.height = 56;
			Item.damage = 42;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 5.5f;
			Item.useTime = 22;
			Item.useAnimation = 22;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.UseSound = SoundID.Item1;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Orange;
			Item.scale = 1.1f;
			Item.crit = 6;
			Item.shoot = ModContent.ProjectileType<BoneSplinter>();
			Item.shootSpeed = 10f;
		}

		// Two splinters per swing at half damage each, spread just wide enough that a
		// wall sends them back on different lines instead of stacking.
		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			for (int i = -1; i <= 1; i += 2)
			{
				Projectile.NewProjectile(source, position, velocity.RotatedBy(i * 0.13f),
					type, (int)(damage * 0.5f), knockback * 0.5f, player.whoAmI);
			}
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.55f }, position);
			return false;
		}

		public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
			=> target.AddBuff(ModContent.BuffType<Cracked>(), 180);

		public override void MeleeEffects(Player player, Rectangle hitbox)
		{
			if (Main.rand.NextBool(4))
			{
				Dust d = Dust.NewDustDirect(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height,
					Main.rand.NextBool() ? DustID.Bone : DustID.PurpleTorch, 0f, 0f, 120, default, 1f);
				d.noGravity = true;
				d.velocity *= 0.35f;
			}
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.Bone, 30)
				.AddIngredient<Riftglass>(14)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
