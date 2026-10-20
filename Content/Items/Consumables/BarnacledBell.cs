using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.NPCs.Bosses;

namespace Terrapex.Content.Items.Consumables
{
	/// <summary>
	/// Summons the Old Shellback: a sunken ship's bell, rung on the beach. Barnacles from the
	/// crabs, seashells and an iron or lead bar - an Ocean trip after the Eye (BOSS_SHELLBACK.md).
	/// </summary>
	public class BarnacledBell : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 3;
			ItemID.Sets.SortingPriorityBossSpawns[Type] = 4;
		}

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 28;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 10);
			Item.rare = ItemRarityID.Blue;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.consumable = true;
		}

		public override bool CanUseItem(Player player)
			=> player.ZoneBeach && !NPC.AnyNPCs(ModContent.NPCType<OldShellback>());

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI != Main.myPlayer)
				return true;
			SoundEngine.PlaySound(SoundID.Item35 with { Pitch = -0.8f }, player.position);
			SoundEngine.PlaySound(SoundID.Roar, player.position);
			int type = ModContent.NPCType<OldShellback>();
			if (Main.netMode != NetmodeID.MultiplayerClient)
				NPC.SpawnOnPlayer(player.whoAmI, type);
			else
				NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: type);
			return true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<BarnacleCluster>(8)
				.AddIngredient(ItemID.Seashell, 3)
				.AddRecipeGroup("IronBar", 4)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
