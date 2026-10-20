using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Particles;
using Terrapex.Content.Items.Weapons.Reaper;
using Terrapex.Content.Projectiles.Trees.Reaper;

namespace Terrapex.Content.Items.Weapons.Trees.Reaper
{
	/// <summary>
	/// The reaper's crafting tree, "Harvest Moon" (CRAFTING_TREES.md). Every node keeps what the
	/// node below it did and adds one thing, so the four scythes are one class with four switches:
	///
	/// | node | name | adds |
	/// |---|---|---|
	/// | 1 | Harvest Moon  | the cycle: each cut ends in the next of four rites (tide, sun, frost, fire) |
	/// | 2 | Eclipse       | a fifth rite (rift), and a cut on the edge gives two rites, under a corona |
	/// | 3 | Long Night    | the Reap brings three seconds of night: inside it every cut is on the edge |
	/// | 4 | Event Horizon | every cut opens a vortex that drags small enemies in and collapses into all five rites |
	///
	/// The cycle lives on <see cref="MoonTreePlayer"/>, and the moon drawn over the reach circle
	/// (<see cref="MoonReachLayer"/>) shows which rite comes next, so the rotation is something
	/// the reaper plays rather than something that happens to them.
	/// </summary>
	public abstract class MoonScythe : ScytheItem
	{
		/// <summary>1 to 4, Harvest Moon to Event Horizon.</summary>
		public abstract int Node { get; }

		/// <summary>Four rites on the Harvest Moon, five from the Eclipse on.</summary>
		public int Rites => Node >= 2 ? 5 : 4;

		public bool EdgeDoubles => Node >= 2;
		public bool BringsNight => Node >= 3;
		public bool OpensVortex => Node >= 4;

		/// <summary>The rite the next cut will end in, for the draw layer.</summary>
		public int NextRite(Player player) => player.GetModPlayer<MoonTreePlayer>().rite % Rites;

		public override void StrikeAt(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 point,
			float share, int damage, float knockback, bool edge, bool flip)
		{
			// the Long Night: while it lasts the whole circle is the edge
			if (BringsNight && player.GetModPlayer<MoonTreePlayer>().NightFalls)
			{
				if (!edge)
					ParticleSystem.Spawn(new Bloom(point, new Color(200, 210, 255, 0) * 0.7f, 0.2f, 0.7f, 14));
				edge = true;
			}
			base.StrikeAt(player, source, point, share, damage, knockback, edge, flip);
		}

		public override void AfterStrike(Player player, Projectile slash, Vector2 aim, bool edge)
		{
			MoonTreePlayer moon = player.GetModPlayer<MoonTreePlayer>();
			IEntitySource source = slash.GetSource_FromThis();
			int rite = moon.rite % Rites;
			MoonRites.Cast(rite, player, source, slash.Center, aim, slash.damage, slash.knockBack, edge);
			if (EdgeDoubles && edge)
			{
				// the eclipse: two rites cross on the point, the next one riding the first
				MoonRites.Cast((rite + 1) % Rites, player, source, slash.Center, aim, slash.damage, slash.knockBack, edge);
				Projectile.NewProjectile(source, slash.Center, Vector2.Zero, ModContent.ProjectileType<EclipseCorona>(),
					0, 0f, player.whoAmI, rite, (rite + 1) % Rites);
				moon.rite = (rite + 2) % Rites;
			}
			else
				moon.rite = (rite + 1) % Rites;

			if (OpensVortex && player.ownedProjectileCounts[ModContent.ProjectileType<EventHorizonVortex>()] < EventHorizonVortex.MaxOpen)
				Projectile.NewProjectile(source, slash.Center, Vector2.Zero, ModContent.ProjectileType<EventHorizonVortex>(),
					(int)(slash.damage * EventHorizonVortex.TickShare), 0f, player.whoAmI, slash.damage, 1f);
		}

		/// <summary>
		/// The Reap, built up node by node: the spin, every rite at once on the reticle, and then what
		/// each node adds on top - the corona, the night, the vortex.
		/// </summary>
		public override void Reap(Player player, IEntitySource source, Vector2 center, Vector2 aim,
			int damage, float knockback)
		{
			ReapSpin(player, source, center, aim, damage, knockback);
			Vector2 point = StrikePoint(player, Main.MouseWorld, out _);
			Vector2 dir = (point - center).SafeNormalize(aim);
			MoonRites.CastAll(Rites, player, source, point, dir, damage, knockback, EdgeDoubles, true);
			if (EdgeDoubles)
				Projectile.NewProjectile(source, point, Vector2.Zero, ModContent.ProjectileType<EclipseCorona>(),
					0, 0f, player.whoAmI, MoonRites.Rift, MoonRites.Sun, 1.6f);
			if (BringsNight)
			{
				player.GetModPlayer<MoonTreePlayer>().BeginNight();
				ReapExtra(player, source, center, dir, damage, knockback);
			}
			if (OpensVortex)
				Projectile.NewProjectile(source, point, Vector2.Zero, ModContent.ProjectileType<EventHorizonVortex>(),
					(int)(damage * EventHorizonVortex.TickShare), 0f, player.whoAmI, damage, EventHorizonVortex.ReapScale);
		}

		/// <summary>What a node's own ingredients add to its Reap. Long Night: the Death Sickle's three.</summary>
		protected virtual void ReapExtra(Player player, IEntitySource source, Vector2 center, Vector2 dir,
			int damage, float knockback)
		{
		}

		/// <summary>
		/// The blade's own hits carry the fire and frost scythes the tree was forged from: every hit
		/// burns, an edge or Reap hit freezes as well. From the Eclipse on the fire is hellfire.
		/// </summary>
		public override void OnScytheHit(Player player, Projectile stroke, NPC target, NPC.HitInfo hit,
			bool edge, bool reap)
		{
			target.AddBuff(Node >= 2 ? BuffID.OnFire3 : BuffID.OnFire, 180);
			if (edge || reap)
				target.AddBuff(Node >= 3 ? BuffID.Frostburn2 : BuffID.Frostburn, 180);
		}

		protected void Defaults(int size, int damage, int useTime, float knockback, int rare, int goldValue)
		{
			Item.width = size;
			Item.height = size;
			Item.damage = damage;
			Item.knockBack = knockback;
			Item.useTime = useTime;
			Item.useAnimation = useTime;
			Item.rare = rare;
			Item.value = Item.sellPrice(gold: goldValue);
		}
	}
}
