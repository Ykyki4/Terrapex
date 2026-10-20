using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// The Old Shellback's expert drop: its own Hull, handed to the player. Stand still for half a
	/// second and you pull into the shell - more defence and less damage taken - and moving or
	/// attacking brings you out again. The boss's dial, from the other side: hiding is safe, and
	/// it costs you the fight while you do it.
	/// </summary>
	public class HermitsHull : ModItem
	{
		public const int Settle = 30;
		public const int Defense = 8;
		public const float Endurance = 0.12f;

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 28;
			Item.height = 26;
			Item.accessory = true;
			Item.expert = true;
			Item.rare = ItemRarityID.Expert;
			Item.value = Item.sellPrice(gold: 1, silver: 50);
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
			=> player.GetModPlayer<HermitsHullPlayer>().equipped = true;
	}

	/// <summary>Counts how long the wearer has stood still and applies the shell.</summary>
	public class HermitsHullPlayer : ModPlayer
	{
		public bool equipped;
		private int still;

		public bool Shelled => equipped && still >= HermitsHull.Settle;

		public override void ResetEffects() => equipped = false;

		public override void PostUpdateEquips()
		{
			if (!equipped)
			{
				still = 0;
				return;
			}
			bool resting = Player.velocity.LengthSquared() < 0.01f && Player.itemAnimation == 0 && !Player.dead;
			still = resting ? still + 1 : 0;
			if (!Shelled)
				return;

			Player.statDefense += HermitsHull.Defense;
			Player.endurance += HermitsHull.Endurance;
			Player.AddBuff(ModContent.BuffType<HermitShellBuff>(), 2);
			if (still == HermitsHull.Settle)
			{
				for (int i = 0; i < 10; i++)
					Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.Sand, 0f, -1f, 80, default, 1f);
			}
		}
	}
}
