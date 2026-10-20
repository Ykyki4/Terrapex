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
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles.Scarab;

namespace Terrapex.Content.NPCs.Desert
{
	/// <summary>
	/// A dung beetle the size of a dog, pushing a ball of packed sand in front of it. The Scarab
	/// Sultan's fight in miniature, met before the Sultan is:
	///
	/// - while it has its ball, the ball is a shield - hits from the side it faces take
	///   <see cref="FrontShare"/> of their damage;
	/// - in range on the same level it stops, rears for <see cref="Tell"/> ticks with the ball's
	///   path drawn on the floor (the same <see cref="BallPhysics"/> the Sultan's sun uses), and
	///   sends the ball rolling;
	/// - then it is bare for <see cref="BareTicks"/>: faster, unshielded, and the moment to hit it.
	///   After that it has packed a new ball.
	///
	/// SandRoller.png is five 48x32 frames, stacked: four walking, one rearing. The ball is drawn
	/// from <see cref="SunBall"/>'s third frame.
	/// </summary>
	public class SandRoller : ModNPC
	{
		public const int Tell = 36;
		public const int BareTicks = 300;
		public const float FrontShare = 0.35f;
		public const int BallDamage = 12;
		public const float FlickSpeed = 6.5f;

		private const int Pushing = 0, Rearing = 1, Bare = 2;

		private ref float State => ref NPC.ai[0];
		private ref float Timer => ref NPC.ai[1];
		private ref float Cooldown => ref NPC.ai[2];

		private Vector2 BallAt => NPC.Center + new Vector2(NPC.direction * 34f, -4f);
		private Vector2 FlickVelocity => new(NPC.direction * FlickSpeed, -1.5f);

		public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 5;

