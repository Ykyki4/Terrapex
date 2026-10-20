using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Wilds;

namespace Terrapex.Content.NPCs.Wilds
{
	/// <summary>
	/// A pitcher plant grown big enough to spit. It does not move. Every couple of seconds it swells
	/// for 24 ticks - the tell - and lobs a glob of acid on an arc solved to land where the player
	/// was standing when it fired. The glob does not steer, so walking keeps you dry.
	///
	/// PitcherSnapper.png: frame 0 idle, 1 swelling, 2 spitting.
	/// </summary>
	public class PitcherSnapper : ModNPC
	{
		private const int Interval = 110;
		private const int SwellTicks = 24;
		private const float Flight = 40f;

		private ref float Timer => ref NPC.ai[0];

		public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 3;

		public override void SetDefaults()
		{
			NPC.width = 30;
			NPC.height = 44;
			NPC.damage = 22;
			NPC.defense = 10;
			NPC.lifeMax = 110;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.knockBackResist = 0f;
			NPC.value = Item.buyPrice(silver: 2);
			NPC.aiStyle = -1;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
			=> spawnInfo.Player.ZoneJungle && !spawnInfo.Water ? 0.10f : 0f;

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
			=> bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Jungle,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.PitcherSnapper.Bestiary")
			});

		public override void AI()
		{
			NPC.TargetClosest(false);
			Player target = Main.player[NPC.target];
			NPC.velocity.X = 0f;
			NPC.spriteDirection = target.Center.X > NPC.Center.X ? 1 : -1;
			if (!target.active || target.dead || NPC.Distance(target.Center) > 560f)
			{
				Timer = 0f;
				return;
			}

			Timer++;
			if (Timer == Interval)
				SoundEngine.PlaySound(SoundID.Item13 with { Pitch = -0.4f }, NPC.Center);
			if (Timer >= Interval + SwellTicks)
			{
				Timer = 0f;
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					// a ballistic solve, fixed flight time: it lands where the player stood
					Vector2 mouth = NPC.Top + new Vector2(0f, 6f);
					Vector2 to = target.Center - mouth;
					Vector2 v = new(to.X / Flight, to.Y / Flight - 0.5f * PitcherSpit.Gravity * Flight);
					Projectile.NewProjectile(NPC.GetSource_FromAI(), mouth, v, ModContent.ProjectileType<PitcherSpit>(),
						NPC.GetAttackDamage_ForProjectiles(14, 12), 0f, Main.myPlayer);
				}
				SoundEngine.PlaySound(SoundID.NPCDeath13 with { Volume = 0.5f, Pitch = 0.4f }, NPC.Center);
			}
		}

		public override void FindFrame(int frameHeight)
		{
			int f = Timer >= Interval ? 1 : (Timer < 10f && Timer > 0f ? 2 : 0);
			NPC.frame.Y = f * frameHeight;
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ItemID.JungleSpores, 2, 1, 3));
			npcLoot.Add(ItemDropRule.Common(ItemID.Vine, 4));
		}
	}
}
