using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.NPCs
{
	/// <summary>
	/// Plan mob #13. Eight shards that move and die as one thing.
	///
	/// Written as a single NPC drawn as a cloud rather than as eight linked NPCs: a real flock
	/// of eight would each need their own pathing and would shred the spawn budget, and the
	/// design brief only ever asked that it *act* as a unit. The shards thin out as its health
	/// falls, so the swarm visibly comes apart.
	/// </summary>
	public class ShardSwarm : ModNPC
	{
		private const int Shards = 8;

		public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 4;

		public override void SetDefaults()
		{
			NPC.width = 52;
			NPC.height = 52;
			NPC.damage = 92;
			NPC.defense = 30;
			NPC.lifeMax = 1900;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.value = Item.buyPrice(silver: 90);
			NPC.knockBackResist = 0.25f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.aiStyle = -1;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Caverns,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.ShardSwarm.Bestiary")
			});
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.PlayerInTown || spawnInfo.Water || !Terraria.NPC.downedPlantBoss)
				return 0f;
			return spawnInfo.Player.ZoneRockLayerHeight ? 0.04f : 0f;
		}

		public override void AI()
		{
			if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
				NPC.TargetClosest();
			Player target = Main.player[NPC.target];

			NPC.ai[0]++;
			Vector2 want = target.Center - NPC.Center;
			float dist = want.Length();
			if (dist > 8f)
				NPC.velocity += want / dist * 0.14f;
			// it drifts rather than steering, so a swarm reads as a swarm and not as a missile
			NPC.velocity += new Vector2((float)Math.Sin(NPC.ai[0] * 0.05f),
				(float)Math.Cos(NPC.ai[0] * 0.043f)) * 0.09f;
			NPC.velocity *= 0.982f;
			if (NPC.velocity.Length() > 7.5f)
				NPC.velocity = Vector2.Normalize(NPC.velocity) * 7.5f;

			Lighting.AddLight(NPC.Center, 0.30f, 0.16f, 0.42f);
		}

		public override void FindFrame(int frameHeight)
		{
			if (++NPC.frameCounter >= 6.0)
			{
				NPC.frameCounter = 0.0;
				NPC.frame.Y = (NPC.frame.Y + frameHeight) % (frameHeight * Main.npcFrameCount[Type]);
			}
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			Texture2D tex = Terraria.GameContent.TextureAssets.Npc[Type].Value;
			Rectangle frame = NPC.frame;
			Vector2 origin = frame.Size() * 0.5f;
			// how many are left to draw is the health bar
			int left = Math.Max(2, (int)Math.Ceiling(Shards * NPC.life / (float)NPC.lifeMax));

			for (int i = 0; i < left; i++)
			{
				float a = NPC.ai[0] * 0.045f + i * MathHelper.TwoPi / Shards;
				float r = 16f + 7f * (float)Math.Sin(NPC.ai[0] * 0.03f + i);
				Vector2 at = NPC.Center + a.ToRotationVector2() * r - Main.screenPosition;
				spriteBatch.Draw(tex, at, frame, drawColor, a * 1.7f, origin, NPC.scale * 0.85f,
					SpriteEffects.None, 0f);
			}
			return false;
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
			=> npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<VoidThread>(), 1, 2, 4));

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 26 : 6); i++)
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.PurpleTorch,
					hit.HitDirection, -1f, 110, default, 1.1f);
				d.noGravity = true;
			}
		}
	}
}
