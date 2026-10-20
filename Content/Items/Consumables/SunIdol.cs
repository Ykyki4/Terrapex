using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.NPCs.Bosses;

namespace Terrapex.Content.Items.Consumables
{
	/// <summary>
	/// Summons the Scarab Sultan: by day, in the surface desert. Shadow Scales or Tissue Samples put
	/// it after the Eater of Worlds / Brain of Cthulhu, which is where the fight sits (BOSS_SCARAB.md).
	/// </summary>
	public class SunIdol : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 3;
			ItemID.Sets.SortingPriorityBossSpawns[Type] = 12;
		}

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 30;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 30);
			Item.rare = ItemRarityID.Green;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.consumable = true;
		}

		public override bool CanUseItem(Player player)
			=> Main.dayTime && player.ZoneDesert && player.ZoneOverworldHeight
				&& !NPC.AnyNPCs(ModContent.NPCType<ScarabSultan>());

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI != Main.myPlayer)
				return true;
			SoundEngine.PlaySound(SoundID.Roar, player.position);
			int type = ModContent.NPCType<ScarabSultan>();
			if (Main.netMode != NetmodeID.MultiplayerClient)
				NPC.SpawnOnPlayer(player.whoAmI, type);
			else
				NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: type);
			return true;
		}

		public override void AddRecipes()
		{
			foreach (int gate in new[] { ItemID.ShadowScale, ItemID.TissueSample })
			{
				CreateRecipe()
					.AddIngredient(ItemID.SandBlock, 20)
					.AddIngredient(ItemID.AntlionMandible, 2)
					.AddIngredient<GildedChitin>(5)
					.AddIngredient<Sunsand>(5)
					.AddIngredient(gate, 5)
					.AddTile(TileID.Anvils)
					.Register();
			}
		}
	}
}
