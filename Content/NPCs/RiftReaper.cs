using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.NPCs
{
	/// <summary>
	/// Plan mob #10. It hangs back, draws a line, and crosses the room on it.
	///
	/// The line is the whole enemy. It is the same telegraph the Weaver and the First Keeper
	/// use for their dashes, at a fraction of the length, so the cavern is where a player
	/// learns to read it before a boss makes them.
	/// </summary>
	public class RiftReaper : ModNPC
	{
		private const int Hover = 0;
		private const int Aim = 1;
		private const int Dash = 2;

		private const int AimTicks = 32;
		private const int DashTicks = 26;
		private const float Standoff = 240f;

		private ref float State => ref NPC.ai[0];
		private ref float Timer => ref NPC.ai[1];
		private ref float Beat => ref NPC.ai[2];

		private Vector2 aimA, aimB;
		private float aimStrength;

		public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 4;

		public override void SetDefaults()
		{
			NPC.width = 38;
			NPC.height = 30;
			NPC.damage = 48;
			NPC.defense = 14;
			NPC.lifeMax = 260;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.value = Item.buyPrice(silver: 34);
			NPC.knockBackResist = 0.15f;
			NPC.noGravity = true;
			NPC.noTileCollide = false;
			NPC.aiStyle = -1;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Underground,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.RiftReaper.Bestiary")
			});
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.PlayerInTown || spawnInfo.Water || !Main.hardMode)
				return 0f;
			return spawnInfo.Player.ZoneRockLayerHeight ? 0.05f : 0f;
		}

		public override void AI()
		{
			NPC.TargetClosest();
			Player target = Main.player[NPC.target];
			Beat++;
			Timer++;
			aimStrength = 0f;

			if (!target.active || target.dead)
			{
				NPC.velocity.Y -= 0.2f;
				if (NPC.timeLeft > 60)
					NPC.timeLeft = 60;
				return;
			}

			switch ((int)State)
			{
				case Aim:
				{
					// it holds still while it aims: a telegraph you have to chase is not one
					NPC.velocity *= 0.88f;
					aimA = NPC.Center;
					aimB = NPC.Center + (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX) * 700f;
					aimStrength = Timer / AimTicks;
					if (Timer >= AimTicks)
					{
						NPC.velocity = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX) * 13.5f;
						SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
						State = Dash;
						Timer = 0f;
						NPC.netUpdate = true;
					}
					break;
				}

				case Dash:
				{
					NPC.velocity *= 0.985f;
					if (Main.rand.NextBool(2))
					{
						Dust d = Dust.NewDustPerfect(NPC.Center, DustID.PurpleTorch,
							NPC.velocity * -0.1f, 120, default, 1.2f);
						d.noGravity = true;
					}
					if (Timer >= DashTicks)
					{
						State = Hover;
						Timer = 0f;
						NPC.netUpdate = true;
					}
					break;
				}

				default:
				{
					Vector2 away = (NPC.Center - target.Center).SafeNormalize(Vector2.UnitX);
					Vector2 seat = target.Center + away * Standoff
						+ new Vector2(0f, (float)Math.Sin(Beat * 0.05f) * 26f - 40f);
					Vector2 want = (seat - NPC.Center) * 0.05f;
					if (want.Length() > 7f)
						want = Vector2.Normalize(want) * 7f;
					NPC.velocity = Vector2.Lerp(NPC.velocity, want, 0.1f);
					if (Timer >= 54f)
					{
						State = Aim;
						Timer = 0f;
						NPC.netUpdate = true;
					}
					break;
				}
			}

			NPC.spriteDirection = NPC.direction = NPC.velocity.X > 0f ? 1 : -1;
			NPC.rotation = NPC.velocity.X * 0.02f;
			Lighting.AddLight(NPC.Center, 0.30f, 0.10f, 0.38f);
		}

		public override void FindFrame(int frameHeight)
		{
			if (++NPC.frameCounter >= ((int)State == Dash ? 3.0 : 6.0))
			{
				NPC.frameCounter = 0.0;
				NPC.frame.Y = (NPC.frame.Y + frameHeight) % (frameHeight * Main.npcFrameCount[Type]);
			}
		}

		public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			if (aimStrength <= 0f)
				return;
			float f = aimStrength;
			RiftDraw.Line(aimA, aimB, RiftDraw.Glow(230, 120, 235, 0.08f + f * 0.26f), 1.8f + f * 2.4f);
			RiftDraw.Line(aimA, aimB, RiftDraw.Glow(255, 242, 251, 0.06f + f * 0.24f), 1f);
			RiftDraw.Bloom(aimA, RiftDraw.Glow(255, 154, 217, 0.3f + f * 0.45f), 0.35f + f * 0.35f);
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 26 : 4); i++)
			{
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.PurpleTorch,
					hit.HitDirection, -1f, 110, default, 1.2f);
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RiftEssence>(), 2, 1, 2));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<PlateShard>(), 4, 1, 1));
		}
	}
}
