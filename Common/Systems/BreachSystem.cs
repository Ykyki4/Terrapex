using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terrapex.Content.NPCs;

namespace Terrapex.Common.Systems
{
	/// <summary>
	/// The Breach: the mod's wave event, opened after the Keeper of the Rift. See
	/// <c>EVENT_BREACH.md</c> — the numbers here and the numbers there are meant to match.
	///
	/// It does not come from the edges of the screen the way vanilla invasions do. Mouths open
	/// in the stone around the player and each one is a spawner, so the fight happens where you
	/// were digging rather than where you built. The mouths are also the dial: every one left
	/// standing raises the spawn rate, raises what the event pays, and makes the finale bigger.
	/// Killing them is how you survive; leaving them up is how you earn.
	///
	/// Progress is counted in kills rather than in time, because an event that ends on its own
	/// teaches the player to hide from it.
	/// </summary>
	public class BreachSystem : ModSystem
	{
		public const int MaxMouths = 6;
		public const int MouthInterval = 60 * 20;

		/// <summary>Kill counts that close each wave.</summary>
		public static readonly int[] WaveGoals = { 40, 70, 100 };

		/// <summary>
		/// Indexed by mouths currently standing. Written as tables rather than a formula so the
		/// three anchor points in EVENT_BREACH.md are literally these numbers and cannot drift
		/// away from the document by rounding.
		/// </summary>
		public static readonly float[] SpawnRate = { 0.4f, 0.6f, 0.8f, 1.0f, 1.3f, 1.55f, 1.8f };
		public static readonly float[] EjectaRate = { 0.4f, 0.6f, 0.8f, 1.0f, 1.4f, 1.8f, 2.2f };

		public static bool Active;
		public static int Wave;             // 1-3 while running, 4 once Heave is out
		public static int Kills;
		public static bool downedHeave;

		private static int mouthTimer;

		/// <summary>Where the event was started. Mouths open around it, not around whoever wandered off.</summary>
		public static Vector2 Origin;

		// ------------------------------------------------------------------ the dial

		/// <summary>Mouths standing right now, counted from Main.npc so every client agrees.</summary>
		public static int MouthsOpen()
		{
			int type = ModContent.NPCType<BreachMouth>();
			int n = 0;
			for (int i = 0; i < Main.maxNPCs; i++)
				if (Main.npc[i].active && Main.npc[i].type == type)
					n++;
			return n;
		}

		public static float SpawnMultiplier => SpawnRate[System.Math.Clamp(MouthsOpen(), 0, MaxMouths)];
		public static float EjectaMultiplier => EjectaRate[System.Math.Clamp(MouthsOpen(), 0, MaxMouths)];

		// ------------------------------------------------------------------ lifecycle

		public static bool CanStart(Player player)
			=> !Active && Main.hardMode && DownedBossSystem.downedKeeper
			   && player.ZoneRockLayerHeight && !player.ZoneDungeon;

		/// <summary>
		/// A flare used on a client reaches the server here. The flare cannot simply call
		/// <see cref="Start"/> from the client: nothing on the client may write world state, and
		/// the mod's other summon item (<c>FracturedEye</c>) already asks the server the same way.
		/// The conditions are re-tested against the sender because a packet is not a promise.
		/// </summary>
		public static void RequestStart(int whoAmI)
		{
			if (Main.netMode != NetmodeID.Server || whoAmI < 0 || whoAmI >= Main.maxPlayers)
				return;

			Player sender = Main.player[whoAmI];
			if (!sender.active || !CanStart(sender))
				return;

			Start(sender.Center);
		}

		public static void Start(Vector2 where)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			Active = true;
			Wave = 1;
			Kills = 0;
			mouthTimer = MouthInterval;     // the first mouth opens on the next tick, not in 20 s
			Origin = where;
			Announce("BreachBegin");
			Sync();
		}

