using System;
using System.IO;
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

namespace Terrapex.Content.NPCs.Ocean
{
	/// <summary>
	/// A beach crab gone heavy with barnacles - the Old Shellback's mechanic in miniature. The
	/// first hit it takes slams it shut: for 70 ticks it is a rock that takes a fifth of every
	/// blow, and for the last 20 of them it rattles. Then it bursts out in a lunge. Either kill
	/// it in the first blow, or let it shut, wait, and step aside from the lunge - the same choice
	/// the boss's Hull asks, at the price of a beach mob.
	///
	/// BarnacleCrab.png: five 40x28 frames, stacked. 0-3 walking, 4 shut.
	/// </summary>
	public class BarnacleCrab : ModNPC
	{
		public const int ShutTicks = 70, RattleTicks = 20, Cooldown = 120;
		public const float ShutTaken = 0.2f;

		private int shut;
		private int cooldown;

		public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 5;

		public override void SetDefaults()
		{
			NPC.width = 34;
			NPC.height = 22;
			NPC.damage = 16;
			NPC.defense = 8;
			NPC.lifeMax = 60;
			NPC.HitSound = SoundID.NPCHit38;
			NPC.DeathSound = SoundID.NPCDeath41;
			NPC.value = Item.buyPrice(silver: 2);
			NPC.knockBackResist = 0.4f;
			NPC.aiStyle = NPCAIStyleID.Fighter;
			AIType = NPCID.Crab;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
			=> bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Ocean,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.BarnacleCrab.Bestiary")
			});

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.PlayerInTown || spawnInfo.Water || !spawnInfo.Player.ZoneBeach)
				return 0f;
			return 0.22f;
		}

		public override void SendExtraAI(BinaryWriter writer)
		{
			writer.Write(shut);
			writer.Write(cooldown);
		}

		public override void ReceiveExtraAI(BinaryReader reader)
		{
			shut = reader.ReadInt32();
			cooldown = reader.ReadInt32();
		}

		// HitEffect runs on every machine for every hit, so the shell shuts the same everywhere
		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 16 : 3); i++)
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Sand, hit.HitDirection, -1f, 100, default, 1f);
			if (NPC.life > 0 && shut <= 0 && cooldown <= 0)
			{
				shut = ShutTicks;
				NPC.netUpdate = true;
				SoundEngine.PlaySound(SoundID.NPCHit42 with { Pitch = 0.4f, Volume = 0.6f }, NPC.Center);
			}
		}

		public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
		{
			if (shut > 0)
				modifiers.FinalDamage *= ShutTaken;
		}

		public override bool PreAI()
		{
			if (cooldown > 0)
				cooldown--;
			if (shut <= 0)
				return true;

			// shut: a rock on the sand, then a rattle, then the lunge
			shut--;
			NPC.velocity.X *= 0.8f;
			if (shut == 0)
			{
				NPC.TargetClosest();
				NPC.velocity = new Vector2(NPC.direction * 7f, -4.5f);
				cooldown = Cooldown;
				NPC.netUpdate = true;
				SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.3f }, NPC.Center);
			}
			return false;
		}

		public override void AI()
		{
			NPC.spriteDirection = NPC.direction;
		}

		public override bool CanHitPlayer(Player target, ref int cooldownSlot) => shut <= 0;

		public override void FindFrame(int frameHeight)
		{
			if (shut > 0)
			{
				NPC.frame.Y = 4 * frameHeight;
				return;
			}
			if (Math.Abs(NPC.velocity.X) < 0.1f)
			{
				NPC.frameCounter = 0.0;
				NPC.frame.Y = 0;
				return;
			}
			NPC.frameCounter += Math.Abs(NPC.velocity.X) * 0.4;
			if (NPC.frameCounter < 3.0)
				return;
			NPC.frameCounter = 0.0;
			NPC.frame.Y = (NPC.frame.Y + frameHeight) % (frameHeight * 4);
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			Texture2D tex = TextureAssets.Npc[Type].Value;
			int fh = tex.Height / Main.npcFrameCount[Type];
			Rectangle frame = new(0, NPC.frame.Y, tex.Width, fh);
			// the rattle is the tell: the shut shell shakes for the last twenty ticks
			Vector2 shake = shut > 0 && shut <= RattleTicks ? new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), 0f) : Vector2.Zero;
			SpriteEffects fx = NPC.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
			spriteBatch.Draw(tex, NPC.Bottom - screenPos + new Vector2(0f, NPC.gfxOffY + 2f) + shake, frame, drawColor,
				NPC.rotation, new Vector2(tex.Width / 2f, fh), 1f, fx, 0f);
			return false;
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
			=> npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BarnacleCluster>(), 1, 1, 3));
	}
}
