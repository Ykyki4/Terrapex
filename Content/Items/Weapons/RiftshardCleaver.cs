using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Items.Weapons
{
	public class RiftshardCleaver : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
		}

		public override void SetDefaults()
		{
			Item.width = 54;
			Item.height = 54;
			Item.damage = 52;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 6.5f;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.UseSound = SoundID.Item1;
			Item.value = Item.sellPrice(gold: 8);
			Item.rare = ItemRarityID.Pink;
			Item.scale = 1.05f;
			Item.shoot = ModContent.ProjectileType<RiftWave>();
			Item.shootSpeed = 13f;
		}

		// Every swing tears one off. This is the only sword projectile in the mod that keeps its
		// damage at range, so it is what makes the drop feel like a boss weapon.
		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			Projectile.NewProjectile(source, position, velocity,
				type, (int)(damage * 0.4f), knockback * 0.6f, player.whoAmI);
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Item60 with { Volume = 0.5f }, position);
			return false;
		}

		public override void MeleeEffects(Player player, Rectangle hitbox)
		{
			if (Main.rand.NextBool(3))
			{
				Dust d = Dust.NewDustDirect(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height,
					Main.rand.NextBool() ? DustID.PurpleTorch : DustID.PinkTorch, 0f, 0f, 100, default, 1.1f);
				d.noGravity = true;
				d.velocity *= 0.3f;
			}
		}

		public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
		{
			for (int i = 0; i < 6; i++)
			{
				Dust d = Dust.NewDustPerfect(target.Center, DustID.PurpleTorch,
					Main.rand.NextVector2Circular(3f, 3f), 100, default, 1.2f);
				d.noGravity = true;
			}
		}

		private static Texture2D GlowTexture => ModContent.Request<Texture2D>(
			"Terrapex/Content/Items/Weapons/RiftshardCleaver_Glow").Value;

		public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor,
			float rotation, float scale, int whoAmI)
		{
			Texture2D glow = GlowTexture;
			Vector2 pos = new Vector2(
				Item.position.X - Main.screenPosition.X + Item.width * 0.5f,
				Item.position.Y - Main.screenPosition.Y + Item.height - glow.Height * 0.5f + 2f);
			spriteBatch.Draw(glow, pos, null, Color.White, rotation, glow.Size() * 0.5f, scale,
				SpriteEffects.None, 0f);
		}

		public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
			Color drawColor, Color itemColor, Vector2 origin, float scale)
		{
			spriteBatch.Draw(GlowTexture, position, frame, Color.White, 0f, origin, scale,
				SpriteEffects.None, 0f);
		}
	}
}
