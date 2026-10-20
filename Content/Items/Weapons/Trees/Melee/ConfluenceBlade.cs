using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Particles;
using Terrapex.Content.Buffs;
using Terrapex.Content.Projectiles;
using Terrapex.Content.Projectiles.Rime;
using Terrapex.Content.Projectiles.Scarab;
using Terrapex.Content.Projectiles.Shellback;
using Terrapex.Content.Projectiles.Trees.Melee;

namespace Terrapex.Content.Items.Weapons.Trees.Melee
{
	/// <summary>
	/// The four "currents" of the Confluence tree, one per ingredient sword of the first node:
	/// the tide of the Wrecker's Cutlass, the sun of the Sultan's Khopesh, the frost of the
	/// Glacierbreaker and the bone of Bonefissure. Each swing of any node carries the next one.
	/// </summary>
	public enum Current
	{
		Tide,
		Sun,
		Frost,
		Bone,
	}

	public static class Currents
	{
		public const int Count = 4;

		/// <summary>The body colour of a current's arc and wave.</summary>
		public static Color Of(Current c) => c switch
		{
			Current.Tide => new Color(70, 214, 214),
			Current.Sun => new Color(255, 186, 64),
			Current.Frost => new Color(150, 228, 255),
			_ => new Color(255, 232, 186),
		};

		/// <summary>The hot edge each tier lays over its current: steel, thread, silk, regard.</summary>
		public static Color Edge(int tier) => tier switch
		{
			1 => new Color(255, 255, 240),
			2 => new Color(127, 242, 224),
			3 => new Color(216, 255, 247),
			_ => new Color(255, 215, 154),
		};

		/// <summary>The debuff a current leaves; from the second node on, the hardmode grade.</summary>
		public static void Debuff(NPC target, Current c, int tier)
		{
			switch (c)
			{
				case Current.Sun:
					target.AddBuff(tier >= 2 ? BuffID.OnFire3 : BuffID.OnFire, 180);
					break;
				case Current.Frost:
					target.AddBuff(tier >= 2 ? BuffID.Frostburn2 : BuffID.Frostburn, 180);
					break;
				case Current.Bone:
					target.AddBuff(ModContent.BuffType<Cracked>(), 180);
					break;
				case Current.Tide:
					target.AddBuff(BuffID.Wet, 240);
					break;
			}
		}

		/// <summary>
		/// The ingredient's own trick, fired once: the Cutlass's barnacle shrapnel, the Khopesh's
		/// sun flare, the Glacierbreaker's three splinters, Bonefissure's ricocheting splinter.
		/// These are the ingredients' real projectiles, not lookalikes - the node keeps them.
		/// </summary>
		public static void Burst(IEntitySource src, Player player, Vector2 at, Vector2 away, Current c, int damage)
		{
			if (player.whoAmI != Main.myPlayer)
				return;
			away = away.SafeNormalize(Vector2.UnitX * player.direction);
			switch (c)
			{
				case Current.Tide:
					for (int i = 0; i < 2; i++)
					{
						Vector2 v = new(Main.rand.NextFloat(-3.5f, 3.5f), Main.rand.NextFloat(-6f, -4f));
						Projectile.NewProjectile(src, at, v, ModContent.ProjectileType<BarnacleShrapnel>(),
							System.Math.Max(1, (int)(damage * 0.35f)), 1f, player.whoAmI);
					}
					break;
				case Current.Sun:
					Projectile.NewProjectile(src, at, Vector2.Zero, ModContent.ProjectileType<KhopeshFlare>(),
						(int)(damage * 0.6f), 3f, player.whoAmI);
					break;
				case Current.Frost:
					for (int i = 0; i < 3; i++)
					{
						float t = i - 1f;
						Projectile.NewProjectile(src, at, away.RotatedBy(t * 0.55f) * 7f - Vector2.UnitY * 2f,
							ModContent.ProjectileType<RimeSplinter>(), (int)(damage * 0.35f), 2f, player.whoAmI, 1f);
					}
					break;
				case Current.Bone:
					Projectile.NewProjectile(src, at, away * 10f, ModContent.ProjectileType<BoneSplinter>(),
						(int)(damage * 0.45f), 2f, player.whoAmI);
					break;
			}
		}