		public static void End(bool won)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			Active = false;
			Wave = 0;
			Kills = 0;
			ClearMouths();
			if (!won)
				ClearHeave();
			Announce(won ? "BreachWon" : "BreachFailed");
			Sync();
		}

		/// <summary>Called by a GlobalNPC when anything that came out of a mouth dies.</summary>
		public static void CountKill()
		{
			if (!Active || Main.netMode == NetmodeID.MultiplayerClient || Wave > WaveGoals.Length)
				return;

			Kills++;
			if (Kills < WaveGoals[Wave - 1])
			{
				// the bar only has to be right on the client that draws it, and it is redrawn
				// from this value, so a sync every ten kills is enough and costs nothing
				if (Kills % 10 == 0)
					Sync();
				return;
			}

			Wave++;
			Kills = 0;
			if (Wave > WaveGoals.Length)
				SummonHeave();
			else
				Announce("BreachWave" + Wave);
			Sync();
		}

		public override void PostUpdateWorld()
		{
			if (!Active || Main.netMode == NetmodeID.MultiplayerClient)
				return;

			// no player left in the cavern means the event has nowhere to happen
			if (!AnyoneHome())
			{
				End(won: false);
				return;
			}

			if (Wave > WaveGoals.Length)
				return;                     // Heave is out; mouths stop opening

			if (++mouthTimer < MouthInterval)
				return;

			if (MouthsOpen() >= MaxMouths)
			{
				mouthTimer = MouthInterval;     // full: check again next tick, do not bank time
				return;
			}

			// only a mouth that actually opened costs the player the next twenty seconds. A
			// failed placement used to zero the timer, so one unlucky roll bought the event
			// another twenty seconds of doing nothing at all
			if (OpenMouth())
				mouthTimer = 0;
		}

		private static bool AnyoneHome()
		{
			for (int i = 0; i < Main.maxPlayers; i++)
			{
				Player p = Main.player[i];
				if (p.active && !p.dead && p.ZoneRockLayerHeight
					&& p.Distance(Origin) < 4000f)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Puts one mouth in the stone near a player. Returns false when it could not find
		/// anywhere to put it, which is the caller's cue to try again on the next tick.
		/// </summary>
		private static bool OpenMouth()
		{
			Player host = NearestPlayer();
			if (host == null)
				return false;

			Point at = host.Center.ToTileCoordinates();

			// searched from close to far rather than uniformly across the whole box: a mouth
			// that opens forty tiles away through solid rock is a spawner the player never
			// finds, and the dial only works if you can reach what you are choosing about
			for (int attempt = 0; attempt < 60; attempt++)
			{
				int reach = 8 + attempt;
				int x = at.X + Main.rand.Next(-reach, reach + 1);
				int y = at.Y + Main.rand.Next(-reach / 2 - 4, reach / 2 + 5);
				if (!WorldGen.InWorld(x, y, 30))
					continue;
				if (!WorldGen.SolidTile(x, y))
					continue;
				// a floor with headroom. Testing only three tiles up used to accept a pocket
				// inside solid rock, where the mouth is buried and nothing it spawns gets out
				if (WorldGen.SolidTile(x, y - 1) || WorldGen.SolidTile(x, y - 2)
					|| WorldGen.SolidTile(x, y - 3))
					continue;

				int index = NPC.NewNPC(new Terraria.DataStructures.EntitySource_WorldEvent(),
					x * 16 + 8, y * 16, ModContent.NPCType<BreachMouth>());
				if (index >= Main.maxNPCs)
					return false;

				// NewNPC does not sync itself, and without this the mouths exist on the server
				// and on nobody's screen
				if (Main.netMode == NetmodeID.Server)
					NetMessage.SendData(MessageID.SyncNPC, number: index);
				return true;
			}

			return false;
		}

		/// <summary>
		/// The finale. It is summoned with the dial already priced in: every mouth still standing
		/// when the third wave closes makes the Heave bigger, which is the bill for the extra
		/// Ejecta those mouths have been paying out all event. The event does not end here — it
		/// ends when the Heave dies, in <c>Heave.OnKill</c>.
		/// </summary>
		private static void SummonHeave()
		{
			Player host = NearestPlayer();
			if (host == null)
			{
				End(won: false);
				return;
			}

			int mouths = MouthsOpen();
			Vector2 at = host.Center + new Vector2(host.direction * 460f, -120f);

			int i = NPC.NewNPC(new Terraria.DataStructures.EntitySource_WorldEvent(),
				(int)at.X, (int)at.Y, ModContent.NPCType<Content.NPCs.Bosses.Heave>());
			if (i >= Main.maxNPCs)
			{
				End(won: false);
				return;
			}

			NPC heave = Main.npc[i];
			heave.lifeMax += Content.NPCs.Bosses.Heave.LifePerMouth * mouths;
			heave.life = heave.lifeMax;
			heave.damage += Content.NPCs.Bosses.Heave.DamagePerMouth * mouths;

			Announce("BreachHeave");
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.SyncNPC, number: i);
		}

		private static Player NearestPlayer()
		{
			Player best = null;
			float dist = float.MaxValue;
			for (int i = 0; i < Main.maxPlayers; i++)
			{
				Player p = Main.player[i];
				if (!p.active || p.dead)
					continue;
				float d = p.Distance(Origin);
				if (d < dist)
				{
					dist = d;
					best = p;
				}
			}
			return best;
		}

		private static void ClearMouths()
		{
			int type = ModContent.NPCType<BreachMouth>();
			for (int i = 0; i < Main.maxNPCs; i++)
				if (Main.npc[i].active && Main.npc[i].type == type)
				{
					Main.npc[i].life = 0;
					Main.npc[i].HitEffect();
					Main.npc[i].active = false;
					if (Main.netMode == NetmodeID.Server)
						NetMessage.SendData(MessageID.SyncNPC, number: i);
				}
		}

		/// <summary>
		/// Only for an abandoned event. A won one is ended from inside the Heave's own death, and
		/// tearing the NPC out from under itself there would cut the throes short.
		/// </summary>
		private static void ClearHeave()
		{
			int type = ModContent.NPCType<Content.NPCs.Bosses.Heave>();
			for (int i = 0; i < Main.maxNPCs; i++)
				if (Main.npc[i].active && Main.npc[i].type == type)
				{
					Main.npc[i].active = false;
					if (Main.netMode == NetmodeID.Server)
						NetMessage.SendData(MessageID.SyncNPC, number: i);
				}
		}

		private static void Announce(string key)
		{
			string text = Language.GetTextValue("Mods.Terrapex.Chat." + key);
			Color colour = new Color(210, 150, 240);
			if (Main.netMode == NetmodeID.SinglePlayer)
				Main.NewText(text, colour);
			else if (Main.netMode == NetmodeID.Server)
				ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), colour);
		}

		// ------------------------------------------------------------------ net

		/// <summary>
		/// Live state has to reach clients mid-event, which world data alone does not do:
		/// <c>NetSend</c> below only runs when someone joins. Terrapex.HandlePacket routes this.
		/// </summary>
		public static void Sync()
		{
			if (Main.netMode != NetmodeID.Server)
				return;

			ModPacket packet = ModContent.GetInstance<Terrapex>().GetPacket();
			packet.Write((byte)TerrapexPacket.BreachState);
			packet.Write(Active);
			packet.Write((byte)Wave);
			packet.Write((short)Kills);
			packet.WriteVector2(Origin);
			packet.Send();
		}

		public static void ReadState(BinaryReader reader)
		{
			Active = reader.ReadBoolean();
			Wave = reader.ReadByte();
			Kills = reader.ReadInt16();
			Origin = reader.ReadVector2();
		}

		// ------------------------------------------------------------------ world data

		public override void OnWorldLoad() => Reset();
		public override void OnWorldUnload() => Reset();

		private static void Reset()
		{
			Active = false;
			Wave = 0;
			Kills = 0;
			mouthTimer = 0;
			downedHeave = false;
			Origin = Vector2.Zero;
		}

		public override void SaveWorldData(TagCompound tag)
		{
			if (downedHeave)
				tag["downedHeave"] = true;
			// an event in progress survives a save, the way vanilla invasions do
			if (Active)
			{
				tag["breachWave"] = Wave;
				tag["breachKills"] = Kills;
				tag["breachOrigin"] = Origin;
			}
		}

		public override void LoadWorldData(TagCompound tag)
		{
			downedHeave = tag.ContainsKey("downedHeave");
			Active = tag.ContainsKey("breachWave");
			if (!Active)
				return;
			Wave = tag.GetInt("breachWave");
			Kills = tag.GetInt("breachKills");
			Origin = tag.Get<Vector2>("breachOrigin");
		}

		public override void NetSend(BinaryWriter writer)
		{
			writer.Write(downedHeave);
			writer.Write(Active);
			writer.Write((byte)Wave);
			writer.Write((short)Kills);
			writer.WriteVector2(Origin);
		}

		public override void NetReceive(BinaryReader reader)
		{
			downedHeave = reader.ReadBoolean();
			ReadState(reader);
		}
	}
}
