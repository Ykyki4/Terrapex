using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.NPCs
{
	/// <summary>
	/// Plan mob #15. A splinter of the First Keeper that got loose, and the only place the
	/// tier's material comes from outside the boss.
	///
	/// It runs the boss's mechanic in miniature, which is the reason it exists at all: it has a
	/// pupil that turns toward you at a capped rate, it can only shoot along the line it is
	/// looking down, and it takes double damage while it has you and very little while it does
	/// not. A player who meets three of these before ever summoning the boss has already been
	/// taught the whole fight — that the answer is neither to stand still nor to run, but to
	/// decide which of the two you want right now.
	/// </summary>
	public class KeeperEcho : ModNPC
	{
		private const float GazeHalf = 0.42f;
		private const float TurnRate = 0.020f;
		private const float Reach = 620f;

		public const float Seen = 2.0f;
		public const float Unseen = 0.5f;

		private ref float Pupil => ref NPC.ai[0];
		private ref float Cooldown => ref NPC.ai[1];

		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[Type] = 4;
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
		}

		public override void SetDefaults()
		{
			NPC.width = 44;
			NPC.height = 44;
			NPC.damage = 130;
			NPC.defense = 56;
			NPC.lifeMax = 5200;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.knockBackResist = 0.15f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.aiStyle = -1;
			NPC.value = Item.buyPrice(gold: 3);
			NPC.npcSlots = 2f;
			NPC.rarity = 3;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
		{
			bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
				new FlavorTextBestiaryInfoElement("Mods.Terrapex.NPCs.KeeperEcho.Bestiary")
			});
		}

		/// <summary>Rare, and only once the Lord is down. It is a T6 mob in every sense.</summary>
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (!NPC.downedMoonlord || spawnInfo.PlayerInTown || spawnInfo.Invasion)
				return 0f;
			return 0.012f;
		}

		public bool Regards(Player player)
		{
			if (player == null || !player.active || player.dead)
				return false;
			if (Vector2.DistanceSquared(player.Center, NPC.Center) > Reach * Reach)
				return false;
			float to = (player.Center - NPC.Center).ToRotation();
			return Math.Abs(MathHelper.WrapAngle(to - Pupil)) <= GazeHalf;
		}

		private void ApplyRegard(Player attacker, ref NPC.HitModifiers modifiers)
			=> modifiers.FinalDamage *= Regards(attacker) ? Seen : Unseen;

		public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
			=> ApplyRegard(player, ref modifiers);

		public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
			=> ApplyRegard(Main.player[projectile.owner], ref modifiers);

		public override void AI()
		{
			NPC.TargetClosest();
			Player target = Main.player[NPC.target];

			// it drifts rather than chases: a mob whose whole point is where it is looking must
			// not also be the thing that closes distance, or the pupil never matters
			Vector2 want = target.Center - NPC.Center;
			float dist = want.Length();
			Vector2 stand = target.Center - want.SafeNormalize(Vector2.UnitX) * 300f;
			NPC.velocity = Vector2.Lerp(NPC.velocity, (stand - NPC.Center) * 0.03f, 0.06f);
			if (NPC.velocity.Length() > 4.6f)
				NPC.velocity = Vector2.Normalize(NPC.velocity) * 4.6f;

			float wantAngle = want.ToRotation();
			Pupil = MathHelper.WrapAngle(Pupil
				+ MathHelper.Clamp(MathHelper.WrapAngle(wantAngle - Pupil), -TurnRate, TurnRate));

			if (Cooldown > 0f)
				Cooldown--;

			// it fires only down the line it already holds, so the shot is never a surprise —
			// you watched the pupil arrive
			if (Cooldown <= 0f && Regards(target) && dist < Reach
				&& Main.netMode != NetmodeID.MultiplayerClient)
			{
				Cooldown = 150f;
				GazeRay.Spawn(NPC.GetSource_FromAI(), NPC.Center, Pupil, 0f,
					Main.expertMode ? 55 : 110, GazeRay.Telegraph + 40, NPC.whoAmI);
			}

			Lighting.AddLight(NPC.Center, 0.45f, 0.45f, 0.5f);
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
			bool seen = Regards(Main.LocalPlayer);
			Vector2 look = Pupil.ToRotationVector2();

			for (int k = -1; k <= 1; k += 2)
				RiftDraw.Line(NPC.Center, NPC.Center + (Pupil + k * GazeHalf).ToRotationVector2() * Reach,
					RiftDraw.Glow(225, 232, 250, seen ? 0.16f : 0.07f), 1.6f);

			RiftDraw.Bloom(NPC.Center + look * 13f,
				RiftDraw.Glow(255, 255, 255, seen ? 0.8f : 0.35f), seen ? 0.6f : 0.4f);
			RiftDraw.Ring(NPC.Center, RiftDraw.Glow(60, 64, 84, 0.4f), 0.6f,
				Main.GlobalTimeWrappedHourly * -0.6f);
		}

		public override void HitEffect(NPC.HitInfo hit)
		{
			for (int i = 0; i < (NPC.life <= 0 ? 28 : 4); i++)
			{
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.WhiteTorch,
					hit.HitDirection, -1f, 100, default, 1.2f);
				d.noGravity = true;
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot)
			=> npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Primordium>(), 1, 1, 2));
	}
}