		/// <summary>Decorative splash of a current at a point - particles only, safe anywhere.</summary>
		public static void Splash(Vector2 at, Current c, float size = 1f)
		{
			if (Main.dedServ)
				return;
			Color col = Of(c);
			switch (c)
			{
				case Current.Tide:
					FX.Sparks(at, col, (int)(8 * size), 6f * size, size, 22, 0.18f);
					ParticleSystem.Spawn(new Shockwave(at, col * 0.8f, 0.1f * size, 0.9f * size, 20));
					break;
				case Current.Sun:
					FX.Impact(at, new Color(255, 230, 150), col, 0.7f * size);
					break;
				case Current.Frost:
					for (int i = 0; i < 6 * size; i++)
						ParticleSystem.Spawn(new Chip(at, Main.rand.NextVector2Circular(4f, 4f) - Vector2.UnitY * 2f,
							col, Main.rand.NextFloat(0.8f, 1.3f), 40));
					ParticleSystem.Spawn(new Bloom(at, col, 0.3f * size, 0.9f * size, 14));
					break;
				default:
					for (int i = 0; i < 5 * size; i++)
						ParticleSystem.Spawn(new Chip(at, Main.rand.NextVector2Circular(4f, 4f) - Vector2.UnitY * 2f,
							col, Main.rand.NextFloat(0.8f, 1.2f), 36));
					FX.Sparks(at, col, (int)(6 * size), 5f * size, size * 0.8f, 18);
					break;
			}
		}
	}

	/// <summary>
	/// Every sword of the tree. The node rule of CRAFTING_TREES.md is implemented here as a ladder
	/// on <see cref="Node"/>: each node runs everything the node below it does and switches one more
	/// thing on - the wave every third swing (1), the wave every swing plus a four-current fan on the
	/// fourth (2), the seam a wave leaves behind (3), the spectral blades of the whole tree (4).
	/// </summary>
	public abstract class ConfluenceBlade : ModItem
	{
		/// <summary>1 Confluence, 2 True Confluence, 3 Worldseam, 4 Meridian.</summary>
		public abstract int Node { get; }

		/// <summary>The share of the sword's damage each wave carries.</summary>
		protected abstract float WaveShare { get; }

		/// <summary>Arc radius in px, about the sprite's diagonal times its scale.</summary>
		protected abstract float Reach { get; }

		private int swing;
		private int procSwing = -1;
		private Current current;

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		protected void Defaults(int size, int damage, int useTime, float knockBack, int rare, int goldValue, float scale)
		{
			Item.width = size;
			Item.height = size;
			Item.damage = damage;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = knockBack;
			Item.useTime = useTime;
			Item.useAnimation = useTime;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.UseSound = SoundID.Item1;
			Item.value = Item.sellPrice(gold: goldValue);
			Item.rare = rare;
			Item.scale = scale;
			Item.shoot = ModContent.ProjectileType<ConfluenceWave>();
			Item.shootSpeed = 11f + Node;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			current = (Current)(swing % Currents.Count);
			swing++;

			Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, ModContent.ProjectileType<ConfluenceSlash>(),
				0, 0f, player.whoAmI, (float)current, Node, Reach * player.GetAdjustedItemScale(Item));

			int wave = (int)(damage * WaveShare);
			if (Node == 1)
			{
				// every third swing, so the wave walks the currents round instead of always being bone
				if (swing % 3 == 0)
					Wave(source, player, position, velocity, current, wave, knockback);
			}
			else if (swing % 4 == 0)
			{
				// the fan: all four currents at once, the True Night's Edge beat of the tree
				for (int i = 0; i < Currents.Count; i++)
					Wave(source, player, position, velocity.RotatedBy((i - 1.5f) * 0.17f), (Current)i,
						(int)(wave * 0.85f), knockback);
				Terraria.Audio.SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.6f, Pitch = -0.1f }, position);
			}
			else
			{
				Wave(source, player, position, velocity, current, wave, knockback);
			}

			if (Node >= 4)
				MeridianBlade.Unfold(source, player, Main.MouseWorld, (int)(damage * MeridianBlade.Share), knockback);
			return false;
		}

		private void Wave(IEntitySource source, Player player, Vector2 position, Vector2 velocity, Current c,
			int damage, float knockback)
		{
			Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<ConfluenceWave>(),
				damage, knockback * (c == Current.Tide ? 1.4f : 0.7f), player.whoAmI, (float)c, Node);
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Item60 with { Volume = 0.35f, Pitch = 0.15f * (int)c }, position);
		}

		// the tide shoves: the Cutlass's swing, made a current
		public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers)
		{
			if (current == Current.Tide)
				modifiers.Knockback *= 1.6f;
		}

		public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
		{
			Currents.Debuff(target, current, Node);
			Currents.Splash(target.Center, current, 0.8f);
			// the ingredient's own trick, once per swing however many things the swing catches
			if (procSwing == swing)
				return;
			procSwing = swing;
			Currents.Burst(player.GetSource_OnHit(target), player, target.Center, target.Center - player.Center,
				current, player.GetWeaponDamage(Item));
		}

		public override void MeleeEffects(Player player, Rectangle hitbox)
		{
			if (!Main.rand.NextBool(3))
				return;
			int dust = current switch
			{
				Current.Tide => DustID.Water,
				Current.Sun => DustID.GoldFlame,
				Current.Frost => DustID.Ice,
				_ => DustID.Bone,
			};
			Dust d = Dust.NewDustDirect(hitbox.TopLeft(), hitbox.Width, hitbox.Height, dust, 0f, 0f, 100, default, 1.1f);
			d.noGravity = true;
			d.velocity *= 0.3f;
		}
	}
}
