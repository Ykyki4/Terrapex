using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs.Trees;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles.Trees.Summon;
using Terrapex.Content.Tiles;
using BeetlingStaffItem = Terrapex.Content.Items.Weapons.Scarab.BeetlingStaff;
using GlacierBellItem = Terrapex.Content.Items.Weapons.Rime.GlacierBell;
using HatchlingRodItem = Terrapex.Content.Items.Weapons.HatchlingRod;
using KeeperStaffItem = Terrapex.Content.Items.Weapons.KeeperStaff;
using RustedCrabPotItem = Terrapex.Content.Items.Weapons.Shellback.RustedCrabPot;
using SailclothItem = Terrapex.Content.Items.Weapons.Sailcloth;
using WardenPlateItem = Terrapex.Content.Items.Weapons.WardenPlate;

namespace Terrapex.Content.Items.Weapons.Trees.Summon
{
	/// <summary>
	/// The summon tree of CRAFTING_TREES.md, "Menagerie". Every node summons the same beast -
	/// <see cref="MenagerieBeast"/>, crab / beetling / rime sprite / hatchling - and differs in
	/// what the pack does once it is out (see the beast's summary). The base here only spawns:
	/// a node with <see cref="WholeArk"/> lets out all four forms at once, every other node lets
	/// out whichever form is fewest on the field, so summon after summon walks the cycle.
	/// </summary>
	public abstract class MenagerieItem : ModItem
	{
		protected abstract int BeastType { get; }
		protected abstract int Buff { get; }
		protected virtual bool WholeArk => false;
		/// <summary>The share of the item's damage each beast carries. A quarter-slot Ark beast carries less.</summary>
		protected virtual float BeastShare => 1f;

		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 1;
			ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
			ItemID.Sets.LockOnIgnoresCollision[Type] = true;
		}

		protected void Base(int damage, int mana, int use, float kb, int rare, int gold)
		{
			Item.width = 40;
			Item.height = 40;
			Item.damage = damage;
			Item.DamageType = DamageClass.Summon;
			Item.mana = mana;
			Item.knockBack = kb;
			Item.useTime = use;
			Item.useAnimation = use;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.noMelee = true;
			Item.UseSound = SoundID.Item44;
			Item.value = Item.sellPrice(gold: gold);
			Item.rare = rare;
			Item.buffType = Buff;
			Item.shoot = BeastType;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			player.AddBuff(Item.buffType, 2);
			int each = (int)(Item.damage * BeastShare);
			if (WholeArk)
			{
				for (int form = 0; form < 4; form++)
				{
					Vector2 at = Main.MouseWorld + new Vector2((form - 1.5f) * 26f, 0f);
					Projectile p = Projectile.NewProjectileDirect(source, at, Vector2.Zero, type,
						(int)(damage * BeastShare), knockback, player.whoAmI, 0f, 0f, form);
					p.originalDamage = each;
				}
				return false;
			}
			int next = MenagerieBeast.NextForm(player, type);
			Projectile proj = Projectile.NewProjectileDirect(source, Main.MouseWorld, Vector2.Zero, type,
				damage, knockback, player.whoAmI, 0f, 0f, next);
			proj.originalDamage = each;
			return false;
		}
	}

	/// <summary>Node 1 (T2): four summons in one horn. Each use lets out the next beast.</summary>
	public class MenagerieHorn : MenagerieItem
	{
		protected override int BeastType => ModContent.ProjectileType<HornBeast>();
		protected override int Buff => ModContent.BuffType<MenagerieHornBuff>();

		public override void SetDefaults() => Base(26, 10, 30, 3f, ItemRarityID.Orange, 4);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<RustedCrabPotItem>()
				.AddIngredient<BeetlingStaffItem>()
				.AddIngredient<GlacierBellItem>()
				.AddIngredient<HatchlingRodItem>()
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}

	/// <summary>Node 2 (T4): the Horn, plus the court - all four beasts out is +15% summon damage.</summary>
	public class WildCourt : MenagerieItem
	{
		protected override int BeastType => ModContent.ProjectileType<CourtBeast>();
		protected override int Buff => ModContent.BuffType<WildCourtBuff>();

		public override void SetDefaults() => Base(46, 12, 28, 3.5f, ItemRarityID.LightPurple, 12);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<MenagerieHorn>()
				.AddIngredient<WardenPlateItem>()
				.AddIngredient(ItemID.SoulofMight, 5)
				.AddIngredient(ItemID.SoulofSight, 5)
				.AddIngredient(ItemID.SoulofFright, 5)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}

	/// <summary>Node 3 (T5): the Court, plus the trade - every 6 s the pack swaps places and strikes at once.</summary>
	public class GroveConclave : MenagerieItem
	{
		protected override int BeastType => ModContent.ProjectileType<ConclaveBeast>();
		protected override int Buff => ModContent.BuffType<GroveConclaveBuff>();

		public override void SetDefaults() => Base(66, 14, 26, 4f, ItemRarityID.Yellow, 24);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<WildCourt>()
				.AddIngredient<SailclothItem>()
				.AddIngredient(ItemID.PygmyStaff)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}

	/// <summary>Node 4 (T6): one slot, the whole ark - all four beasts at once, and a volley every 3 s.</summary>
	public class TheArk : MenagerieItem
	{
		protected override int BeastType => ModContent.ProjectileType<ArkBeast>();
		protected override int Buff => ModContent.BuffType<ArkBuff>();
		protected override bool WholeArk => true;
		protected override float BeastShare => 0.32f;

		public override void SetDefaults() => Base(115, 18, 24, 4.5f, ItemRarityID.Purple, 50);

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<GroveConclave>()
				.AddIngredient<KeeperStaffItem>()
				.AddIngredient(ItemID.StardustDragonStaff)
				.AddIngredient(ItemID.LunarBar, 10)
				.AddIngredient<PrimordiumBar>(5)
				.AddTile(TileID.LunarCraftingStation)
				.Register();
		}
	}
}
