using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Particles;
using Terrapex.Content.Projectiles;
using Terrapex.Content.Projectiles.Rime;
using Terrapex.Content.Projectiles.Scarab;
using Terrapex.Content.Projectiles.Shellback;
using Terrapex.Content.Projectiles.Trees.Magic;

namespace Terrapex.Content.Items.Weapons.Trees.Magic
{
	/// <summary>
	/// The magic tree of CRAFTING_TREES.md - Almanac, Equinox Almanac, Solstice, Cosmogony. One
	/// book that grows, and its four pages are the four tomes it was bound out of:
	///
	/// | page | from | cast | held (node 2 on) |
	/// |---|---|---|---|
	/// | Tide  | Brine Conch   | one bubble that bursts into brine | three bubbles in a fan |
	/// | Sun   | Sun Scepter   | a sun bolt that raises a pillar where it lands | three pillars straight up under the cursor |
	/// | Snow  | Snowcap Staff | a snowball that rolls and grows | the snowball arrives already full-grown |
	/// | Scree | Scree Tome    | a cone of five stones | a tighter, faster cone of nine |
	///
	/// The casts *are* the ingredients' own projectiles - the Almanac spawns ConchBubble,
	/// ScepterBolt, RollingSnowball and ScreeStone rather than drawing copies of them - so a
	/// player who carried the four tomes recognises every page on sight.
	/// </summary>
	public abstract class AlmanacItem : ModItem
	{
		public const int Tide = 0, Sun = 1, Snow = 2, Scree = 3, Pages = 4;

		/// <summary>1 Almanac, 2 Equinox, 3 Solstice, 4 Cosmogony.</summary>
		public abstract int Level { get; }

		public static readonly Color[] PageColor =
		{
			new(127, 212, 224),   // tide: the Tidewalker teal
			new(255, 196, 90),    // sun: gold
			new(176, 232, 255),   // snow: ice
			new(201, 189, 209),   // scree: the stone ramp's light end
		};

		/// <summary>The share of the book's damage one page gets when the book casts several at once.</summary>
		public static float PageShare(int level) => level switch
		{
			3 => 0.75f,
			4 => 0.55f,
			_ => 1f,
		};

		/// <summary>The Cosmogony's chosen page, out of its ring of four.</summary>
		public const float SelectedShare = 0.85f;

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		protected void BookDefaults(int damage, int mana, int useTime, float knockBack)
		{
			Item.damage = damage;
			Item.DamageType = DamageClass.Magic;
			Item.mana = mana;
			Item.knockBack = knockBack;
			Item.useTime = useTime;
			Item.useAnimation = useTime;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.noMelee = true;
			Item.autoReuse = true;
			Item.UseSound = SoundID.Item8;
			Item.shootSpeed = 8f;
			if (Level >= 2)
			{
				// held: a charge projectile owns the cast from here and lets go on release
				Item.channel = true;
				Item.shoot = ModContent.ProjectileType<AlmanacCharge>();
			}
			else
			{
				Item.shoot = ModContent.ProjectileType<ConchBubble>();
			}
		}

		public override Vector2? HoldoutOffset() => new Vector2(-4f, -2f);

		// right-click turns the page, from the Equinox on - the first book turns its own
		public override bool AltFunctionUse(Player player) => Level >= 2;

		public override bool CanUseItem(Player player)
		{
			if (player.altFunctionUse == 2)
				return true;
			return player.ownedProjectileCounts[ModContent.ProjectileType<AlmanacCharge>()] < 1;
		}

		public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
		{
			if (player.altFunctionUse == 2)
				mult = 0f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			AlmanacPlayer book = player.GetModPlayer<AlmanacPlayer>();
			if (player.altFunctionUse == 2)
			{
				book.TurnPage(player, 1);
				return false;
			}
			if (Level >= 2)
			{
				Vector2 dir = velocity.SafeNormalize(Vector2.UnitX * player.direction);
				Projectile.NewProjectile(source, player.MountedCenter, dir, type, damage, knockback,
					player.whoAmI, 0f, Level);
				return false;
			}

			// the Almanac proper: the book decides the page, four casts each
			Cast(source, player, position, Main.MouseWorld, book.page, false, damage, knockback);
			book.casts++;
			if (book.casts >= AlmanacPlayer.CastsPerPage)
				book.TurnPage(player, 1);
			return false;
		}

