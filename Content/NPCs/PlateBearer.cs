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
	/// Plan mob #9. It carries one plate off the Keeper's shell and hides behind it.
	///
	/// The plate is not a health pool with a second name - it is a budget. Every blow while it
	/// is up is cut to a fifth and spends what it absorbed out of the plate; when the budget is
	/// gone the plate comes off, drops as a shard, and the bearer is an ordinary enemy. So the
	/// question the player answers is "do I spend four hits getting through, or do I walk past
	/// it", which is the same shape as the Keeper's shell one tier up.
	/// </summary>
	public class PlateBearer : ModNPC
	{
		private const int PlateBudget = 420;
		public const float Behind = 0.2f;

		/// <summary>What is left of the plate. Synced, because the draw depends on it.</summary>
		private ref float Plate => ref NPC.ai[0];
		private ref float Spin => ref NPC.ai[1];

		public bool Shielded => Plate > 0f;

		public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 4;

		public override void SetDefaults()
		{
			NPC.width = 32;
			NPC.height = 34;
			NPC.damage = 42;
			NPC.defense = 16;
			NPC.lifeMax = 220;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.value = Item.buyPrice(silver: 30);
			NPC.knockBackResist = 0.2f;
			NPC.aiStyle = NPCAIStyleID.Fighter;
			AIType = NPCID.Zombie;
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
			=> Plate = PlateBudget;

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Underground,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.PlateBearer.Bestiary")
			});
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.PlayerInTown || spawnInfo.Water || !Main.hardMode)
				return 0f;
			return spawnInfo.Player.ZoneRockLayerHeight ? 0.06f : 0f;
		}

		public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
		{
			if (Shielded)
				modifiers.FinalDamage *= Behind;
		}

		/// <summary>Spends the plate by what it actually absorbed, not by what got through.</summary>
		public void Chip(int dealt)
		{
			if (!Shielded)
				return;
			// what the plate ATE, not what got through: the hit was cut to Behind, so the
			// blow that landed was dealt / Behind before the plate took its share
			Plate -= dealt / Behind;
			NPC.netUpdate = true;
			if (Plate > 0f)
				return;

			Plate = 0f;
			SoundEngine.PlaySound(SoundID.NPCDeath43, NPC.Center);
			for (int i = 0; i < 26; i++)
			{
				Dust d = Dust.NewDustPerfect(NPC.Center + new Vector2(NPC.direction * 16f, 0f),
					DustID.PurpleTorch, Main.rand.NextVector2Circular(4f, 4f), 100, default, 1.3f);
				d.noGravity = true;
			}
			if (Main.netMode != NetmodeID.MultiplayerClient)
				Item.NewItem(NPC.GetSource_Death(), NPC.Hitbox, ModContent.ItemType<PlateShard>(), 1);
		}

		public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
			=> Chip(damageDone);

		public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
			=> Chip(damageDone);

		/// <summary>Called by the shepherd: a fresh plate, handed over.</summary>
		public void Rearm()
		{
			if (Shielded)
				return;
			Plate = PlateBudget;
			NPC.netUpdate = true;
			SoundEngine.PlaySound(SoundID.Item37, NPC.Center);
		}

		public override void AI()
		{
			Spin++;
			NPC.spriteDirection = NPC.direction;
		}

		/// <summary>
		/// Own framing rather than <c>AnimationType</c>. Borrowing the Zombie's animation means
		/// borrowing its sixteen frames, and the game then reads frame.Y past the end of a
		/// four-frame sheet. aiStyle and AIType still do the walking; only the frames are ours.
		/// </summary>
		public override void FindFrame(int frameHeight)
		{
			if (System.Math.Abs(NPC.velocity.X) < 0.1f)
			{
				NPC.frameCounter = 0.0;
				NPC.frame.Y = 0;
				return;
			}
			NPC.frameCounter += System.Math.Abs(NPC.velocity.X) * 0.32;
			if (NPC.frameCounter < 3.0)
				return;
			NPC.frameCounter = 0.0;
			NPC.frame.Y = (NPC.frame.Y + frameHeight) % (frameHeight * Main.npcFrameCount[Type]);
		}

		public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			if (!Shielded)
				return;
			// the plate reads as intact, cracked or nearly gone, so "how many more hits" is a
			// thing you can see rather than a thing you count
			float left = MathHelper.Clamp(Plate / PlateBudget, 0f, 1f);
			Vector2 at = NPC.Center + new Vector2(NPC.direction * 17f, 0f);
			float beat = 0.8f + (float)Math.Sin(Spin * 0.07f) * 0.2f;
			RiftDraw.Line(at + new Vector2(0f, -15f), at + new Vector2(0f, 15f),
				RiftDraw.Glow(230, 120, 235, (0.18f + left * 0.35f) * beat), 5f + left * 3f);
			RiftDraw.Bloom(at, RiftDraw.Glow(255, 154, 217, (0.15f + left * 0.3f) * beat), 0.45f);
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 24 : 4); i++)
				Dust.NewDust(NPC.position, NPC.width, NPC.height,
					Shielded ? DustID.PurpleTorch : DustID.Stone,
					hit.HitDirection, -1f, 100, default, 1.1f);
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
			=> npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<PlateShard>(), 1, 1, 2));
	}
}
