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
	/// One of the eight lids grown over the First Keeper. It is a shell plate the way
	/// <see cref="KeeperPlate"/> is, and it orbits and can be thrown the same way — but what it
	/// does while it lives is the opposite of armour.
	///
	/// A lid does not reduce damage. It <em>narrows the gaze</em>: with all eight closed the
	/// First Keeper can only see a thin wedge of the arena, so it is hard to catch its regard
	/// and hard to be hurt by it. Cut a lid away and the eye opens wider — the window in which
	/// the boss takes double damage grows, and so does the slice of the arena in which it can
	/// aim at you. The player prices that trade themselves, in both directions, which is the
	/// whole reason this tier's shell is destructible.
	/// </summary>
	public class KeeperLid : ModNPC
	{
		public const int ModeOrbit = 0;
		public const int ModeWindup = 1;
		public const int ModeSpear = 2;
		public const int ModeReturn = 3;

		private ref float ParentIndex => ref NPC.ai[0];
		private ref float Angle => ref NPC.ai[1];
		private ref float Radius => ref NPC.ai[2];
		private ref float Mode => ref NPC.ai[3];
		private ref float ModeTimer => ref NPC.localAI[0];

		/// <summary>Which orbit seat this lid holds. Kept so a regrown lid fills the hole.</summary>
		public int Slot;

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 4;
			NPCID.Sets.CantTakeLunchMoney[Type] = true;
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
			NPCID.Sets.NPCBestiaryDrawModifiers value = new() { Hide = true };
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
		}

		public override void SetDefaults()
		{
			NPC.width = 40;
			NPC.height = 40;
			NPC.damage = 120;
			NPC.defense = 60;
			NPC.lifeMax = 7000;
			NPC.HitSound = SoundID.NPCHit42;
			NPC.DeathSound = SoundID.NPCDeath43;
			NPC.knockBackResist = 0f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.aiStyle = -1;
			NPC.value = 0f;
			NPC.dontCountMe = true;
			NPC.npcSlots = 0f;
			NPC.netAlways = true;
		}

		/// <summary>
		/// Held well down, like the Weaver's anchors. Eight lids are a price the player pays to
		/// open the eye, and at the default scaling that price would be higher than the window
		/// it buys — so nobody would ever cut one and the mechanic would not exist.
		/// </summary>
		public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
			=> NPC.lifeMax = (int)(NPC.lifeMax * 0.6f * balance);

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
			=> bestiaryEntry.UIInfoProvider = new CommonEnemyUICollectionInfoProvider(
				ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[Type], quickUnlock: true);

		public override bool CheckActive() => false;

		public bool BelongsTo(NPC parent) => (int)ParentIndex == parent.whoAmI;

		public override void AI()
		{
			int p = (int)ParentIndex;
			if (p < 0 || p >= Main.maxNPCs || !Main.npc[p].active
				|| Main.npc[p].type != ModContent.NPCType<FirstKeeper>())
			{
				NPC.life = 0;
				NPC.HitEffect();
				NPC.active = false;
				return;
			}

			NPC parent = Main.npc[p];
			Player target = Main.player[parent.target];
			float phase = parent.ai[3];
			float spin = phase >= 3f ? 0.030f : (phase >= 2f ? 0.021f : 0.013f);

			// the seat keeps turning whatever this lid is doing, so a thrown one comes home
			// into the gap it left instead of to a stale angle
			Angle = MathHelper.WrapAngle(Angle + spin);
			Vector2 slot = parent.Center + Angle.ToRotationVector2() * Radius;
			ModeTimer++;

			switch ((int)Mode)
			{
				case ModeWindup:
				{
					Vector2 spot = parent.Center + Angle.ToRotationVector2() * (Radius * 0.68f)
						+ Main.rand.NextVector2Circular(4f, 4f);
					NPC.Center = Vector2.Lerp(NPC.Center, spot, 0.2f);
					NPC.velocity = Vector2.Zero;
					NPC.rotation = Angle + MathHelper.PiOver2;

					if (Main.rand.NextBool(2))
					{
						Dust d = Dust.NewDustPerfect(NPC.Center, DustID.WhiteTorch,
							Main.rand.NextVector2Circular(2f, 2f), 110, default, 1.1f);
						d.noGravity = true;
					}

					if (ModeTimer > 48f)
					{
						NPC.velocity = target.active && !target.dead
							? (target.Center - NPC.Center).SafeNormalize(Vector2.UnitY) * 19f
							: Angle.ToRotationVector2() * 19f;
						SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
						Mode = ModeSpear;
						ModeTimer = 0f;
						NPC.netUpdate = true;
					}
					break;
				}

				case ModeSpear:
				{
					NPC.velocity *= 0.995f;
					NPC.rotation = NPC.velocity.ToRotation() + MathHelper.PiOver2;
					if (Main.rand.NextBool(2))
					{
						Dust d = Dust.NewDustPerfect(NPC.Center, DustID.WhiteTorch,
							NPC.velocity * -0.1f, 130, default, 1.2f);
						d.noGravity = true;
					}
					if (ModeTimer > 66f)
					{
						Mode = ModeReturn;
						ModeTimer = 0f;
						NPC.netUpdate = true;
					}
					break;
				}

				case ModeReturn:
				{
					float dist = NPC.Distance(slot);
					float speed = MathHelper.Clamp(dist * 0.11f, 6f, 26f);
					Vector2 want = (slot - NPC.Center).SafeNormalize(Vector2.Zero) * speed;
					NPC.velocity = Vector2.Lerp(NPC.velocity, want, 0.18f);
					NPC.rotation = NPC.velocity.ToRotation() + MathHelper.PiOver2;

					if (dist < 22f || ModeTimer > 200f)
					{
						NPC.Center = slot;
						NPC.rotation = Angle + MathHelper.PiOver2;
						Mode = ModeOrbit;
						ModeTimer = 0f;
						NPC.velocity = Vector2.Zero;
						NPC.netUpdate = true;
					}
					break;
				}

				default:
				{
					NPC.Center = Vector2.Lerp(NPC.Center, slot, 0.35f);
					NPC.velocity = Vector2.Zero;
					NPC.rotation = Angle + MathHelper.PiOver2;
					break;
				}
			}

			Lighting.AddLight(NPC.Center, 0.30f, 0.30f, 0.34f);
		}

		public void BeginSpear()
		{
			if ((int)Mode != ModeOrbit)
				return;
			Mode = ModeWindup;
			ModeTimer = 0f;
			NPC.netUpdate = true;
		}

		public bool Orbiting => (int)Mode == ModeOrbit;

		public override void FindFrame(int frameHeight)
		{
			float ratio = NPC.life / (float)NPC.lifeMax;
			int frame = ratio > 0.75f ? 0 : ratio > 0.5f ? 1 : ratio > 0.25f ? 2 : 3;
			NPC.frame.Y = frame * frameHeight;
		}

		public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			// a hairline of light down the seam it is holding shut
			float beat = 0.8f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.4f + Slot) * 0.2f;
			RiftDraw.Bloom(NPC.Center, RiftDraw.Glow(230, 235, 250, 0.22f * beat), 0.5f);
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 34 : 5); i++)
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height,
					Main.rand.NextBool(3) ? DustID.WhiteTorch : DustID.Stone,
					hit.HitDirection * 2f, -1f, 100, default, NPC.life <= 0 ? 1.7f : 1.1f);
				d.noGravity = true;
			}
		}

		public override void OnKill()
		{
			SoundEngine.PlaySound(SoundID.Item27 with { Pitch = -0.4f }, NPC.Center);
			int p = (int)ParentIndex;
			if (p >= 0 && p < Main.maxNPCs && Main.npc[p].active
				&& Main.npc[p].ModNPC is FirstKeeper keeper)
				keeper.LidCut();
		}
	}
}
