using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.NPCs.Bosses
{
	/// <summary>
	/// One armour plate of the Keeper's shell. Orbits its parent, blocks damage while alive,
	/// and can be launched at the player as a spear.
	/// </summary>
	public class KeeperPlate : ModNPC
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

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 4;
			NPCID.Sets.CantTakeLunchMoney[Type] = true;
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
		}

		public override void SetDefaults()
		{
			NPC.width = 30;
			NPC.height = 30;
			NPC.damage = 45;
			NPC.defense = 26;
			NPC.lifeMax = 900;
			NPC.HitSound = SoundID.NPCHit42;
			NPC.DeathSound = SoundID.NPCDeath43;
			NPC.knockBackResist = 0f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.aiStyle = -1;
			NPC.value = 0f;
			NPC.dontCountMe = true;
			NPC.npcSlots = 0f;
			NPC.HitSound = SoundID.NPCHit4;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.UIInfoProvider = new Terraria.GameContent.Bestiary.CommonEnemyUICollectionInfoProvider(
				ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[Type], quickUnlock: true);
			bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.KeeperPlate.Bestiary"));
		}

		public override bool CheckActive() => false;

		public bool BelongsTo(NPC parent) => (int)ParentIndex == parent.whoAmI;

		public override void AI()
		{
			int p = (int)ParentIndex;
			if (p < 0 || p >= Main.maxNPCs || !Main.npc[p].active
				|| Main.npc[p].type != ModContent.NPCType<KeeperOfTheRift>())
			{
				NPC.life = 0;
				NPC.HitEffect();
				NPC.active = false;
				return;
			}

			NPC parent = Main.npc[p];
			Player target = Main.player[parent.target];
			float phase = parent.ai[3];
			float spin = phase >= 3f ? 0.042f : (phase >= 2f ? 0.028f : 0.016f);

			// The slot keeps turning with the rest of the ring no matter what this plate is
			// doing. Freezing it while the plate was thrown was what made plates come home to
			// a stale angle and leave the formation lopsided.
			Angle = MathHelper.WrapAngle(Angle + spin);
			Vector2 slot = parent.Center + Angle.ToRotationVector2() * Radius;

			ModeTimer++;

			switch ((int)Mode)
			{
				case ModeOrbit:
				{
					NPC.Center = Vector2.Lerp(NPC.Center, slot, 0.35f);
					NPC.velocity = Vector2.Zero;
					NPC.rotation = Angle + MathHelper.PiOver2;
					break;
				}

				case ModeWindup:
				{
					// pulls in toward the core and rattles — the tell before it is thrown
					Vector2 spot = parent.Center + Angle.ToRotationVector2() * (Radius * 0.72f)
						+ Main.rand.NextVector2Circular(3f, 3f);
					NPC.Center = Vector2.Lerp(NPC.Center, spot, 0.22f);
					NPC.velocity = Vector2.Zero;
					NPC.rotation = Angle + MathHelper.PiOver2;

					if (Main.rand.NextBool(2))
					{
						Dust d = Dust.NewDustPerfect(NPC.Center, DustID.PurpleTorch,
							Main.rand.NextVector2Circular(2f, 2f), 100, default, 1.1f);
						d.noGravity = true;
					}

					if (ModeTimer > 45f)
					{
						if (target.active && !target.dead)
							NPC.velocity = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitY) * 16f;
						else
							NPC.velocity = Angle.ToRotationVector2() * 16f;

						SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
						Mode = ModeSpear;
						ModeTimer = 0f;
						NPC.netUpdate = true;
					}
					break;
				}

				case ModeSpear:
				{
					NPC.velocity *= 0.994f;
					NPC.rotation = NPC.velocity.ToRotation() + MathHelper.PiOver2;

					if (Main.rand.NextBool(2))
					{
						Dust d = Dust.NewDustPerfect(NPC.Center, DustID.PinkTorch,
							NPC.velocity * -0.1f, 120, default, 1.2f);
						d.noGravity = true;
					}

					if (ModeTimer > 70f)
					{
						Mode = ModeReturn;
						ModeTimer = 0f;
						NPC.netUpdate = true;
					}
					break;
				}

				default:
				{
					// chases the live slot and eases in, so it slides back into the gap it left
					// instead of overshooting past a fixed point
					float dist = NPC.Distance(slot);
					float speed = MathHelper.Clamp(dist * 0.11f, 5f, 24f);
					Vector2 desired = (slot - NPC.Center).SafeNormalize(Vector2.Zero) * speed;
					NPC.velocity = Vector2.Lerp(NPC.velocity, desired, 0.18f);
					NPC.rotation = NPC.velocity.ToRotation() + MathHelper.PiOver2;

					if (Main.rand.NextBool(3))
					{
						Dust d = Dust.NewDustPerfect(NPC.Center, DustID.PurpleTorch,
							NPC.velocity * -0.08f, 140, default, 0.8f);
						d.noGravity = true;
					}

					if (dist < 20f || ModeTimer > 200f)
					{
						// snap onto the slot so the ring is exactly even again
						NPC.Center = slot;
						NPC.rotation = Angle + MathHelper.PiOver2;
						Mode = ModeOrbit;
						ModeTimer = 0f;
						NPC.velocity = Vector2.Zero;
						NPC.netUpdate = true;
					}
					break;
				}
			}
		}

		public void BeginSpear()
		{
			if ((int)Mode != ModeOrbit)
				return;
			Mode = ModeWindup;
			ModeTimer = 0f;
			NPC.netUpdate = true;
		}

		public override void FindFrame(int frameHeight)
		{
			float ratio = NPC.life / (float)NPC.lifeMax;
			int frame = ratio > 0.75f ? 0 : ratio > 0.5f ? 1 : ratio > 0.25f ? 2 : 3;
			NPC.frame.Y = frame * frameHeight;
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 30 : 5); i++)
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height,
					Main.rand.NextBool(3) ? DustID.PurpleTorch : DustID.Stone,
					hit.HitDirection * 2f, -1f, 100, default, NPC.life <= 0 ? 1.7f : 1.1f);
				d.noGravity = true;
			}
		}

		// The shell material comes off the plates, not the core, so a player who ignores the
		// shell and burns the boss down walks away short of steel.
		public override void ModifyNPCLoot(NPCLoot npcLoot)
			=> npcLoot.Add(ItemDropRule.Common(
				ModContent.ItemType<PlateShard>(), 1, 2, 4));
	}
}
