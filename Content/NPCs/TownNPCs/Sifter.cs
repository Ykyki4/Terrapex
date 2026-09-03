using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Systems;
using Terrapex.Content.Items.Accessories;
using Terrapex.Content.Items.Consumables;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Items.Weapons;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.NPCs.TownNPCs
{
	/// <summary>
	/// The Sifter — the one thing that came up the crack and decided to stay.
	///
	/// It exists so the Breach has somewhere to spend itself. Ejecta only drops inside the event,
	/// and a currency with no shop is a drop with extra steps; a shop priced in coins would be a
	/// shop the player could fill from anywhere. Together they make the event repeatable on
	/// purpose: you run it again because you chose what it was for.
	///
	/// It moves in once the Heave is dead, which is also the gate on the whole stock — nothing it
	/// sells is reachable before the event has been finished once.
	/// </summary>
	[AutoloadHead]
	public class Sifter : ModNPC
	{
		public const string ShopName = "Shop";

		public const int SlabmaulPrice = 60;
		public const int OverburdenPrice = 45;
		public const int SoundingRodPrice = 30;
		public const int FlarePrice = 12;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 25;

			NPCID.Sets.ExtraFramesCount[Type] = 9;
			NPCID.Sets.AttackFrameCount[Type] = 4;
			NPCID.Sets.DangerDetectRange[Type] = 700;
			NPCID.Sets.AttackType[Type] = 0;            // it throws rock. It has nothing else
			NPCID.Sets.AttackTime[Type] = 90;
			NPCID.Sets.AttackAverageChance[Type] = 30;
			NPCID.Sets.HatOffsetY[Type] = 4;
			NPCID.Sets.ShimmerTownTransform[Type] = false;

			NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new() { Velocity = 1f };
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);
		}

		public override void SetDefaults()
		{
			NPC.townNPC = true;
			NPC.friendly = true;
			NPC.width = 18;
			NPC.height = 40;
			NPC.aiStyle = 7;
			NPC.damage = 10;
			NPC.defense = 30;
			NPC.lifeMax = 350;
			NPC.HitSound = SoundID.NPCHit41;
			NPC.DeathSound = SoundID.NPCDeath43;
			NPC.knockBackResist = 0.4f;

			AnimationType = NPCID.Guide;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
			=> bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Caverns,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.Sifter.Bestiary")
			});

		/// <summary>
		/// It arrives once the Breach has been closed the hard way. Before that there is no
		/// Ejecta in the world, so there would be nothing to say to it.
		/// </summary>
		public override bool CanTownNPCSpawn(int numTownNPCs) => BreachSystem.downedHeave;

		public override List<string> SetNPCNameList() => new() { "Torv", "Skarn", "Vess", "Dolo", "Marl" };

		public override string GetChat()
		{
			int line = Main.rand.Next(4);
			if (line == 3 && !BreachSystem.Active)
				line = 0;
			return Language.GetTextValue("Mods.Terrapex.Chat.Sifter" + line);
		}

		public override void SetChatButtons(ref string button, ref string button2)
			=> button = Language.GetTextValue("LegacyInterface.28");

		public override void OnChatButtonClicked(bool firstButton, ref string shop)
		{
			if (firstButton)
				shop = ShopName;
		}

		/// <summary>
		/// The stock, priced in Ejecta. One weapon and two accessories, deliberately: T3 already
		/// carries six boss weapons and a seventh adds nothing, while an accessory that continues
		/// the event's own mechanic does. The flare is here too, cheaply, so a player who has run
		/// the Breach once can go straight back in without a trip to the altar.
		/// </summary>
		public override void AddShops()
		{
			new NPCShop(Type, ShopName)
				.Add(Priced<Slabmaul>(SlabmaulPrice))
				.Add(Priced<Overburden>(OverburdenPrice))
				.Add(Priced<SoundingRod>(SoundingRodPrice))
				.Add(Priced<BreachFlare>(FlarePrice))
				.Register();
		}

		private static Item Priced<T>(int ejecta) where T : ModItem
			=> new(ModContent.ItemType<T>())
			{
				shopCustomPrice = ejecta,
				shopSpecialCurrency = EjectaCurrency.Id
			};

		// ------------------------------------------------------------------ when the town is attacked

		public override void TownNPCAttackStrength(ref int damage, ref float knockback)
		{
			damage = 30;
			knockback = 4f;
		}

		public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
		{
			cooldown = 24;
			randExtraCooldown = 18;
		}

		public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
		{
			projType = ModContent.ProjectileType<ScreeStone>();
			attackDelay = 6;
		}

		public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection,
			ref float randomOffset)
		{
			multiplier = 10f;
			gravityCorrection = 4f;
			randomOffset = 1.4f;
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 24 : 4); i++)
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Stone,
					hit.HitDirection, -1f, 100, default, 1.05f);
				d.noGravity = NPC.life <= 0;
			}
		}

		public override void AI()
		{
			// it is a piece of the far side of the crack, and it never stops showing it
			if (Main.rand.NextBool(22))
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height,
					DustID.PurpleTorch, 0f, 0f, 150, default, 0.75f);
				d.noGravity = true;
				d.velocity *= 0.15f;
			}
			Lighting.AddLight(NPC.Center, 0.20f, 0.07f, 0.26f);
		}
	}
}
