using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// The Breach's dial, carried out of the event.
	///
	/// Defence that only exists while you are surrounded: one point for every two enemies close
	/// enough to matter, up to <see cref="MaxDefense"/>. It is the same bargain the mouths are —
	/// the more pressure you stand in, the more it pays — and it is worth nothing at all in the
	/// fight the player was going to win anyway.
	/// </summary>
	public class Overburden : ModItem
	{
		public const int Range = 900;
		public const int PerEnemies = 2;
		public const int MaxDefense = 12;

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 24;
			Item.value = Item.sellPrice(gold: 3);
			Item.rare = ItemRarityID.Pink;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			int near = 0;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC npc = Main.npc[i];
				if (npc.active && !npc.friendly && npc.damage > 0 && !npc.dontCountMe
					&& npc.Distance(player.Center) < Range)
					near++;
			}

			int bonus = System.Math.Min(near / PerEnemies, MaxDefense);
			if (bonus <= 0)
				return;

			player.statDefense += bonus;

			// the load shows: more dust the heavier it gets, so the bonus is legible without a
			// number on the screen
			if (Main.rand.NextBool(30 - System.Math.Min(bonus * 2, 22)))
			{
				Dust d = Dust.NewDustDirect(player.position, player.width, player.height,
					DustID.Stone, 0f, 0f, 140, default, 0.9f);
				d.noGravity = true;
				d.velocity *= 0.2f;
			}
		}
	}
}
