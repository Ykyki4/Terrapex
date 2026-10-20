using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Accessories;

namespace Terrapex.Content.NPCs.Wilds
{
	/// <summary>
	/// A forest boar with a back of thorns. It walks like a zombie until a player is level with it
	/// and in range, then it stops and scrapes the ground for 36 ticks - dust flying back off its
	/// hoof, the tell - and charges in a straight line it has already committed to. Step off the
	/// line and it runs past. Part of the mod's world beyond the rift: an ordinary animal, made
	/// readable.
	///
	/// ThornbackBoar.png: frames 0-3 the trot, 4 the scrape.
	/// </summary>
	public class ThornbackBoar : ModNPC
	{
		private const int ScrapeTicks = 36;
		private const int ChargeTicks = 40;
		private const float ChargeSpeed = 9f;
		private const int Cooldown = 150;

		private const float Walk = 0f, Scrape = 1f, Charge = 2f;

		private ref float State => ref NPC.ai[2];
		private ref float Timer => ref NPC.ai[3];

		public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 5;

		public override void SetDefaults()
		{
			NPC.width = 40;
			NPC.height = 26;
			NPC.damage = 18;
			NPC.defense = 6;
			NPC.lifeMax = 70;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.knockBackResist = 0.4f;
			NPC.value = Item.buyPrice(silver: 1);
			NPC.aiStyle = NPCAIStyleID.Fighter;
			AIType = NPCID.Zombie;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
			=> spawnInfo.Player.ZoneForest && Main.dayTime && spawnInfo.SpawnTileY < Main.worldSurface ? 0.12f : 0f;

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
			=> bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.DayTime,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.ThornbackBoar.Bestiary")
			});

		public override void PostAI()
		{
			Player target = Main.player[NPC.target];
			if (Timer > 0f)
				Timer--;

			if (State == Walk)
			{
				bool level = Math.Abs(target.Center.Y - NPC.Center.Y) < 48f;
				bool near = Math.Abs(target.Center.X - NPC.Center.X) < 340f;
				if (Timer <= 0f && level && near && NPC.collideY && target.active && !target.dead)
				{
					State = Scrape;
					Timer = ScrapeTicks;
					NPC.direction = NPC.spriteDirection = target.Center.X > NPC.Center.X ? 1 : -1;
					NPC.netUpdate = true;
				}
				return;
			}

			if (State == Scrape)
			{
				NPC.velocity.X = 0f;
				if (Main.rand.NextBool(2))
					Dust.NewDustDirect(NPC.BottomLeft + new Vector2(NPC.direction < 0 ? NPC.width - 8 : 0, -4), 8, 4,
						DustID.Dirt, -NPC.direction * 3f, -2f, 60, default, 1f);
				if (Timer <= 0f)
				{
					State = Charge;
					Timer = ChargeTicks;
					Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit25 with { Pitch = -0.3f }, NPC.Center);
				}
				return;
			}

			// the charge: a line chosen before it started, not a chase
			NPC.velocity.X = NPC.direction * ChargeSpeed;
			NPC.spriteDirection = NPC.direction;
			NPC.damage = (int)(NPC.defDamage * 1.5f);
			if (NPC.collideX || Timer <= 0f)
			{
				State = Walk;
				Timer = Cooldown;
				NPC.damage = NPC.defDamage;
				NPC.netUpdate = true;
			}
		}

		public override void FindFrame(int frameHeight)
		{
			if (State == Scrape)
			{
				NPC.frame.Y = 4 * frameHeight;
				return;
			}
			NPC.frameCounter += Math.Abs(NPC.velocity.X) * (State == Charge ? 0.35f : 0.25f);
			if (NPC.frameCounter >= 6)
			{
				NPC.frameCounter = 0;
				NPC.frame.Y = (NPC.frame.Y + frameHeight) % (4 * frameHeight);
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ItemID.Leather, 2, 1, 2));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<TuskCharm>(), 30));
		}
	}
}
