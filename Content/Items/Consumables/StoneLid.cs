using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Placeable;
using Terrapex.Content.NPCs.Bosses;

namespace Terrapex.Content.Items.Consumables
{
	/// <summary>
	/// Plan item #20. Summons the Dormant Eye, and only underground — the fight is a
	/// cave fight, and the rockfall has nothing to fall from on the surface.
	/// </summary>
	public class StoneLid : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 3;
			ItemID.Sets.SortingPriorityBossSpawns[Type] = 11;
		}

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 24;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 10);
			Item.rare = ItemRarityID.Blue;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.consumable = true;
		}

		public override bool CanUseItem(Player player)
			=> player.ZoneRockLayerHeight || player.ZoneDirtLayerHeight
			   ? !NPC.AnyNPCs(ModContent.NPCType<DormantEye>())
			   : false;

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI != Main.myPlayer)
				return true;

			SoundEngine.PlaySound(SoundID.Roar, player.position);

			int type = ModContent.NPCType<DormantEye>();
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
				.AddIngredient(ModContent.ItemType<FissuriteOre>(), 6)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
