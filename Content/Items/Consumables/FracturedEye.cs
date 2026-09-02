using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.NPCs.Bosses;

namespace Terrapex.Content.Items.Consumables
{
	public class FracturedEye : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 3;
			ItemID.Sets.SortingPriorityBossSpawns[Type] = 12;
		}

		public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 30;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 40);
			Item.rare = ItemRarityID.LightRed;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.consumable = true;
		}

		public override bool CanUseItem(Player player)
			=> !NPC.AnyNPCs(ModContent.NPCType<KeeperOfTheRift>());

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI != Main.myPlayer)
				return true;

			SoundEngine.PlaySound(SoundID.Roar, player.position);

			int type = ModContent.NPCType<KeeperOfTheRift>();
			if (Main.netMode != NetmodeID.MultiplayerClient)
				NPC.SpawnOnPlayer(player.whoAmI, type);
			else
				NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: type);

			return true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.StoneBlock, 25)
				.AddIngredient(ItemID.Lens, 3)
				.AddIngredient(ItemID.SoulofNight, 5)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}
}
