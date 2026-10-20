using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Accessories;

namespace Terrapex.Content.NPCs.Wilds
{
	/// <summary>
	/// A cavern mole with a miner's lantern strapped to its head. It swims through the rock toward
	/// the player, untouchable and harmless, its lantern the only thing you see moving behind the
	/// walls; then the ground bursts for 30 ticks where it is about to surface - the tell - and it
	/// leaps out and runs at you for a few seconds before it digs back in.
	///
	/// LanternMole.png: frames 0-1 the run, 2 the leap.
	/// </summary>
	public class LanternMole : ModNPC
	{
		private const int TellTicks = 30;
		private const int SurfaceTicks = 200;

		private const float Dig = 0f, Tell = 1f, Out = 2f;

		private ref float State => ref NPC.ai[0];
		private ref float Timer => ref NPC.ai[1];

		public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 3;

		public override void SetDefaults()
		{
			NPC.width = 30;
			NPC.height = 22;
			NPC.damage = 20;
			NPC.defense = 8;
			NPC.lifeMax = 80;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.knockBackResist = 0.5f;
			NPC.value = Item.buyPrice(silver: 2);
			NPC.aiStyle = -1;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
			=> spawnInfo.Player.ZoneRockLayerHeight && !spawnInfo.Player.ZoneDungeon && !spawnInfo.Player.ZoneJungle
				? 0.08f : 0f;

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
			=> bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Caverns,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.LanternMole.Bestiary")
			});

		public override bool? CanBeHitByItem(Player player, Item item) => State == Out ? null : false;

		public override bool? CanBeHitByProjectile(Projectile projectile) => State == Out ? null : false;

		public override bool CanHitPlayer(Player target, ref int cooldownSlot) => State == Out;

		public override void AI()
		{
			NPC.TargetClosest();
			Player target = Main.player[NPC.target];
			Lighting.AddLight(NPC.Center, 0.9f, 0.7f, 0.35f);
			Timer++;

			if (State == Dig)
			{
				// through the rock, toward a spot under the player's feet
				NPC.noGravity = true;
				NPC.noTileCollide = true;
				NPC.alpha = 200;
				Vector2 want = target.Bottom + new Vector2(0f, 40f) - NPC.Center;
				NPC.velocity = Vector2.Lerp(NPC.velocity, want.SafeNormalize(Vector2.Zero) * 4f, 0.08f);
				if (Main.rand.NextBool(4) && Collision.SolidCollision(NPC.position, NPC.width, NPC.height))
					Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Dirt, 0f, 0f, 120, default, 0.8f);
				if (want.Length() < 70f && Timer > 60f)
				{
					State = Tell;
					Timer = 0f;
					NPC.velocity = Vector2.Zero;
					NPC.netUpdate = true;
				}
				return;
			}

			if (State == Tell)
			{
				// the ground breaks where it is about to come out: the tell
				NPC.velocity = Vector2.Zero;
				if (Main.rand.NextBool(2))
					Dust.NewDustDirect(NPC.position + new Vector2(0f, -16f), NPC.width, 16, DustID.Dirt,
						Main.rand.NextFloat(-2f, 2f), -3f, 60, default, 1.2f);
				if (Timer >= TellTicks)
				{
					State = Out;
					Timer = 0f;
					NPC.velocity = new Vector2(0f, -7f);
					NPC.alpha = 0;
					SoundEngine.PlaySound(SoundID.Dig, NPC.Center);
					NPC.netUpdate = true;
				}
				return;
			}

			// out: it runs at you on the ground for a few seconds
			NPC.noGravity = false;
			NPC.noTileCollide = Timer < 12f;
			NPC.direction = NPC.spriteDirection = target.Center.X > NPC.Center.X ? 1 : -1;
			NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.direction * 3.2f, 0.1f);
			if (NPC.collideX && NPC.velocity.Y == 0f)
				NPC.velocity.Y = -6f;
			if (Timer >= SurfaceTicks && NPC.velocity.Y == 0f)
			{
				State = Dig;
				Timer = 0f;
				SoundEngine.PlaySound(SoundID.Dig, NPC.Center);
				NPC.netUpdate = true;
			}
		}

		public override void FindFrame(int frameHeight)
		{
			if (State != Out || NPC.velocity.Y != 0f)
			{
				NPC.frame.Y = 2 * frameHeight;
				return;
			}
			if (++NPC.frameCounter >= 7)
			{
				NPC.frameCounter = 0;
				NPC.frame.Y = NPC.frame.Y == 0 ? frameHeight : 0;
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<MolesLantern>(), 25));
			npcLoot.Add(ItemDropRule.Common(ItemID.Torch, 2, 2, 5));
		}
	}
}
