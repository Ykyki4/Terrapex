using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.NPCs
{
	/// <summary>
	/// Plan mob #11. It pairs off with another Weaverling and strings a thread between them.
	///
	/// This is the tier's mechanic pointed back at the player: T4's weapons stitch enemies
	/// together, and these stitch themselves. A lone Weaverling is nearly harmless, so the
	/// fight is about which one you kill first rather than about killing both.
	/// </summary>
	public class Weaverling : ModNPC
	{
		private const float PairRange = 420f;
		private const float ThreadWidth = 10f;
		private const int ThreadDamage = 42;
		private const int Segments = 8;

		/// <summary>Partner index, stored one-based so that a fresh 0 means "none".</summary>
		private ref float Partner => ref NPC.ai[0];
		private ref float Drift => ref NPC.ai[1];

		public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 4;

		public override void SetDefaults()
		{
			NPC.width = 28;
			NPC.height = 26;
			NPC.damage = 44;
			NPC.defense = 22;
			NPC.lifeMax = 420;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.value = Item.buyPrice(silver: 40);
			NPC.knockBackResist = 0.4f;
			NPC.noGravity = true;
			NPC.noTileCollide = false;
			NPC.aiStyle = -1;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Caverns,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.Weaverling.Bestiary")
			});
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.PlayerInTown || spawnInfo.Water || !Terraria.NPC.downedMechBossAny)
				return 0f;
			return spawnInfo.Player.ZoneRockLayerHeight ? 0.06f : 0f;
		}

		public override void AI()
		{
			if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
				NPC.TargetClosest();

			Player target = Main.player[NPC.target];
			Drift++;

			// it keeps its distance: the thread is the weapon, not the body
			Vector2 want = target.Center - NPC.Center;
			float dist = want.Length();
			if (dist > 4f)
			{
				want /= dist;
				float pull = dist > 260f ? 1f : (dist < 150f ? -0.7f : 0f);
				NPC.velocity += want * pull * 0.06f;
			}
			NPC.velocity += new Vector2(0f, (float)Math.Sin(Drift * 0.05f) * 0.02f);
			NPC.velocity *= 0.985f;
			if (NPC.velocity.Length() > 4.2f)
				NPC.velocity = Vector2.Normalize(NPC.velocity) * 4.2f;

			NPC.spriteDirection = NPC.direction = NPC.velocity.X > 0f ? 1 : -1;
			NPC.rotation = NPC.velocity.X * 0.03f;

			Pair();
			Sting();
			Lighting.AddLight(NPC.Center, 0.10f, 0.26f, 0.24f);
		}

		/// <summary>Finds an unpaired neighbour and stitches to it, both ways.</summary>
		private void Pair()
		{
			NPC held = Held();
			if (held != null)
				return;

			Partner = 0f;
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC other = Main.npc[i];
				if (!other.active || other.type != Type || other.whoAmI == NPC.whoAmI)
					continue;
				if (Vector2.Distance(other.Center, NPC.Center) > PairRange)
					continue;

				// only take one that is free, or two threads end up sharing an end
				float theirs = other.ai[0];
				if (theirs != 0f && Main.npc[(int)theirs - 1].active)
					continue;

				Partner = other.whoAmI + 1;
				other.ai[0] = NPC.whoAmI + 1;
				NPC.netUpdate = true;
				other.netUpdate = true;
				return;
			}
		}

		private NPC Held()
		{
			int index = (int)Partner - 1;
			if (index < 0 || index >= Main.maxNPCs)
				return null;
			NPC other = Main.npc[index];
			if (!other.active || other.type != Type)
				return null;
			return Vector2.Distance(other.Center, NPC.Center) <= PairRange * 1.6f ? other : null;
		}

		/// <summary>The thread itself hurts. Only the lower index checks, or it hits twice.</summary>
		private void Sting()
		{
			NPC other = Held();
			if (other == null || NPC.whoAmI > other.whoAmI)
				return;

			Player p = Main.LocalPlayer;
			if (!p.active || p.dead || p.immune)
				return;

			float point = 0f;
			if (!Collision.CheckAABBvLineCollision(p.Hitbox.TopLeft(), p.Hitbox.Size(),
					NPC.Center, other.Center, ThreadWidth, ref point))
				return;

			p.Hurt(PlayerDeathReason.ByNPC(NPC.whoAmI), ThreadDamage,
				p.Center.X < NPC.Center.X ? -1 : 1);
		}

		public override void FindFrame(int frameHeight)
		{
			if (++NPC.frameCounter >= 8.0)
			{
				NPC.frameCounter = 0.0;
				NPC.frame.Y = (NPC.frame.Y + frameHeight) % (frameHeight * Main.npcFrameCount[Type]);
			}
		}

		public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			NPC other = Held();
			if (other == null || NPC.whoAmI > other.whoAmI)
				return;

			Vector2 a = NPC.Center - Main.screenPosition;
			Vector2 b = other.Center - Main.screenPosition;
			Texture2D px = TextureAssets.MagicPixel.Value;
			Rectangle src = new Rectangle(0, 0, 1, 1);

			// it has to look dangerous, because it is: a thin decorative line here would be a
			// hitbox the player never saw coming
			float beat = 1f + 0.3f * (float)Math.Sin(Main.GameUpdateCount * 0.16f);
			float sag = MathHelper.Clamp(Vector2.Distance(a, b) * 0.10f, 3f, 30f);
			Vector2 prev = a;
			for (int i = 1; i <= Segments; i++)
			{
				float t = i / (float)Segments;
				Vector2 p = Vector2.Lerp(a, b, t);
				p.Y += (float)Math.Sin(t * MathHelper.Pi) * sag;
				Vector2 seg = p - prev;
				float rot = seg.ToRotation();
				float run = seg.Length() + 1f;

				spriteBatch.Draw(px, prev, src, new Color(18, 124, 120, 0) * 0.6f, rot,
					new Vector2(0f, 0.5f), new Vector2(run, 8f * beat), SpriteEffects.None, 0f);
				spriteBatch.Draw(px, prev, src, new Color(120, 240, 220, 0) * 0.9f, rot,
					new Vector2(0f, 0.5f), new Vector2(run, 2.5f), SpriteEffects.None, 0f);
				prev = p;
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
		{
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<VoidThread>(), 1, 1, 3));
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 14 : 4); i++)
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Vortex,
					hit.HitDirection, -1f, 110, default, 1f);
				d.noGravity = true;
			}
		}
	}
}
