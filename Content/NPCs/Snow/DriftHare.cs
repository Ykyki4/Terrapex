using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Content.NPCs.Snow
{
	/// <summary>
	/// A white hare that travels under the snow. Burrowed, it is a mound running at you - it can
	/// neither be hurt nor hurt you. Under your feet the mound stops and shakes for
	/// <see cref="Tell"/> ticks, then the hare bursts out in a leap, hops at you for a while in
	/// the open where it can be hit, and dives back under.
	///
	/// DriftHare.png is four 32x32 frames, facing left: 0 sitting, 1 falling, 2 leaping, 3 the mound.
	/// </summary>
	public class DriftHare : ModNPC
	{
		public const int Tell = 36;
		private const int Burrowed = 0, Shaking = 1, Out = 2;
		private const int OutTicks = 240;

		private ref float State => ref NPC.ai[0];
		private ref float Timer => ref NPC.ai[1];
		private int contactDamage = -1;

		public override void SetStaticDefaults() => Main.npcFrameCount[Type] = 4;

		public override void SetDefaults()
		{
			NPC.width = 24;
			NPC.height = 22;
			NPC.damage = 22;
			NPC.defense = 6;
			NPC.lifeMax = 70;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.value = Item.buyPrice(silver: 3);
			NPC.knockBackResist = 0.5f;
			NPC.aiStyle = -1;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
			=> bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Snow,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.DriftHare.Bestiary")
			});

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.PlayerInTown || spawnInfo.Water || !spawnInfo.Player.ZoneSnow)
				return 0f;
			return spawnInfo.Player.ZoneOverworldHeight ? 0.12f : 0f;
		}

		public override void SendExtraAI(BinaryWriter writer) => writer.Write(contactDamage);

		public override void ReceiveExtraAI(BinaryReader reader) => contactDamage = reader.ReadInt32();

		public override void AI()
		{
			// the scaled contact damage, kept once so it can be switched off under the snow
			if (contactDamage < 0)
				contactDamage = NPC.damage;

			NPC.TargetClosest();
			Player target = Main.player[NPC.target];
			Timer++;
			bool onGround = NPC.velocity.Y == 0f;
			float dx = target.Center.X - NPC.Center.X;

			switch ((int)State)
			{
				case Burrowed:
					NPC.dontTakeDamage = true;
					NPC.damage = 0;
					NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, Math.Sign(dx) * 3.2f, 0.08f);
					if (onGround && NPC.collideX)
						NPC.velocity.Y = -5f;
					if (onGround && Main.rand.NextBool(2))
						Dust.NewDustPerfect(NPC.Bottom + new Vector2(Main.rand.NextFloat(-10f, 10f), -4f), DustID.Snow,
							new Vector2(-NPC.velocity.X * 0.3f, -Main.rand.NextFloat(0.5f, 2f)), 100, default, 1f);
					if (Timer > 50f && onGround && ((Math.Abs(dx) < 18f && Math.Abs(target.Bottom.Y - NPC.Bottom.Y) < 80f) || Timer > 600f))
						Switch(Shaking);
					break;

				case Shaking:
					NPC.dontTakeDamage = true;
					NPC.damage = 0;
					NPC.velocity.X *= 0.8f;
					if (Main.rand.NextBool(2))
						Dust.NewDustPerfect(NPC.Bottom + new Vector2(Main.rand.NextFloat(-12f, 12f), -6f), DustID.Snow,
							new Vector2(Main.rand.NextFloat(-1f, 1f), -Main.rand.NextFloat(2f, 4f)), 80, default, 1.2f);
					if (Timer >= Tell)
					{
						Switch(Out);
						NPC.velocity = new Vector2(Math.Sign(dx) * 1.5f, -9.5f);
						SoundEngine.PlaySound(SoundID.Item51 with { Pitch = 0.4f }, NPC.Center);
						for (int i = 0; i < 16; i++)
							Dust.NewDustPerfect(NPC.Bottom, DustID.Snow, new Vector2(Main.rand.NextFloat(-3f, 3f), -Main.rand.NextFloat(1f, 5f)),
								80, default, 1.3f);
					}
					break;

				default:
					NPC.dontTakeDamage = false;
					NPC.damage = contactDamage;
					if (onGround)
					{
						NPC.velocity.X *= 0.85f;
						if ((int)Timer % 40 == 0)
						{
							NPC.velocity = new Vector2(Math.Sign(dx) * 3.6f, -5.5f);
							NPC.netUpdate = true;
						}
						if (Timer > OutTicks)
						{
							Switch(Burrowed);
							for (int i = 0; i < 12; i++)
								Dust.NewDustPerfect(NPC.Bottom, DustID.Snow, new Vector2(Main.rand.NextFloat(-2f, 2f), -Main.rand.NextFloat(1f, 3f)),
									90, default, 1.1f);
						}
					}
					break;
			}

			if (Math.Abs(NPC.velocity.X) > 0.2f)
				NPC.direction = NPC.spriteDirection = NPC.velocity.X > 0f ? 1 : -1;
		}

		private void Switch(int state)
		{
			State = state;
			Timer = 0f;
			NPC.netUpdate = true;
		}

		public override void FindFrame(int frameHeight)
		{
			int f;
			if (State != Out)
				f = 3;
			else if (NPC.velocity.Y < 0f)
				f = 2;
			else if (NPC.velocity.Y > 0f)
				f = 1;
			else
				f = 0;
			// the mound shivers in place during the tell
			NPC.gfxOffY = State == Shaking ? (float)Math.Sin(Timer * 1.6f) * 1.5f : 0f;
			NPC.frame.Y = f * frameHeight;
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 16 : 4); i++)
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Snow, hit.HitDirection, -1f, 100, default, 1f);
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
			=> npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<FrostPelt>(), 1, 1, 2));
	}
}
