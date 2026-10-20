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

namespace Terrapex.Content.NPCs.Desert
{
	/// <summary>
	/// A dust devil with a dead scarab spinning in its eye. It does not chase so much as wander
	/// your way, and then it breathes in:
	///
	/// - <see cref="Gather"/> ticks of tell, a ring of grit closing from far out down to the
	///   exact reach of the pull, so the player sees where the pull will end before it starts;
	/// - <see cref="Pull"/> ticks of suction inside that ring, toward the eye, while it spins
	///   faster and barely moves;
	/// - then it drifts again, and the pull cannot come back for <see cref="Drift"/> ticks.
	///
	/// The pull is a velocity nudge, capped, so it can always be walked out of - it is a tax on
	/// standing near it, not a trap. SpawnChance puts it in the surface and underground desert.
	/// DustDevil.png is four 36x52 frames of the funnel turning, stacked.
	/// </summary>
	public class DustDevil : ModNPC
	{
		public const int Drift = 150;
		public const int Gather = 40;
		public const int Pull = 120;
		public const float Reach = 210f;
		public const float PullForce = 0.32f;
		public const float PullCap = 5f;

		private const int Drifting = 0, Gathering = 1, Pulling = 2;

		private ref float State => ref NPC.ai[0];
		private ref float Timer => ref NPC.ai[1];

		private static readonly Color Grit = new(236, 196, 120);

		public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 4;

		public override void SetDefaults()
		{
			NPC.width = 30;
			NPC.height = 46;
			NPC.damage = 16;
			NPC.defense = 4;
			NPC.lifeMax = 70;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath6;
			NPC.value = Item.buyPrice(silver: 2);
			NPC.knockBackResist = 0.5f;
			NPC.noGravity = true;
			NPC.aiStyle = -1;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
			=> bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Desert,
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.UndergroundDesert,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.DustDevil.Bestiary")
			});

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.PlayerInTown || spawnInfo.Water)
				return 0f;
			Player p = spawnInfo.Player;
			if (p.ZoneDesert && p.ZoneOverworldHeight)
				return 0.09f;
			if (p.ZoneUndergroundDesert)
				return 0.07f;
			return 0f;
		}

		public override void AI()
		{
			NPC.TargetClosest();
			Player target = Main.player[NPC.target];
			Timer++;

			switch ((int)State)
			{
				case Drifting:
				{
					// it wanders toward you on a wobble, and keeps near the floor
					Vector2 to = target.Center - NPC.Center;
					float wobble = MathF.Sin(Timer * 0.07f + NPC.whoAmI) * 1.2f;
					Vector2 want = new(Math.Sign(to.X) * 1.6f + wobble, Math.Clamp(to.Y * 0.02f, -1.2f, 1.2f));
					NPC.velocity = Vector2.Lerp(NPC.velocity, want, 0.05f);
					if (Timer >= Drift && to.Length() < Reach * 1.4f && target.active && !target.dead)
						Enter(Gathering);
					break;
				}

				case Gathering:
				{
					NPC.velocity *= 0.9f;
					if (Timer == 1f)
						SoundEngine.PlaySound(SoundID.Item34 with { Pitch = -0.6f, Volume = 0.7f }, NPC.Center);
					if (Timer >= Gather)
						Enter(Pulling);
					break;
				}

				default:
				{
					NPC.velocity *= 0.92f;
					PullLocal();
					if (Main.rand.NextBool(2))
					{
						Vector2 at = NPC.Center + Main.rand.NextVector2CircularEdge(Reach, Reach);
						Dust d = Dust.NewDustPerfect(at, DustID.Sand, (NPC.Center - at) * 0.04f, 80, default, 1f);
						d.noGravity = true;
					}
					if (Timer >= Pull)
						Enter(Drifting);
					break;
				}
			}

			NPC.rotation = NPC.velocity.X * 0.05f;
			if (Main.rand.NextBool(4))
				Dust.NewDustDirect(NPC.BottomLeft - new Vector2(0f, 8f), NPC.width, 8, DustID.Sand, 0f, -1f, 100, default, 0.8f).noGravity = true;
		}

		private void Enter(int state)
		{
			State = state;
			Timer = 0f;
			NPC.netUpdate = true;
		}

		/// <summary>
		/// The suction. Every client nudges only its own player, so it is the same rule everywhere
		/// and nothing needs syncing; the cap keeps it something you can walk against.
		/// </summary>
		private void PullLocal()
		{
			if (Main.netMode == NetmodeID.Server)
				return;
			Player p = Main.LocalPlayer;
			if (!p.active || p.dead)
				return;
			Vector2 to = NPC.Center - p.Center;
			float d = to.Length();
			if (d > Reach || d < 8f)
				return;
			Vector2 dir = to / d;
			// only while the player is moving toward the eye slower than the cap
			if (Vector2.Dot(p.velocity, dir) < PullCap)
				p.velocity += dir * PullForce;
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 24 : 5); i++)
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Sand, hit.HitDirection * 2f, -1f, 80, default, 1.1f);
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Sunsand>(), 1, 1, 3));
			npcLoot.Add(ItemDropRule.Common(ItemID.AntlionMandible, 4));
		}

		public override void FindFrame(int frameHeight)
		{
			// it turns faster the harder it breathes
			NPC.frameCounter += State == Pulling ? 2 : State == Gathering ? 1.5 : 1;
			if (NPC.frameCounter >= 5.0)
			{
				NPC.frameCounter = 0;
				NPC.frame.Y = (NPC.frame.Y + frameHeight) % (frameHeight * Main.npcFrameCount[Type]);
			}
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			// the ring: closing in during the tell, then standing at the pull's exact reach
			if (State != Drifting)
			{
				float f = State == Gathering ? Timer / Gather : 1f;
				float radius = State == Gathering ? MathHelper.Lerp(Reach * 1.8f, Reach, f) : Reach;
				float spin = Main.GlobalTimeWrappedHourly * (State == Pulling ? -3f : -1f);
				Texture2D px = TextureAssets.MagicPixel.Value;
				int dashes = 48;
				for (int i = 0; i < dashes; i++)
				{
					if (i % 3 == 2)
						continue;
					Vector2 at = NPC.Center + Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / dashes + spin) * radius - screenPos;
					spriteBatch.Draw(px, at, new Rectangle(0, 0, 1, 1), Grit * (0.2f + 0.45f * f), 0f, new Vector2(0.5f), 3f,
						SpriteEffects.None, 0f);
				}
			}

			Texture2D tex = TextureAssets.Npc[Type].Value;
			spriteBatch.Draw(tex, NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY), NPC.frame, drawColor, NPC.rotation,
				NPC.frame.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
			return false;
		}
	}
}
