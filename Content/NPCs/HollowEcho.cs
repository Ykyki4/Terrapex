using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Accessories;

namespace Terrapex.Content.NPCs
{
	/// <summary>
	/// Plan mob #7. A dungeon ghost that ignores walls, which would be unfair if it were
	/// silent — so it is not. Every charge is announced a full 40 ticks early by a tick
	/// of sound and a hard brightening, and it only deals damage while charging.
	/// </summary>
	public class HollowEcho : ModNPC
	{
		private const int Cycle = 150;
		private const int TellAt = 40;    // ticks of warning before the charge
		private const int ChargeFor = 46;

		private ref float Timer => ref NPC.ai[0];

		private bool Winding => Timer >= Cycle - TellAt - ChargeFor && Timer < Cycle - ChargeFor;
		private bool Charging => Timer >= Cycle - ChargeFor;

		public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 6;

		public override void SetDefaults()
		{
			NPC.width = 32;
			NPC.height = 40;
			NPC.damage = 40;
			NPC.defense = 6;
			NPC.lifeMax = 160;
			NPC.HitSound = SoundID.NPCHit36;
			NPC.DeathSound = SoundID.NPCDeath39;
			NPC.value = Item.buyPrice(silver: 20);
			NPC.knockBackResist = 0f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.aiStyle = -1;
			NPC.alpha = 90;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.TheDungeon,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.HollowEcho.Bestiary")
			});
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.PlayerInTown || !NPC.downedBoss3)
				return 0f;
			return spawnInfo.Player.ZoneDungeon ? 0.09f : 0f;
		}

		public override void AI()
		{
			if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
				NPC.TargetClosest();

			Player target = Main.player[NPC.target];
			Timer++;

			// harmless except mid-charge: a wall-ignoring mob that always hurts on contact
			// is a mob you cannot do anything about
			NPC.damage = Charging ? 40 : 0;

			if (Charging)
			{
				NPC.velocity *= 0.995f;
				NPC.alpha = 20;
			}
			else if (Winding)
			{
				// stop dead and aim: the tell is that it is the only time it holds still
				NPC.velocity *= 0.86f;
				NPC.alpha = (int)MathHelper.Lerp(90f, 30f, (Timer - (Cycle - TellAt - ChargeFor)) / TellAt);

				if (Timer == Cycle - TellAt - ChargeFor + 1)
					SoundEngine.PlaySound(SoundID.Item104 with { Volume = 0.7f, Pitch = -0.4f }, NPC.Center);

				if (Timer == Cycle - ChargeFor - 1)
				{
					NPC.velocity = NPC.DirectionTo(target.Center) * 10.5f;
					SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = -0.6f }, NPC.Center);
				}
			}
			else
			{
				// drift: slow enough that the player chooses the range, not the ghost
				Vector2 want = NPC.DirectionTo(target.Center) * 2.6f;
				NPC.velocity = Vector2.Lerp(NPC.velocity, want, 0.04f);
				NPC.alpha = 90;
			}

			if (Timer >= Cycle)
				Timer = 0f;

			NPC.spriteDirection = NPC.direction = NPC.velocity.X >= 0f ? 1 : -1;
			NPC.rotation = NPC.velocity.X * 0.03f;

			Lighting.AddLight(NPC.Center, 0.18f, 0.06f, 0.26f);
			if ((Winding || Charging) && Main.rand.NextBool(3))
			{
				Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(16f, 20f),
					DustID.PurpleTorch, Vector2.Zero, 120, default, 1.05f);
				d.noGravity = true;
			}
		}

		public override void FindFrame(int frameHeight)
		{
			// 0-3 drift, 4-5 the charge, so the animation carries the same warning the sound does
			if (Winding || Charging)
			{
				if (++NPC.frameCounter >= 6.0)
				{
					NPC.frameCounter = 0;
					NPC.frame.Y = NPC.frame.Y / frameHeight == 4 ? frameHeight * 5 : frameHeight * 4;
				}
				return;
			}

			if (++NPC.frameCounter >= 9.0)
			{
				NPC.frameCounter = 0;
				int f = NPC.frame.Y / frameHeight;
				NPC.frame.Y = (f >= 3 ? 0 : f + 1) * frameHeight;
			}
		}

		public override Color? GetAlpha(Color drawColor)
			=> Charging ? Color.White * 0.9f : new Color(210, 190, 235) * ((255 - NPC.alpha) / 255f);

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(
				ModContent.ItemType<EchoPendant>(), 25));
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			int count = NPC.life > 0 ? 3 : 20;
			for (int i = 0; i < count; i++)
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.PurpleTorch,
					Main.rand.NextFloat(-2.5f, 2.5f), Main.rand.NextFloat(-2.5f, 2.5f), 120, default, 1.1f);
				d.noGravity = true;
			}
		}
	}
}
