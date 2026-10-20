using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Personalities;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Content.Items.Accessories;
using Terrapex.Content.Items.Armor;
using Terrapex.Content.Items.Weapons.Reaper;

namespace Terrapex.Content.NPCs.TownNPCs
{
	/// <summary>
	/// The Ronin - a wandering swordsman who traded the katana for the kama. The reaper class's NPC,
	/// see <c>CLASS_REAPER.md</c>.
	///
	/// He moves in once somebody carries a scythe, the way the Arms Dealer follows a gun: the class
	/// starts with a crafted sickle, and the NPC is how a player who picked it up finds the rest.
	/// Everything past the Whetstone waits for Skeletron, which is where his set sits in the tiers.
	/// </summary>
	[AutoloadHead]
	public class Ronin : ModNPC
	{
		public const string ShopName = "Shop";

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 25;

			NPCID.Sets.ExtraFramesCount[Type] = 9;
			NPCID.Sets.AttackFrameCount[Type] = 4;
			NPCID.Sets.DangerDetectRange[Type] = 500;
			NPCID.Sets.AttackType[Type] = 3;            // a swing, with his own sickle
			NPCID.Sets.AttackTime[Type] = 24;
			NPCID.Sets.AttackAverageChance[Type] = 15;
			NPCID.Sets.HatOffsetY[Type] = 4;

			NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new() { Velocity = 1f };
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);

			NPC.Happiness
				.SetBiomeAffection<ForestBiome>(AffectionLevel.Like)
				.SetBiomeAffection<SnowBiome>(AffectionLevel.Dislike)
				.SetNPCAffection(NPCID.Merchant, AffectionLevel.Dislike)
				.SetNPCAffection(NPCID.Clothier, AffectionLevel.Like);
		}

		public override void SetDefaults()
		{
			NPC.townNPC = true;
			NPC.friendly = true;
			NPC.width = 18;
			NPC.height = 40;
			NPC.aiStyle = 7;
			NPC.damage = 10;
			NPC.defense = 20;
			NPC.lifeMax = 300;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.knockBackResist = 0.5f;

			AnimationType = NPCID.Guide;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
			=> bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.Ronin.Bestiary")
			});

		public override bool CanTownNPCSpawn(int numTownNPCs)
		{
			foreach (Player player in Main.ActivePlayers)
			{
				foreach (Item item in player.inventory)
				{
					if (!item.IsAir && item.ModItem is ScytheItem)
						return true;
				}
			}
			return false;
		}

		public override List<string> SetNPCNameList() => new() { "Kenshiro", "Takeda", "Musashi", "Jubei", "Sanjuro", "Hanzo" };

		public override string GetChat()
		{
			int line = Main.rand.Next(5);
			if (line == 4 && !NPC.downedBoss3)
				line = 0;
			return Language.GetTextValue("Mods.Terrapex.Chat.Ronin" + line);
		}

		public override void SetChatButtons(ref string button, ref string button2)
			=> button = Language.GetTextValue("LegacyInterface.28");

		public override void OnChatButtonClicked(bool firstButton, ref string shop)
		{
			if (firstButton)
				shop = ShopName;
		}

		public override void AddShops()
		{
			new NPCShop(Type, ShopName)
				.Add<Whetstone>()
				.Add<HarvestCharm>(Condition.DownedEyeOfCthulhu)
				.Add<Kusarigama>(Condition.DownedSkeletron)
				.Add<RoninKabuto>(Condition.DownedSkeletron)
				.Add<RoninDo>(Condition.DownedSkeletron)
				.Add<RoninSuneate>(Condition.DownedSkeletron)
				.Register();
		}

		// ------------------------------------------------------------------ when the town is attacked

		public override void TownNPCAttackStrength(ref int damage, ref float knockback)
		{
			damage = NPC.downedBoss3 ? 40 : 20;
			knockback = 5f;
		}

		public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
		{
			cooldown = 18;
			randExtraCooldown = 10;
		}

		public override void TownNPCAttackSwing(ref int itemWidth, ref int itemHeight)
		{
			itemWidth = 44;
			itemHeight = 44;
		}

		public override void DrawTownAttackSwing(ref Texture2D item, ref Rectangle itemFrame, ref int itemSize,
			ref float scale, ref Vector2 offset)
		{
			int type = ModContent.ItemType<Kusarigama>();
			Main.instance.LoadItem(type);
			item = TextureAssets.Item[type].Value;
			itemFrame = item.Bounds;
			itemSize = 44;
			scale = 1f;
		}
	}
}
