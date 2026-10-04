using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Weapons.Reaper;

namespace Terrapex.Common.Reaper
{
	/// <summary>
	/// Where the scythes that are not crafted come from, on vanilla's side. A class needs more
	/// routes than the anvil: Calamity and Homeward Journey both hand out weapons through nights,
	/// biomes and chests, and a reaper who only ever crafts never has a reason to go anywhere.
	///
	/// | scythe | from |
	/// |---|---|
	/// | Gravedigger | Blood Zombie, Drippler - 4% |
	/// | Frostbite Sickle | Ice Slime, Spiked Ice Slime, Zombie Eskimo - 2%, plus a third of ice chests |
	/// | Hivecutter | Queen Bee - one in three, from the boss in normal mode and from the bag in expert |
	/// </summary>
	public class ScytheLootNPC : GlobalNPC
	{
		public const int GravediggerChance = 25;
		public const int FrostbiteChance = 50;
		public const int HivecutterChance = 3;

		public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
		{
			switch (npc.type)
			{
				case NPCID.BloodZombie:
				case NPCID.Drippler:
					npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Gravedigger>(), GravediggerChance));
					break;

				case NPCID.IceSlime:
				case NPCID.SpikedIceSlime:
				case NPCID.ZombieEskimo:
				case NPCID.ArmedZombieEskimo:
					npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FrostbiteSickle>(), FrostbiteChance));
					break;

				case NPCID.QueenBee:
					npcLoot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(),
						ModContent.ItemType<Hivecutter>(), HivecutterChance));
					break;
			}
		}
	}

	public class ScytheLootBag : GlobalItem
	{
		public override bool AppliesToEntity(Item entity, bool lateInstantiation)
			=> entity.type == ItemID.QueenBeeBossBag;

		public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
			=> itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Hivecutter>(), ScytheLootNPC.HivecutterChance));
	}

	/// <summary>
	/// A third of the ice chests a new world generates carry a Frostbite Sickle. Worlds made before
	/// it existed still have the slimes and the Eskimo zombies.
	/// </summary>
	public class ScytheChests : ModSystem
	{
		/// <summary>The Ice Chest's style in <see cref="TileID.Containers"/>.</summary>
		private const int IceChestStyle = 11;

		public override void PostWorldGen()
		{
			int sickle = ModContent.ItemType<FrostbiteSickle>();
			foreach (Chest chest in Main.chest)
			{
				if (chest == null)
					continue;
				Tile tile = Main.tile[chest.x, chest.y];
				if (tile.TileType != TileID.Containers || tile.TileFrameX / 36 != IceChestStyle)
					continue;
				if (!WorldGen.genRand.NextBool(3))
					continue;
				foreach (Item slot in chest.item)
				{
					if (slot.IsAir)
					{
						slot.SetDefaults(sickle);
						break;
					}
				}
			}
		}
	}
}
