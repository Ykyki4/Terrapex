using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.NPCs.Bosses;

namespace Terrapex.Content.Items.Consumables
{
	/// <summary>
	/// Summons the Rime, in the Snow biome. An old mountain horn crusted with ice; the bone in it
	/// puts the fight after Skeletron, which is where it is meant to sit (BOSS_RIME.md).
	/// </summary>
	public class FrozenHorn : ModItem
	{
		public override void SetStaticDefaults()
		{
			Item.ResearchUnlockCount = 3;
			ItemID.Sets.SortingPriorityBossSpawns[Type] = 12;
		}

		public override void SetDefaults()
		{
			Item.width = 30;
			Item.height = 26;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 20);
			Item.rare = ItemRarityID.Orange;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.consumable = true;
		}

		public override bool CanUseItem(Player player)
			=> player.ZoneSnow && !NPC.AnyNPCs(ModContent.NPCType<RimeColossus>());

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI != Main.myPlayer)
				return true;
			SoundEngine.PlaySound(SoundID.Roar with { Pitch = -0.6f }, player.position);
			int type = ModContent.NPCType<RimeColossus>();
			if (Main.netMode != NetmodeID.MultiplayerClient)
				NPC.SpawnOnPlayer(player.whoAmI, type);
			else
				NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: type);
			return true;
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient(ItemID.IceBlock, 20)
				.AddIngredient(ItemID.SnowBlock, 10)
				.AddIngredient(ItemID.Bone, 8)
				.AddIngredient(ModContent.ItemType<FrostPelt>(), 3)
				.AddIngredient(ModContent.ItemType<RimeCrystal>(), 3)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
