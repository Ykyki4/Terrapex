using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.NPCs.Bosses;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Consumables
{
	/// <summary>
	/// Plan item #111. Summons the First Keeper — and it is the Fractured Eye put back
	/// together, which is the loop the whole mod has been walking toward: the thing you cracked
	/// open to start hardmode is a piece of the thing that has been watching since before it.
	/// </summary>
	public class UnbrokenEye : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 3;
			ItemID.Sets.SortingPriorityBossSpawns[Type] = 20;
		}

		public override void SetDefaults()
		{
			Item.width = 28;
			Item.height = 28;
			Item.maxStack = Item.CommonMaxStack;
			Item.rare = ItemRarityID.Red;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.consumable = true;
		}

		/// <summary>
		/// The Lord is the floor, checked here as well as on the recipe: the eye can arrive out
		/// of a treasure bag or another player's chest, and there is nothing on the far side of
		/// this fight to progress to if it is taken early.
		/// </summary>
		public override bool CanUseItem(Player player)
			=> Terraria.NPC.downedMoonlord && !NPC.AnyNPCs(ModContent.NPCType<FirstKeeper>());

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI == Main.myPlayer)
			{
				SoundEngine.PlaySound(SoundID.Roar, player.position);
				if (Main.netMode != NetmodeID.MultiplayerClient)
					NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<FirstKeeper>());
				else
					NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI,
						number2: ModContent.NPCType<FirstKeeper>());
			}
			return true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Primordium>(15)
				.AddIngredient<FracturedEye>(1)
				.AddIngredient(ItemID.LunarBar, 12)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.AddCondition(Condition.DownedMoonLord)
				.Register();
		}
	}
}
