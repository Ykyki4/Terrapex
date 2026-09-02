using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Terrapex.Content.Buffs;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// Plan item #21. A slab of the lid swung as a sword: slow, heavy, and it cracks
	/// what it hits, so the tier's own debuff keeps working without the fissurite set.
	/// </summary>
	public class Lid : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 44;
			Item.height = 44;
			Item.damage = 31;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 8.5f;
			Item.useTime = 32;
			Item.useAnimation = 32;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.UseSound = SoundID.Item1;
			Item.value = Item.sellPrice(gold: 1);
			Item.rare = ItemRarityID.Green;
			Item.scale = 1.15f;
			Item.shoot = ModContent.ProjectileType<StoneShockwave>();
			Item.shootSpeed = 7f;
		}

		// The wave only exists if the swing had a floor to land on. Swinging in mid-air gets you
		// the slab and nothing else, which is the price of the reach.
		//
		// The floor is found by scanning tiles, NOT by testing player.velocity.Y == 0. That test
		// looks right and quietly fails: standing on a slope, walking off a step or standing on a
		// platform all leave a non-zero vertical velocity at the moment Shoot runs, so the wave
		// simply never spawned.
		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			int tx = (int)(player.Bottom.X / 16f);
			int ty = (int)(player.Bottom.Y / 16f);
			int floor = -1;

			for (int k = 0; k <= 3 && floor < 0; k++)
			{
				Tile tile = Framing.GetTileSafely(tx, ty + k);
				if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType])
					floor = ty + k;
			}
			if (floor < 0)
				return false;

			Vector2 spawn = new Vector2(player.Center.X + player.direction * 20f, floor * 16f - 11f);
			Projectile.NewProjectile(source, spawn, new Vector2(player.direction * Item.shootSpeed, 0f),
				type, (int)(damage * 0.45f), knockback * 0.7f, player.whoAmI);
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.45f }, player.Center);
			return false;
		}

		public override void MeleeEffects(Player player, Rectangle hitbox)
		{
			if (Main.rand.NextBool(4))
			{
				Dust d = Dust.NewDustDirect(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height,
					DustID.Stone, 0f, 0f, 110, default, 1.1f);
				d.velocity *= 0.4f;
			}
		}

		public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(ModContent.BuffType<Cracked>(), 180);
		}
	}
}
