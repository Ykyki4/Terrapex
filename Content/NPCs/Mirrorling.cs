using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Accessories;

namespace Terrapex.Content.NPCs
{
	/// <summary>
	/// Plan mob #6. It does not chase — it replays what the player did thirty ticks ago,
	/// mirrored, so it closes exactly as fast as the player runs and only a change of
	/// direction shakes it. Running away from a Mirrorling is running into it.
	/// </summary>
	public class Mirrorling : ModNPC
	{
		/// <summary>Half a second. Long enough to read as a delay, short enough to still feel aimed.</summary>
		public const int Delay = 30;

		private readonly Vector2[] trail = new Vector2[Delay];
		private int trailHead;
		private bool trailFilled;

		public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 6;

		public override void SetDefaults()
		{
			NPC.width = 28;
			NPC.height = 36;
			NPC.damage = 34;
			NPC.defense = 12;
			NPC.lifeMax = 180;
			NPC.HitSound = SoundID.NPCHit3;
			NPC.DeathSound = SoundID.Shatter;
			NPC.value = Item.buyPrice(silver: 18);
			NPC.knockBackResist = 0.25f;
			NPC.noGravity = true;
			NPC.noTileCollide = false;
			NPC.aiStyle = -1;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Caverns,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.Mirrorling.Bestiary")
			});
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.PlayerInTown || spawnInfo.Water || !NPC.downedBoss3)
				return 0f;
			return spawnInfo.Player.ZoneRockLayerHeight ? 0.05f : 0f;
		}

		public override void AI()
		{
			if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
				NPC.TargetClosest();

			Player target = Main.player[NPC.target];

			// the ring buffer is the whole mob: write this tick, read the one from Delay ago
			trail[trailHead] = target.velocity;
			trailHead = (trailHead + 1) % Delay;
			if (trailHead == 0)
				trailFilled = true;

			Vector2 echoed = trailFilled ? trail[trailHead] : Vector2.Zero;
			echoed.X = -echoed.X;   // mirrored, so the player's retreat becomes its approach

			// a floor of its own so it does not simply stall when the player stands still
			Vector2 drift = NPC.DirectionTo(target.Center) * 1.6f;
			NPC.velocity = Vector2.Lerp(NPC.velocity, echoed + drift, 0.12f);
			if (NPC.velocity.Length() > 9f)
				NPC.velocity = Vector2.Normalize(NPC.velocity) * 9f;

			NPC.spriteDirection = NPC.direction = NPC.velocity.X >= 0f ? 1 : -1;
			NPC.rotation = NPC.velocity.X * 0.02f;

			Lighting.AddLight(NPC.Center, 0.22f, 0.10f, 0.30f);
			if (Main.rand.NextBool(14))
			{
				Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(12f, 16f),
					DustID.Glass, -NPC.velocity * 0.15f, 150, default, 0.8f);
				d.noGravity = true;
			}
		}

		public override void FindFrame(int frameHeight)
		{
			if (++NPC.frameCounter >= 7.0)
			{
				NPC.frameCounter = 0;
				NPC.frame.Y = (NPC.frame.Y + frameHeight) % (frameHeight * Main.npcFrameCount[Type]);
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(
				ModContent.ItemType<MirrorCharm>(), 34));
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			int count = NPC.life > 0 ? 4 : 22;
			for (int i = 0; i < count; i++)
			{
				Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Glass,
					Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 120, default, 1.1f);
			}
		}
	}
}
