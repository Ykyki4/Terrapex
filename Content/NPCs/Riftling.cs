using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Placeable;

namespace Terrapex.Content.NPCs
{
	public class Riftling : ModNPC
	{
		// Riftling.png: frames 0-7 idle loop, frames 8-17 the lunge
		private const int IdleFrames = 8;
		private const int AttackFrame = 8;
		private const int AttackFrames = 10;
		private const int AttackLength = 60;

		private ref float Timer => ref NPC.ai[0];
		private ref float Attacking => ref NPC.ai[1];

		private int loopFrame;
		private double loopCounter;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 18;
		}

		public override void SetDefaults()
		{
			NPC.width = 30;
			NPC.height = 30;
			NPC.damage = 26;
			NPC.defense = 8;
			NPC.lifeMax = 110;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.value = Item.buyPrice(silver: 12);
			NPC.knockBackResist = 0.35f;
			NPC.noGravity = true;
			NPC.noTileCollide = false;
			NPC.aiStyle = -1;
			Banner = Item.NPCtoBanner(NPC.type);
			BannerItem = ModContent.ItemType<RiftlingBanner>();
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Underground,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.Riftling.Bestiary")
			});
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.PlayerInTown || spawnInfo.Water)
				return 0f;
			return spawnInfo.Player.ZoneRockLayerHeight ? 0.055f : 0f;
		}

		public override void AI()
		{
			if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
				NPC.TargetClosest();

			Player target = Main.player[NPC.target];
			Timer++;

			if (Attacking == 0f)
			{
				Vector2 desired = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitY) * 3.2f;
				NPC.velocity = Vector2.Lerp(NPC.velocity, desired, 0.03f);

				if (Timer > 150f && NPC.Distance(target.Center) < 380f)
				{
					Attacking = 1f;
					Timer = 0f;
					NPC.netUpdate = true;
				}
			}
			else
			{
				// wind up, then lunge — matches the attack animation beat for beat
				if (Timer < 28f)
				{
					NPC.velocity *= 0.93f;
				}
				else if (Timer == 28f)
				{
					SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
					NPC.velocity = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitY) * 9f;
					NPC.netUpdate = true;
				}
				else
				{
					NPC.velocity *= 0.985f;
				}

				if (Timer > AttackLength)
				{
					Attacking = 0f;
					Timer = 0f;
					NPC.netUpdate = true;
				}
			}

			if (Main.rand.NextBool(14))
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.PurpleTorch,
					0f, 0f, 100, default, 0.9f);
				d.noGravity = true;
				d.velocity *= 0.3f;
			}
		}

		public override void FindFrame(int frameHeight)
		{
			int frame;

			if (Attacking == 1f)
			{
				int f = (int)(Timer / (AttackLength / (float)AttackFrames));
				frame = AttackFrame + Math.Clamp(f, 0, AttackFrames - 1);
			}
			else
			{
				loopCounter += 1.0;
				if (loopCounter >= 7.0)
				{
					loopCounter = 0.0;
					loopFrame = (loopFrame + 1) % IdleFrames;
				}
				frame = loopFrame;
			}

			NPC.frame.Y = frame * frameHeight;
		}

		public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			Texture2D glow = ModContent.Request<Texture2D>(Texture + "_Glow").Value;
			spriteBatch.Draw(glow, NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY),
				NPC.frame, Color.White, NPC.rotation, NPC.frame.Size() * 0.5f,
				NPC.scale, SpriteEffects.None, 0f);
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 22 : 4); i++)
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height,
					Main.rand.NextBool() ? DustID.PurpleTorch : DustID.PinkTorch,
					hit.HitDirection, -1f, 100, default, 1.2f);
				d.noGravity = true;
			}
		}
	}
}
