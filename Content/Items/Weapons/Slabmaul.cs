using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// The Breach's melee purchase: the Heave's own Upthrust, sold back to the player.
	///
	/// It swings like a maul, but the reason to own it is that a swing aimed at the floor drives
	/// a plate of stone up out of it — a wall the player places, where the boss placed one at
	/// them. The plate needs ground to come out of, so the weapon is worth most in the cavern
	/// the event happens in and worth least in the air, which is the trade for the reach.
	/// </summary>
	public class Slabmaul : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 46;
			Item.height = 46;
			Item.damage = 56;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 9f;
			Item.useTime = 34;
			Item.useAnimation = 34;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.UseSound = SoundID.Item1;
			Item.value = Item.sellPrice(gold: 4);
			Item.rare = ItemRarityID.Pink;
			Item.scale = 1.2f;
			Item.shoot = ModContent.ProjectileType<FriendlySlab>();
			Item.shootSpeed = 1f;
		}

		/// <summary>
		/// The plate goes where the cursor is, if there is a floor under it and the player can
		/// reach. Two at a time, so the weapon builds a short wall instead of a fence.
		/// </summary>
		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			if (player.ownedProjectileCounts[type] >= 2)
				return false;

			Vector2 aim = Main.MouseWorld;
			if (Vector2.Distance(aim, player.Center) > 260f)
				aim = player.Center + Vector2.Normalize(aim - player.Center) * 260f;

			// the floor is found by scanning tiles, the way the Lid does it: a velocity test looks
			// right and quietly fails on slopes, steps and platforms
			int tx = (int)(aim.X / 16f);
			int ty = (int)(aim.Y / 16f);
			int floor = -1;
			for (int k = -4; k <= 8 && floor < 0; k++)
			{
				Tile tile = Framing.GetTileSafely(tx, ty + k);
				if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType])
					floor = ty + k;
			}
			if (floor < 0)
				return false;

			Projectile.NewProjectile(source, new Vector2(tx * 16 + 8, floor * 16f), Vector2.Zero,
				type, (int)(damage * 0.5f), knockback * 0.6f, player.whoAmI);
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70 with { Volume = 0.5f }, player.Center);
			return false;
		}

		public override void MeleeEffects(Player player, Rectangle hitbox)
		{
			if (Main.rand.NextBool(4))
			{
				Dust d = Dust.NewDustDirect(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height,
					DustID.Stone, 0f, 0f, 110, default, 1.15f);
				d.velocity *= 0.4f;
			}
		}

		public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
			=> target.AddBuff(ModContent.BuffType<Cracked>(), 240);
	}
}
