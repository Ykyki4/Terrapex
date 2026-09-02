using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

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
		}
	}
}
