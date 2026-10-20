using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Particles;

namespace Terrapex.Content.NPCs.Bosses
{
	/// <summary>
	/// One of the four slabs orbiting the Dormant Eye. Deliberately simpler than
	/// <see cref="KeeperPlate"/>: it only orbits. The teaching goal of this fight is
	/// "shells come off, then the eye is open" — a plate that also launches itself
	/// would be a second lesson on top of the first.
	/// </summary>
	public class EyePlate : ModNPC
	{
		private ref float ParentIndex => ref NPC.ai[0];
		private ref float Angle => ref NPC.ai[1];
		private ref float Radius => ref NPC.ai[2];

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 4;
			NPCID.Sets.CantTakeLunchMoney[Type] = true;
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
		}

		public override void SetDefaults()
		{
			NPC.width = 22;
			NPC.height = 22;
			NPC.damage = 20;
			NPC.defense = 6;
			NPC.lifeMax = 250;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath43;
			NPC.knockBackResist = 0f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.aiStyle = -1;
			NPC.value = 0f;
			NPC.dontCountMe = true;
			NPC.npcSlots = 0f;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.UIInfoProvider = new CommonEnemyUICollectionInfoProvider(
				ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[Type], quickUnlock: true);
			bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.EyePlate.Bestiary"));
		}

		public override bool CheckActive() => false;

		public bool BelongsTo(NPC parent) => (int)ParentIndex == parent.whoAmI;

		public override void AI()
		{
			int p = (int)ParentIndex;
			if (p < 0 || p >= Main.maxNPCs || !Main.npc[p].active
				|| Main.npc[p].type != ModContent.NPCType<DormantEye>())
			{
				NPC.life = 0;
				NPC.HitEffect();
				NPC.active = false;
				return;
			}

			NPC parent = Main.npc[p];
			Angle = MathHelper.WrapAngle(Angle + 0.020f);
			NPC.Center = parent.Center + Angle.ToRotationVector2() * Radius;
			NPC.rotation = Angle + MathHelper.PiOver2;

			if (Main.rand.NextBool(14))
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height,
					DustID.Stone, 0f, 0f, 120, default, 0.9f);
				d.noGravity = true;
				d.velocity *= 0.2f;
			}
		}

		public override void FindFrame(int frameHeight)
		{
			NPC.frameCounter += 0.12;
			NPC.frame.Y = (int)(NPC.frameCounter % Main.npcFrameCount[Type]) * frameHeight;
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			int count = NPC.life <= 0 ? 14 : 4;
			for (int i = 0; i < count; i++)
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height,
					DustID.Stone, hit.HitDirection, -1f, 90, default, 1.1f);
				d.velocity *= 1.4f;
			}

			// matte on purpose: a slab of T0 stone breaks into stone, never into light
			if (NPC.life > 0)
			{
				HarvestFX.Rubble(NPC.Center, Dense, 2, 2.5f, 0.8f, 1.5f);
				return;
			}
			// the shatter: a few big slabs, a spray of chips, and a grey ring of grit
			Vector2 c = NPC.Center;
			Vector2 outward = NPC.rotation.ToRotationVector2().RotatedBy(-MathHelper.PiOver2);
			for (int i = 0; i < 4; i++)
				ParticleSystem.Spawn(new Chip(c + Main.rand.NextVector2Circular(6f, 6f),
					outward * Main.rand.NextFloat(1.5f, 3.5f) + Main.rand.NextVector2Circular(2f, 2f),
					Dense[Main.rand.Next(Dense.Length)], Main.rand.NextFloat(2.2f, 3.2f), 70));
			HarvestFX.Rubble(c, Dense, 14, 5f, 1.1f, 2f);
			ParticleSystem.Spawn(new Shockwave(c, new Color(0x84, 0x76, 0x96) * 0.6f, 0.1f, 0.8f, 20) { Glow = false });
			HarvestFX.MatteCrack(c, outward.RotatedBy(1.2f), 16f, new Color(0x1f, 0x19, 0x30), 24, 2f);
			HarvestFX.MatteCrack(c, outward.RotatedBy(-1.2f), 16f, new Color(0x1f, 0x19, 0x30), 24, 2f);
		}

		private static readonly Color[] Dense =
			{ new(0xba, 0xac, 0xc4), new(0x84, 0x76, 0x96), new(0x58, 0x4a, 0x6e), new(0x3b, 0x30, 0x50) };
	}
}
