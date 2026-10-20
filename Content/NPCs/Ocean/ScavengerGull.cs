using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;

namespace Terrapex.Content.NPCs.Ocean
{
	/// <summary>
	/// A gull that has learned the beach is full of things worth stealing from. It wheels over
	/// the player doing no harm at all; then it stops, cries, and a line is drawn from it through
	/// where the player stands - 34 ticks later it dives along that line, and only then does it
	/// hurt. After the dive it climbs back up and wheels again. The boss's scuttle, as a bird:
	/// the line is the whole tell, and stepping off it is the whole answer.
	///
	/// ScavengerGull.png: four 36x26 frames, stacked. 0-2 flapping, 3 diving.
	/// </summary>
	public class ScavengerGull : ModNPC
	{
		public const int Wheel = 150, Tell = 34, Dive = 28, Climb = 40;
		public const float DiveSpeed = 12f;

		private const int Wheeling = 0, Aiming = 1, Diving = 2, Climbing = 3;

		private ref float State => ref NPC.ai[0];
		private ref float Timer => ref NPC.ai[1];
		private ref float Aim => ref NPC.ai[2];

		public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 4;

		public override void SetDefaults()
		{
			NPC.width = 30;
			NPC.height = 20;
			NPC.damage = 18;
			NPC.defense = 2;
			NPC.lifeMax = 38;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath4;
			NPC.value = Item.buyPrice(silver: 1, copper: 50);
			NPC.knockBackResist = 0.6f;
			NPC.aiStyle = -1;
			NPC.noGravity = true;
			NPC.noTileCollide = false;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
			=> bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Ocean,
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.DayTime,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.ScavengerGull.Bestiary")
			});

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.PlayerInTown || spawnInfo.Water || !spawnInfo.Player.ZoneBeach || !Main.dayTime)
				return 0f;
			return 0.16f;
		}

		public override void AI()
		{
			NPC.TargetClosest(false);
			Player target = Main.player[NPC.target];
			if (!target.active || target.dead)
			{
				NPC.velocity.Y -= 0.1f;
				NPC.EncourageDespawn(60);
				return;
			}

			Timer++;
			switch ((int)State)
			{
				case Wheeling:
					{
						// wheel over the player, bobbing, never touching
						float side = (float)Math.Sin((Timer + NPC.whoAmI * 37) * 0.025f) * 170f;
						Vector2 want = target.Center + new Vector2(side, -190f + (float)Math.Sin(Timer * 0.08f) * 16f);
						Vector2 v = (want - NPC.Center) * 0.04f;
						if (v.Length() > 6f)
							v = Vector2.Normalize(v) * 6f;
						NPC.velocity = Vector2.Lerp(NPC.velocity, v, 0.08f);
						NPC.direction = NPC.velocity.X >= 0f ? 1 : -1;
						if (Timer >= Wheel && Main.netMode != NetmodeID.MultiplayerClient
							&& Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1))
						{
							State = Aiming;
							Timer = 0f;
							Aim = (target.Center - NPC.Center).ToRotation();
							NPC.netUpdate = true;
						}
					}
					break;

				case Aiming:
					NPC.velocity *= 0.88f;
					NPC.direction = Math.Cos(Aim) >= 0 ? 1 : -1;
					if (Timer == 1f)
						SoundEngine.PlaySound(SoundID.Item32 with { Pitch = 0.6f, Volume = 0.7f }, NPC.Center);
					if (Timer >= Tell)
					{
						State = Diving;
						Timer = 0f;
						NPC.velocity = Aim.ToRotationVector2() * DiveSpeed;
						NPC.netUpdate = true;
					}
					break;

				case Diving:
					NPC.velocity = Aim.ToRotationVector2() * DiveSpeed;
					NPC.direction = NPC.velocity.X >= 0f ? 1 : -1;
					if (Timer >= Dive || NPC.collideX || NPC.collideY)
					{
						State = Climbing;
						Timer = 0f;
						NPC.netUpdate = true;
					}
					break;

				case Climbing:
					NPC.velocity = Vector2.Lerp(NPC.velocity, new Vector2(NPC.direction * 3f, -5f), 0.1f);
					if (Timer >= Climb)
					{
						State = Wheeling;
						Timer = 0f;
						NPC.netUpdate = true;
					}
					break;
			}
			NPC.spriteDirection = NPC.direction;
			NPC.rotation = State == Diving ? NPC.velocity.ToRotation() + (NPC.direction < 0 ? MathHelper.Pi : 0f) : NPC.velocity.X * 0.04f;
		}

		/// <summary>Harmless except on the dive - the dive is the only thing it ever does to you.</summary>
		public override bool CanHitPlayer(Player target, ref int cooldownSlot) => State == Diving;

		public override void FindFrame(int frameHeight)
		{
			if (State == Diving)
			{
				NPC.frame.Y = 3 * frameHeight;
				return;
			}
			NPC.frameCounter += State == Aiming ? 2.0 : 1.0;
			if (NPC.frameCounter >= 6.0)
			{
				NPC.frameCounter = 0.0;
				NPC.frame.Y = (NPC.frame.Y + frameHeight) % (frameHeight * 3);
			}
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			if (State == Aiming)
			{
				float f = Timer / Tell;
				Vector2 dir = Aim.ToRotationVector2();
				RiftDraw.Line(NPC.Center, NPC.Center + dir * DiveSpeed * Dive, new Color(255, 236, 200) * (0.2f + 0.55f * f), 1.5f + 1.5f * f);
			}
			Texture2D tex = TextureAssets.Npc[Type].Value;
			int fh = tex.Height / Main.npcFrameCount[Type];
			Rectangle frame = new(0, NPC.frame.Y, tex.Width, fh);
			SpriteEffects fx = NPC.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
			spriteBatch.Draw(tex, NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY), frame, drawColor,
				NPC.rotation, frame.Size() / 2f, 1f, fx, 0f);
			return false;
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 12 : 3); i++)
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Cloud, hit.HitDirection, -1f, 100, default, 0.9f);
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ItemID.Feather, 2, 1, 2));
			npcLoot.Add(ItemDropRule.Common(ItemID.Seashell, 8));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Accessories.Trees.GullFeather>(), 25));
		}
	}
}
