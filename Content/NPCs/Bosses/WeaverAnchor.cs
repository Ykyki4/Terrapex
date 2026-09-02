using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;

namespace Terrapex.Content.NPCs.Bosses
{
	/// <summary>
	/// One of the Weaver's legs, driven into the arena as an anchor point. Threads are strung
	/// between anchors and out to them from the hub, so an anchor is the corner of the web and
	/// the thing you actually attack.
	///
	/// Anchors are not a wall in front of the boss's health bar — that version of the fight was
	/// six minutes of chores. Each one standing takes a tenth off the damage the Weaver receives,
	/// so cutting one pays immediately and cutting all of them is a real reward; and a death
	/// staggers the boss out of whatever it was doing, which is the opening you fight for.
	/// </summary>
	public class WeaverAnchor : ModNPC
	{
		/// <summary>What one standing anchor takes off the damage the Weaver receives.</summary>
		public const float Shelter = 0.10f;

		private ref float Slot => ref NPC.ai[0];
		private ref float Owner => ref NPC.ai[1];
		private ref float Beat => ref NPC.ai[2];

		/// <summary>Where the anchor is being told to sit, written by the boss each tick.</summary>
		public Vector2 Seat;

		/// <summary>
		/// Raised by the boss on the frames it is about to string silk from this corner. The
		/// anchor lights up before the thread exists, which buys the player a second read on
		/// top of the thread's own telegraph.
		/// </summary>
		public float Tension;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 2;
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			NPCID.Sets.CantTakeLunchMoney[Type] = true;
			NPCID.Sets.NPCBestiaryDrawModifiers value = new() { Hide = true };
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
		}

		public override void SetDefaults()
		{
			NPC.width = 34;
			NPC.height = 34;
			NPC.damage = 60;
			NPC.defense = 26;
			NPC.lifeMax = 1800;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.knockBackResist = 0f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.aiStyle = -1;
			NPC.value = 0f;
			NPC.npcSlots = 0f;
			NPC.dontCountMe = true;
			NPC.netAlways = true;
		}

		/// <summary>
		/// Held down deliberately. Six anchors are a tax the player chooses to pay for the
		/// shelter they remove, so at the default scaling they would have cost more than the
		/// forty percent they are worth and nobody would ever cut one.
		/// </summary>
		public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
			=> NPC.lifeMax = (int)(NPC.lifeMax * 0.55f * balance);

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
			=> bestiaryEntry.UIInfoProvider = new CommonEnemyUICollectionInfoProvider(
				ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[Type], quickUnlock: true);

		public override void AI()
		{
			int owner = (int)Owner;
			if (owner < 0 || owner >= Main.maxNPCs || !Main.npc[owner].active
				|| Main.npc[owner].type != ModContent.NPCType<WeaverOfTheRift>())
			{
				NPC.life = 0;
				NPC.HitEffect();
				NPC.active = false;
				return;
			}

			Beat++;
			Tension = MathHelper.Max(0f, Tension - 0.04f);

			// the boss writes the seat; the anchor only has to get there, and it breathes on its
			// own phase so six of them do not read as one rigid object turning
			float bob = (float)Math.Sin(Beat * 0.05f + Slot * 1.7f) * 9f;
			Vector2 out_ = (Seat - Main.npc[owner].Center).SafeNormalize(Vector2.UnitY);
			NPC.Center = Vector2.Lerp(NPC.Center, Seat + out_ * bob, 0.18f);
			NPC.rotation += 0.03f + Tension * 0.06f;

			Lighting.AddLight(NPC.Center, 0.3f + Tension * 0.4f, 0.6f + Tension * 0.4f, 0.55f);

			if (Tension > 0.5f && Main.rand.NextBool(2))
			{
				Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(16f, 16f),
					DustID.Vortex, Vector2.Zero, 110, default, 1f);
				d.noGravity = true;
				d.velocity = (NPC.Center - d.position) * 0.06f;
			}
		}

		public override void FindFrame(int frameHeight)
		{
			if (++NPC.frameCounter >= 10.0)
			{
				NPC.frameCounter = 0.0;
				NPC.frame.Y = (NPC.frame.Y + frameHeight) % (frameHeight * Main.npcFrameCount[Type]);
			}
		}

		public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			float breathe = 0.85f + (float)Math.Sin(Beat * 0.07f + Slot) * 0.15f;
			RiftDraw.Bloom(NPC.Center, RiftDraw.Glow(110, 250, 230, 0.45f + Tension * 0.5f),
				0.55f * breathe);
			RiftDraw.Ring(NPC.Center, RiftDraw.Glow(150, 255, 240, 0.30f + Tension * 0.6f),
				0.42f * breathe + Tension * 0.2f, NPC.rotation);
		}

		public override bool CheckActive() => false;

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 30 : 5); i++)
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Vortex,
					hit.HitDirection, -1f, 110, default, 1.1f);
				d.noGravity = true;
			}
		}

		public override void OnKill()
		{
			SoundEngine.PlaySound(SoundID.Item27 with { Pitch = 0.5f }, NPC.Center);

			int owner = (int)Owner;
			if (owner >= 0 && owner < Main.maxNPCs && Main.npc[owner].active
				&& Main.npc[owner].ModNPC is WeaverOfTheRift weaver)
				weaver.Stagger();
		}
	}
}
