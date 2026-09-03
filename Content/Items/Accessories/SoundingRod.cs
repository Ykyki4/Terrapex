using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.NPCs;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// Mouths show through stone.
	///
	/// The Breach is fought where you were digging, which means the thing raising the spawn rate
	/// is usually behind a wall. Without this the dial is real but invisible, and a player who
	/// cannot find a mouth is not choosing to leave it standing. The marker is dust rather than
	/// light, for the same reason Fissure Sight uses dust: light stops at the rock.
	/// </summary>
	public class SoundingRod : ModItem
	{
		public const int Range = 3200;
		private const int Interval = 12;

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 26;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Pink;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			if (player.whoAmI != Main.myPlayer || Main.GameUpdateCount % Interval != 0)
				return;

			int mouth = ModContent.NPCType<BreachMouth>();
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC npc = Main.npc[i];
				if (!npc.active || npc.type != mouth || npc.Distance(player.Center) > Range)
					continue;

				// a mark on the mouth itself, and a bead on the line to it, so a mouth off the
				// edge of the screen still tells the player which way to dig
				Mark(npc.Center);
				Vector2 toward = Vector2.Normalize(npc.Center - player.Center);
				for (int step = 1; step <= 3; step++)
					Mark(player.Center + toward * (step * 90f));
			}
		}

		private static void Mark(Vector2 at)
		{
			Dust d = Dust.NewDustPerfect(at, DustID.PurpleTorch, Vector2.Zero, 90, default, 1.15f);
			d.noGravity = true;
			d.velocity = Vector2.Zero;
			d.fadeIn = 1.3f;
		}
	}
}
