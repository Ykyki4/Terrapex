using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles.Rime;

namespace Terrapex.Content.NPCs.Snow
{
	/// <summary>
	/// A small cold spirit. It barely hurts to touch; its threat is where it has been. It weaves
	/// back and forth across the player, from one side to the other, and leaves motes of frost
	/// hanging in the air behind it that sting and chill (<see cref="FrostMote"/>). Fragile -
	/// the answer is to shoot it before the net of its trail closes round you.
	///
	/// FrostWisp.png is four 28x32 frames of its flame flickering.
	/// </summary>
	public class FrostWisp : ModNPC
	{
		public const int MoteEvery = 9;
		public const int MoteDamage = 10;
		public const int SwapTicks = 150;

		private ref float Timer => ref NPC.ai[0];
		private ref float Side => ref NPC.ai[1];

		public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 4;

		public override void SetDefaults()
		{
			NPC.width = 22;
			NPC.height = 26;
			NPC.damage = 14;
			NPC.defense = 4;
			NPC.lifeMax = 45;
			NPC.HitSound = SoundID.NPCHit5;
			NPC.DeathSound = SoundID.NPCDeath7;
			NPC.value = Item.buyPrice(silver: 3);
			NPC.knockBackResist = 0.6f;
			NPC.aiStyle = -1;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
			=> bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Snow,
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.UndergroundSnow,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.FrostWisp.Bestiary")
			});

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.PlayerInTown || !spawnInfo.Player.ZoneSnow)
				return 0f;
			if (spawnInfo.Player.ZoneOverworldHeight)
				return Main.dayTime ? 0f : 0.08f;
			return 0.08f;
		}

		public override void AI()
		{
			NPC.TargetClosest();
			Player target = Main.player[NPC.target];
			if (Side == 0f)
				Side = Main.rand.NextBool() ? 1f : -1f;
			Timer++;
			if (Timer % SwapTicks == 0f)
			{
				Side = -Side;
				NPC.netUpdate = true;
			}

			// it crosses the player from one side to the other, low, so its trail cuts their ground
			Vector2 want = target.Center + new Vector2(Side * 170f, -30f + (float)Math.Sin(Timer * 0.05f) * 40f);
			Vector2 accel = (want - NPC.Center).SafeNormalize(Vector2.Zero) * 0.14f;
			NPC.velocity += accel;
			if (NPC.velocity.Length() > 3.4f)
				NPC.velocity = Vector2.Normalize(NPC.velocity) * 3.4f;
			NPC.spriteDirection = NPC.direction = NPC.velocity.X >= 0f ? 1 : -1;

			if (Main.netMode != NetmodeID.MultiplayerClient && Timer % MoteEvery == 0f && NPC.Distance(target.Center) < 900f)
				Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, NPC.velocity * 0.15f,
					ModContent.ProjectileType<FrostMote>(), NPC.GetAttackDamage_ForProjectiles(MoteDamage, MoteDamage), 0f, Main.myPlayer);

			Lighting.AddLight(NPC.Center, 0.2f, 0.4f, 0.55f);
			if (Main.rand.NextBool(4))
				Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(8f, 10f), DustID.Ice, -NPC.velocity * 0.2f,
					120, default, 0.8f).noGravity = true;
		}

		public override void FindFrame(int frameHeight)
		{
			if (++NPC.frameCounter >= 6)
			{
				NPC.frameCounter = 0;
				NPC.frame.Y = (NPC.frame.Y + frameHeight) % (frameHeight * Main.npcFrameCount[Type]);
			}
		}

		public override Color? GetAlpha(Color drawColor) => Color.Lerp(drawColor, Color.White, 0.6f);

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 18 : 4); i++)
				Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Ice, hit.HitDirection, -1f, 100, default, 1f).noGravity = true;
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
			=> npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RimeCrystal>(), 1, 1, 2));
	}
}
