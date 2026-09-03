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
	/// It swings like a maul, and it has two beats. A swing aimed at the floor drives a plate of
	/// stone up out of it — a wall the player places, where the boss placed one at them. A swing
	/// aimed at a plate that is already standing <b>breaks it</b>, and the wall goes where the
	/// cursor was pointing as a fan of shards.
	///
	/// The second beat is the point. Everything from here on flies, and a weapon whose only trick
	/// is a wall on the floor has nothing to say to a boss twenty tiles up: the plate stops being
	/// cover and becomes ammunition. Raise, then break — the same shape as the Seam's third swing,
	/// which is the mod's own idea of what a combo is.
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

		/// <summary>How far from the cursor a standing plate may be and still be the target.</summary>
		private const float ShatterReach = 150f;

		private const int Shards = 7;

		/// <summary>
		/// One swing, two possible answers: break the plate the cursor is on, or raise a new one
		/// where the cursor is. Two plates at a time, so the weapon builds a short wall rather
		/// than a fence.
		/// </summary>
		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			Vector2 aim = Main.MouseWorld;
			if (Vector2.Distance(aim, player.Center) > 260f)
				aim = player.Center + Vector2.Normalize(aim - player.Center) * 260f;

			if (Shatter(player, source, aim, damage, knockback))
				return false;

			if (player.ownedProjectileCounts[type] >= 2)
				return false;

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

		/// <summary>
		/// Breaks the standing plate nearest the cursor and throws it, in a fan aimed from the
		/// plate toward the cursor. Returns false when there was nothing to break, which is the
		/// swing that raises one instead.
		/// </summary>
		private bool Shatter(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 aim,
			int damage, float knockback)
		{
			int slabType = ModContent.ProjectileType<FriendlySlab>();
			Projectile best = null;
			float dist = ShatterReach;

			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				Projectile p = Main.projectile[i];
				if (!p.active || p.type != slabType || p.owner != player.whoAmI
					|| p.ModProjectile is not HeaveSlab slab || !slab.Standing)
					continue;
				float d = Vector2.Distance(slab.Face, aim);
				if (d >= dist)
					continue;
				dist = d;
				best = p;
			}

			if (best == null)
				return false;

			Vector2 from = ((HeaveSlab)best.ModProjectile).Face;
			Vector2 toward = aim - from;
			if (toward.LengthSquared() < 4f)
				toward = -Vector2.UnitY;
			toward.Normalize();

			for (int i = 0; i < Shards; i++)
			{
				float spread = MathHelper.Lerp(-0.34f, 0.34f, i / (Shards - 1f));
				Vector2 v = toward.RotatedBy(spread) * Main.rand.NextFloat(11f, 14f);
				Projectile.NewProjectile(source, from, v, ModContent.ProjectileType<SlabShard>(),
					(int)(damage * 0.42f), knockback * 0.5f, player.whoAmI);
			}

			for (int i = 0; i < 22; i++)
			{
				Dust d = Dust.NewDustPerfect(from + Main.rand.NextVector2Circular(14f, 34f),
					DustID.Stone, toward.RotatedByRandom(0.6) * Main.rand.NextFloat(1f, 5f),
					100, default, 1.25f);
				d.noGravity = true;
			}

			best.Kill();
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70 with { Pitch = 0.3f }, from);
			return true;
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
