using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.NPCs.Bosses;

namespace Terrapex.Content.Items.Consumables
{
	/// <summary>
	/// Summons the Hollow Harvester: at night, on the surface. Hay, silk and bone - the bone puts it
	/// after Skeletron, which is where the fight is meant to sit (BOSS_HARVESTER.md).
	/// </summary>
	public class ScarecrowEffigy : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 3;
			ItemID.Sets.SortingPriorityBossSpawns[Type] = 12;
		}

		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 30;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 20);
			Item.rare = ItemRarityID.Orange;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.consumable = true;
		}

		public override bool CanUseItem(Player player)
			=> !Main.dayTime && player.ZoneOverworldHeight && !NPC.AnyNPCs(ModContent.NPCType<HollowHarvester>());

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI != Main.myPlayer)
				return true;
			SoundEngine.PlaySound(SoundID.Roar, player.position);
			int type = ModContent.NPCType<HollowHarvester>();
			if (Main.netMode != NetmodeID.MultiplayerClient)
				NPC.SpawnOnPlayer(player.whoAmI, type);
			else
				NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: type);
			return true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.Hay, 30)
				.AddIngredient(ItemID.Bone, 10)
				.AddIngredient(ItemID.Silk, 5)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