		/// <summary>
		/// One page, cast from <paramref name="from"/> toward <paramref name="target"/>. Every
		/// node of the tree routes through here, so a page plays the same in all four books.
		/// </summary>
		public static void Cast(IEntitySource src, Player p, Vector2 from, Vector2 target, int page,
			bool charged, int damage, float kb)
		{
			Vector2 dir = (target - from).SafeNormalize(Vector2.UnitX * p.direction);
			int owner = p.whoAmI;
			switch (page)
			{
				case Tide:
					if (!charged)
						Projectile.NewProjectile(src, from, dir * 5f, ModContent.ProjectileType<ConchBubble>(), damage, kb, owner);
					else
						for (int i = -1; i <= 1; i++)
							Projectile.NewProjectile(src, from, dir.RotatedBy(i * 0.28f) * 5.5f,
								ModContent.ProjectileType<ConchBubble>(), damage, kb, owner);
					break;

				case Sun:
					if (!charged)
						Projectile.NewProjectile(src, from, dir * 10f, ModContent.ProjectileType<ScepterBolt>(), damage, kb, owner);
					else
						for (int i = -1; i <= 1; i++)
						{
							Vector2 floor = SandPillar.FindGround(target + new Vector2(i * 44f, 0f), 24);
							Projectile.NewProjectile(src, floor, Vector2.Zero, ModContent.ProjectileType<ScepterPillar>(),
								(int)(damage * 1.2f), kb, owner);
						}
					break;

				case Snow:
					// ai[0] is the snowball's own rolled distance; handing it GrowTicks makes it full-grown
					Projectile.NewProjectile(src, from, dir * 8f, ModContent.ProjectileType<RollingSnowball>(),
						damage, kb + 1f, owner, charged ? RollingSnowball.GrowTicks : 0f);
					break;

				default:
					int stones = charged ? 9 : 5;
					float spread = charged ? 0.22f : 0.30f;
					float speed = charged ? 13f : 10.5f;
					int each = (int)(damage * (charged ? 0.45f : 0.4f));
					for (int i = 0; i < stones; i++)
					{
						Vector2 v = dir.RotatedByRandom(spread) * speed * Main.rand.NextFloat(0.82f, 1.12f);
						Projectile.NewProjectile(src, from, v, ModContent.ProjectileType<ScreeStone>(), each, kb, owner);
					}
					break;
			}
			AlmanacFX.CastFlash(from, dir, PageColor[page], charged);
		}

		/// <summary>
		/// What a held book does when it lets go. <paramref name="aim"/> is the cursor at release.
		/// Each level keeps everything the one below it did and adds one thing on top:
		/// 2 the page of your choosing, and the Riftflow lance on a full charge;
		/// 3 the opposite page too, and the Loom's thread strung across the cursor;
		/// 4 all four pages from where they orbit, and the Regard's field on the cursor.
		/// </summary>
		public static void Release(IEntitySource src, Player p, int level, bool charged, int damage, float kb, Vector2 aim)
		{
			AlmanacPlayer book = p.GetModPlayer<AlmanacPlayer>();
			int share = (int)(damage * PageShare(level));
			Vector2 dir = (aim - p.MountedCenter).SafeNormalize(Vector2.UnitX * p.direction);

			if (level >= 4)
			{
				for (int k = 0; k < Pages; k++)
				{
					// the page right-click picked is the lit one, and it hits harder
					int dmg = k == book.page ? (int)(damage * SelectedShare) : share;
					Cast(src, p, p.MountedCenter + book.OrbitOffset(k), aim, k, charged, dmg, kb);
				}
				book.Close();
			}
			else
			{
				Cast(src, p, p.MountedCenter + dir * 16f, aim, book.page, charged, share, kb);
				if (level >= 3)
					Cast(src, p, p.MountedCenter + dir * 16f, aim, (book.page + 2) % Pages, charged, share, kb);
			}

			if (!charged)
			{
				SoundEngine.PlaySound(SoundID.Item8, p.MountedCenter);
				return;
			}

			SoundEngine.PlaySound(SoundID.Item72, p.MountedCenter);
			Projectile.NewProjectile(src, p.MountedCenter, dir, ModContent.ProjectileType<EquinoxLance>(),
				(int)(damage * 0.8f), kb, p.whoAmI);

			if (level >= 3)
			{
				// the Loom Staff's frame, as a single strand across where the pages are going
				Vector2 across = dir.RotatedBy(MathHelper.PiOver2);
				Projectile.NewProjectile(src, aim, across, ModContent.ProjectileType<FriendlyThread>(),
					(int)(damage * 0.5f), 0f, p.whoAmI, 72f);
			}

			if (level >= 4 && p.ownedProjectileCounts[ModContent.ProjectileType<GazeField>()] < 1)
			{
				Projectile field = Projectile.NewProjectileDirect(src, aim, Vector2.Zero,
					ModContent.ProjectileType<GazeField>(), (int)(damage * 0.6f), kb, p.whoAmI);
				field.Center = aim;
			}
		}
	}
}
