using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terrapex.Common.Particles;
using Terrapex.Content.Projectiles.Reaper;
using Terrapex.Content.Projectiles.Rime;
using Terrapex.Content.Projectiles.Scarab;
using Terrapex.Content.Projectiles.Shellback;

namespace Terrapex.Content.Items.Weapons.Trees.Reaper
{
	/// <summary>
	/// The five rites of the Harvest Moon tree - each one the after-effect of a scythe it was forged
	/// from, fired through that scythe's own projectile rather than a copy of it, so a change to the
	/// Undertow's whirlpool is a change to the Harvest Moon's tide as well.
	///
	/// | rite | from | what |
	/// |---|---|---|
	/// | Tide  | Undertow        | a whirlpool on the point (`UndertowWhirl`) |
	/// | Sun   | Khepri's Scythe | a sun pebble rolling on along the stroke; two on the edge |
	/// | Frost | Frostfell       | an icicle dropped through the point; three on the edge |
	/// | Fire  | Hellreaper      | a burning crescent thrown along the stroke; two on the edge |
	/// | Rift  | Keeper's Harvest| five rift shards fanned along the stroke (Eclipse and up) |
	///
	/// Shares are of the cut's own damage, which already carries the distance multiplier.
	/// </summary>
	public static class MoonRites
	{
		public const int Tide = 0, Sun = 1, Frost = 2, Fire = 3, Rift = 4;

		public const float TideShare = 0.3f;
		public const float SunShare = 0.5f;
		public const float FrostShare = 0.5f;
		public const float FireShare = 0.45f;
		public const float RiftShare = 0.2f;

		/// <summary>The rite's colour: the scythe it came from, by its own SlashColor.</summary>
		public static readonly Color[] Tints =
		{
			new(120, 220, 210),
			new(255, 200, 90),
			new(160, 230, 255),
			new(255, 144, 64),
			new(255, 154, 217),
		};

		public static void Cast(int rite, Player player, IEntitySource source, Vector2 point, Vector2 aim,
			int damage, float knockback, bool edge, bool fromReap = false)
		{
			float swing = fromReap ? 0f : ReapBolt.FromSwing;
			switch (rite)
			{
				case Tide:
					Projectile.NewProjectile(source, point, Vector2.Zero, ModContent.ProjectileType<UndertowWhirl>(),
						(int)(damage * TideShare), 0f, player.whoAmI);
					break;

				case Sun:
				{
					int side = aim.X >= 0f ? 1 : -1;
					Pebble(source, player, point, side, (int)(damage * SunShare), knockback);
					if (edge)
						Pebble(source, player, point, -side, (int)(damage * SunShare), knockback);
					break;
				}

				case Frost:
				{
					int count = edge ? 3 : 1;
					for (int i = 0; i < count; i++)
					{
						Vector2 at = new(point.X + (i - (count - 1) / 2f) * 30f, point.Y);
						Projectile.NewProjectile(source, at - new Vector2(0f, Rime.FrostfellScythe.DropHeight), Vector2.Zero,
							ModContent.ProjectileType<FrostfellIcicle>(), (int)(damage * FrostShare), knockback * 0.4f,
							player.whoAmI, 0f, at.Y);
					}
					break;
				}

				case Fire:
				{
					int count = edge ? 2 : 1;
					for (int i = 0; i < count; i++)
					{
						float turn = count == 1 ? 0f : (i == 0 ? -0.2f : 0.2f);
						Projectile.NewProjectile(source, point, aim.RotatedBy(turn) * 13f, ModContent.ProjectileType<ReapBolt>(),
							(int)(damage * FireShare), knockback * 0.3f, player.whoAmI, ReapBolt.Fire, swing);
					}
					break;
				}

				default:
					for (int i = 0; i < 5; i++)
						Projectile.NewProjectile(source, point, aim.RotatedBy((i - 2) * 0.28f) * 9f,
							ModContent.ProjectileType<ReapBolt>(), (int)(damage * RiftShare), knockback * 0.3f,
							player.whoAmI, ReapBolt.Rift, swing);
					break;
			}

			// the rite's mark on the point: a bloom of its colour and a few sparks. Client-only
			// decoration, and only the owner runs this, which is the reaper it is for
			Color tint = Tints[rite % Tints.Length];
			ParticleSystem.Spawn(new Bloom(point, new Color(tint.R, tint.G, tint.B, 0) * 0.8f, 0.3f, 0.9f, 18));
			FX.Sparks(point, tint, edge ? 9 : 5, 5f, 0.9f, 20);
		}

		/// <summary>Every rite at once, the way a Reap gives them.</summary>
		public static void CastAll(int count, Player player, IEntitySource source, Vector2 point, Vector2 aim,
			int damage, float knockback, bool edge, bool fromReap)
		{
			for (int r = 0; r < count; r++)
				Cast(r, player, source, point, aim, damage, knockback, edge, fromReap);
		}

		private static void Pebble(IEntitySource source, Player player, Vector2 point, int side, int damage, float knockback)
		{
			Vector2 v = new(side * 6.5f, -3f);
			Projectile.NewProjectile(source, point, Vector2.Zero, ModContent.ProjectileType<SunPebble>(),
				damage, knockback * 0.4f, player.whoAmI, v.X, v.Y);
		}
	}
}
