using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Items.Placeable;

namespace Terrapex.Content.NPCs
{
	/// <summary>
	/// Plan mob #2. It hangs from the ceiling and falls on whatever walks under it.
	///
	/// An ambush that simply drops would be the one thing this mod refuses to ship - damage the
	/// player had no way to see coming. So it shakes for <see cref="Tell"/> ticks first and
	/// sheds grit while it does, which is the same contract every boss attack here honours,
	/// written small enough to be the second enemy anyone meets.
	///
	/// It is a one-shot: once it has fallen it breaks on the floor. Nothing that ambushes should
	/// also get to chase.
	/// </summary>
	public class Hangstone : ModNPC
	{
		private const int Hanging = 0;
		private const int Shaking = 1;
		private const int Falling = 2;

		public const int Tell = 34;
		private const float Watch = 130f;

		private ref float State => ref NPC.ai[0];
		private ref float Timer => ref NPC.ai[1];

		public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 4;

		public override void SetDefaults()
		{
			NPC.width = 26;
			NPC.height = 26;
			NPC.damage = 20;
			NPC.defense = 6;
			NPC.lifeMax = 55;
			NPC.HitSound = SoundID.NPCHit42;
			NPC.DeathSound = SoundID.NPCDeath43;
			NPC.value = Item.buyPrice(copper: 80);
			NPC.knockBackResist = 0f;
			NPC.noGravity = true;
			NPC.noTileCollide = false;
			NPC.aiStyle = -1;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Underground,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.Hangstone.Bestiary")
			});
		}

		/// <summary>Only under a solid ceiling - there is nothing for it to hang from otherwise.</summary>
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.PlayerInTown || spawnInfo.Water)
				return 0f;
			if (!spawnInfo.Player.ZoneDirtLayerHeight && !spawnInfo.Player.ZoneRockLayerHeight)
				return 0f;

			for (int up = 1; up <= 6; up++)
			{
				Tile t = Framing.GetTileSafely(spawnInfo.SpawnTileX, spawnInfo.SpawnTileY - up);
				if (t.HasTile && Main.tileSolid[t.TileType])
					return 0.07f;
			}
			return 0f;
		}

		public override void AI()
		{
			NPC.TargetClosest(false);
			Player target = Main.player[NPC.target];

			switch ((int)State)
			{
				case Hanging:
				{
					NPC.velocity = Vector2.Zero;
					bool below = target.active && !target.dead
						&& System.Math.Abs(target.Center.X - NPC.Center.X) < 26f
						&& target.Center.Y > NPC.Center.Y
						&& target.Center.Y - NPC.Center.Y < Watch;
					if (below)
					{
						State = Shaking;
						Timer = 0f;
						NPC.netUpdate = true;
					}
					break;
				}

				case Shaking:
				{
					// the tell: it rattles in place and grit comes off it
					Timer++;
					NPC.position.X += Main.rand.NextFloat(-0.7f, 0.7f);
					if (Main.rand.NextBool(2))
					{
						Dust d = Dust.NewDustDirect(NPC.Bottom - new Vector2(NPC.width * 0.5f, 0f),
							NPC.width, 4, DustID.Stone, 0f, 1.5f, 60, default, 0.9f);
						d.noGravity = false;
					}
					if (Timer == 1f)
						SoundEngine.PlaySound(SoundID.Item50 with { Volume = 0.5f, Pitch = -0.4f }, NPC.Center);
					if (Timer >= Tell)
					{
						State = Falling;
						NPC.noGravity = false;
						NPC.velocity.Y = 1.5f;
						NPC.netUpdate = true;
					}
					break;
				}

				default:
				{
					NPC.velocity.Y += 0.42f;
					if (NPC.velocity.Y > 13f)
						NPC.velocity.Y = 13f;
					NPC.rotation += NPC.velocity.Y * 0.01f;

					// it breaks where it lands: an ambusher that then walks around would be a
					// different enemy wearing the same sprite
					if (NPC.collideY || NPC.velocity.Y == 0f)
					{
						NPC.life = 0;
						NPC.HitEffect();
						NPC.active = false;
						SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.6f }, NPC.Center);
					}
					break;
				}
			}
		}

		public override void FindFrame(int frameHeight)
		{
			int frame = (int)State == Hanging ? 0
				: (int)State == Shaking ? (Timer % 8 < 4 ? 1 : 2) : 3;
			NPC.frame.Y = frame * frameHeight;
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 20 : 4); i++)
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Stone,
					hit.HitDirection, -1f, 100, default, 1.1f);
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrackedStone>(), 1, 2, 5));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RiftDust>(), 3, 1, 2));
		}
	}
}