		public override void SetDefaults()
		{
			NPC.width = 40;
			NPC.height = 28;
			NPC.damage = 18;
			NPC.defense = 8;
			NPC.lifeMax = 90;
			NPC.HitSound = SoundID.NPCHit31;
			NPC.DeathSound = SoundID.NPCDeath34;
			NPC.value = Item.buyPrice(silver: 2);
			NPC.knockBackResist = 0.3f;
			NPC.aiStyle = -1;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
			=> bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Desert,
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.UndergroundDesert,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.SandRoller.Bestiary")
			});

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.PlayerInTown || spawnInfo.Water)
				return 0f;
			Player p = spawnInfo.Player;
			if (p.ZoneDesert && p.ZoneOverworldHeight)
				return Main.dayTime ? 0.14f : 0.06f;
			if (p.ZoneUndergroundDesert)
				return 0.06f;
			return 0f;
		}

		public override void AI()
		{
			NPC.TargetClosest(State != Rearing);
			Player target = Main.player[NPC.target];
			NPC.spriteDirection = NPC.direction;
			if (Cooldown > 0f)
				Cooldown--;

			switch ((int)State)
			{
				case Pushing:
				{
					Walk(1.3f);
					float dx = target.Center.X - NPC.Center.X;
					bool level = Math.Abs(target.Center.Y - NPC.Center.Y) < 80f;
					bool facing = Math.Sign(dx) == NPC.direction;
					if (Cooldown <= 0f && level && facing && Math.Abs(dx) < 420f && Math.Abs(dx) > 90f
						&& NPC.velocity.Y == 0f && Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1))
					{
						State = Rearing;
						Timer = 0f;
						NPC.netUpdate = true;
					}
					break;
				}

				case Rearing:
				{
					// the tell: it stops, rears and trembles, and the ball's path is on the floor
					Timer++;
					NPC.velocity.X *= 0.8f;
					if (Timer == 1f)
						SoundEngine.PlaySound(SoundID.Item32 with { Pitch = -0.5f }, NPC.Center);
					if (Timer >= Tell)
					{
						if (Main.netMode != NetmodeID.MultiplayerClient)
						{
							Vector2 v = FlickVelocity;
							Projectile.NewProjectile(NPC.GetSource_FromAI(), BallAt, Vector2.Zero, ModContent.ProjectileType<SunBall>(),
								NPC.GetAttackDamage_ForProjectiles(BallDamage, BallDamage), 0f, Main.myPlayer, SunBall.Dung, v.X, v.Y);
						}
						SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.4f }, NPC.Center);
						State = Bare;
						Timer = 0f;
						NPC.netUpdate = true;
					}
					break;
				}

				default:
				{
					// bare: quicker, and nothing between it and you
					Timer++;
					Walk(2.4f);
					if (Timer >= BareTicks)
					{
						State = Pushing;
						Timer = 0f;
						Cooldown = 90f;
						NPC.netUpdate = true;
						for (int i = 0; i < 14; i++)
							Dust.NewDustDirect(BallAt - new Vector2(16f), 32, 32, DustID.Sand, 0f, -1f, 60, default, 1.1f);
					}
					break;
				}
			}
		}

		private void Walk(float speed)
		{
			float want = NPC.direction * speed;
			NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, want, 0.08f);
			Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
			if (NPC.collideX && NPC.velocity.Y == 0f)
				NPC.velocity.Y = -6.5f;
		}

		private bool HasBall => State != Bare;

		private void Shield(Vector2 from, ref NPC.HitModifiers modifiers)
		{
			if (HasBall && Math.Sign(from.X - NPC.Center.X) == NPC.direction)
			{
				modifiers.FinalDamage *= FrontShare;
				modifiers.Knockback *= 0.2f;
			}
		}

		public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
			=> Shield(player.Center, ref modifiers);

		public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
			=> Shield(projectile.Center - projectile.velocity * 4f, ref modifiers);

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 20 : 4); i++)
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GoldCoin, hit.HitDirection, -1f, 100, default, 1f);
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<GildedChitin>(), 1, 1, 3));
			npcLoot.Add(ItemDropRule.Common(ItemID.SandBlock, 2, 3, 8));
		}

		public override void FindFrame(int frameHeight)
		{
			if (State == Rearing)
			{
				NPC.frame.Y = 4 * frameHeight;
				return;
			}
			if (NPC.velocity.Y == 0f && Math.Abs(NPC.velocity.X) > 0.2f)
			{
				NPC.frameCounter += Math.Abs(NPC.velocity.X);
				if (NPC.frameCounter >= 8.0)
				{
					NPC.frameCounter = 0;
					NPC.frame.Y = (NPC.frame.Y / frameHeight + 1) % 4 * frameHeight;
				}
			}
			if (NPC.frame.Y >= 4 * frameHeight)
				NPC.frame.Y = 0;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			if (State == Rearing)
			{
				float f = Timer / Tell;
				BallPhysics.DrawPath(BallAt, FlickVelocity, SunBall.Size, SunBall.Size, 120, new Color(240, 200, 120), 0.3f + 0.6f * f);
			}

			Texture2D tex = TextureAssets.Npc[Type].Value;
			SpriteEffects fx = NPC.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
			Vector2 shake = State == Rearing ? new Vector2(Main.rand.NextFloat(-1f, 1f), 0f) : Vector2.Zero;
			Vector2 at = NPC.Bottom - screenPos + new Vector2(0f, NPC.gfxOffY + 2f) + shake;
			spriteBatch.Draw(tex, at, NPC.frame, drawColor, 0f, new Vector2(NPC.frame.Width * 0.5f, NPC.frame.Height), 1f, fx, 0f);

			if (HasBall)
			{
				Texture2D ball = TextureAssets.Projectile[ModContent.ProjectileType<SunBall>()].Value;
				Rectangle frame = ball.Frame(1, 3, 0, SunBall.Dung);
				float spin = NPC.position.X / 17f;
				spriteBatch.Draw(ball, BallAt - screenPos + new Vector2(0f, NPC.gfxOffY) + shake, frame, drawColor, spin,
					frame.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
			}
			return false;
		}
	}
}
