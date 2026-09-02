using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.NPCs.Bosses;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Consumables
{
	/// <summary>Plan item #92. Summons the Weaver of the Rift.</summary>
	public class ThreadSkein : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 3;
			ItemID.Sets.SortingPriorityBossSpawns[Type] = 13;
		}

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 26;
			Item.maxStack = Item.CommonMaxStack;
			Item.rare = ItemRarityID.Lime;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.consumable = true;
		}

		/// <summary>
		/// Plantera is the floor, checked here as well as on the recipe: the skein can arrive
		/// out of a treasure bag or another player's chest, and a post-Plantera boss summoned
		/// straight after the mechanicals is not a fight, it is a flattening.
		/// </summary>
		public override bool CanUseItem(Player player)
			=> Terraria.NPC.downedPlantBoss && !NPC.AnyNPCs(ModContent.NPCType<WeaverOfTheRift>());

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI == Main.myPlayer)
			{
				SoundEngine.PlaySound(SoundID.Roar, player.position);
				if (Main.netMode != NetmodeID.MultiplayerClient)
					NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<WeaverOfTheRift>());
				else
					NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI,
						number2: ModContent.NPCType<WeaverOfTheRift>());
			}
			return true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<VoidThread>(20)
				.AddIngredient<VoidCloth>(8)
				.AddIngredient(ItemID.SoulofFright, 10)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.AddCondition(Condition.DownedPlantera)
				.Register();
		}
	}
}
