using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Common.Systems;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.NPCs
{
	/// <summary>
	/// Plan mob #5. A slab of stone that grew a front. Hitting it from the side it faces is
	/// nearly pointless; hitting it from behind is not.
	///
	/// This is the first time the mod asks the question its last boss is built out of - where
	/// is this thing looking, and where does that leave me. The Spall answers it with one
	/// number and no telegraph to read, so by the time a player meets the First Keeper's cone
	/// six tiers later the verb is already familiar.
	///
	/// Which face is armoured has to be *visible*, so the guarded side carries a drawn plate
	/// and a lit rim rather than being something the player infers from damage numbers.
	/// </summary>
	public class Spall : ModNPC
	{
		/// <summary>What a blow to the armoured face is worth.</summary>
		public const float Guarded = 0.15f;

		private ref float Turn => ref NPC.ai[0];

		public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 4;

		public override void SetDefaults()
		{
			NPC.width = 30;
			NPC.height = 28;
			NPC.damage = 22;
			NPC.defense = 10;
			NPC.lifeMax = 90;
			NPC.HitSound = SoundID.NPCHit42;
			NPC.DeathSound = SoundID.NPCDeath43;
			NPC.value = Item.buyPrice(silver: 4);
			NPC.knockBackResist = 0.25f;
			NPC.noGravity = false;
			NPC.aiStyle = NPCAIStyleID.Fighter;
			AIType = NPCID.Zombie;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Underground,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.Spall.Bestiary")
			});
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.PlayerInTown || spawnInfo.Water || !DownedBossSystem.downedDormantEye)
				return 0f;
			return spawnInfo.Player.ZoneRockLayerHeight ? 0.07f : 0f;
		}

		/// <summary>True when the blow is landing on the plated side.</summary>
		private bool FromFront(Vector2 from)
			=> (from.X - NPC.Center.X) * NPC.direction > 0f;

		private void Block(Vector2 from, ref NPC.HitModifiers modifiers)
		{
			if (!FromFront(from))
				return;
			modifiers.FinalDamage *= Guarded;
			modifiers.HideCombatText();
		}

		public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
			=> Block(player.Center, ref modifiers);

		public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
			=> Block(projectile.Center, ref modifiers);

		public override void AI()
		{
			Turn++;
			// it walks with the Fighter AI, so direction is already the way it is facing; all
			// this does is keep the sprite honest about it
			NPC.spriteDirection = NPC.direction;
			Lighting.AddLight(NPC.Center, 0.10f, 0.05f, 0.14f);
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
			// the plate, drawn on the guarded side. Without it the mechanic is a damage number
			// the player has to reverse-engineer.
			Vector2 at = NPC.Center + new Vector2(NPC.direction * 13f, 0f);
			float beat = 0.75f + (float)Math.Sin(Turn * 0.06f) * 0.25f;
			RiftDraw.Line(at + new Vector2(0f, -12f), at + new Vector2(0f, 12f),
				RiftDraw.Glow(210, 190, 235, 0.30f * beat), 4f);
			RiftDraw.Bloom(at, RiftDraw.Glow(200, 170, 235, 0.22f * beat), 0.34f);
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 22 : 4); i++)
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Stone,
					hit.HitDirection, -1f, 100, default, 1.1f);
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Riftglass>(), 2, 1, 3));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RiftDust>(), 2, 1, 3));
		}
	}
}
