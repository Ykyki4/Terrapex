using System;
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

		/// <summary>
		/// Kill counts that close each wave. 40/70/100 was 210 kills for one run and played as a
		/// slaughter rather than as three waves — the first pass at these numbers assumed adds
		/// died slower than they do with six mouths open.
		/// </summary>
		public static readonly int[] WaveGoals = { 20, 35, 50 };

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

		/// <summary>The finale is owed but not placed yet. Retried every tick until it stands.</summary>
		private static bool pendingHeave;
		private static int heaveMouths;

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
				// every kill: the progress bar is drawn from this value, and a bar that moves in
				// steps of ten looks broken. The packet is twelve bytes
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
			{
				if (pendingHeave)
					TrySpawnHeave();
				return;                     // the finale is out; mouths stop opening
			}

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

		/// <summary>
		/// The event's progress, drawn by vanilla's own invasion bar.
		///
		/// <c>Main.ReportInvasionProgress</c> only writes four fields on the local client — there
		/// is no netcode behind it — so every client reports its own from the state the system
		/// already syncs. Two details are not optional: <c>invasionProgressMode</c> 0 forces the
		/// bar's alpha to zero every frame, so it has to be 2, and mode 2 is the one that keeps
		/// the bar pinned up while <c>invasionProgressNearInvasion</c> is set.
		///
		/// The badge next to the bar draws one of vanilla's own icons whatever we pass; an index
		/// outside 1-7 lands on the default, which is an icon with no name written under it.
		/// A modded icon is not something this bar can be given.
		/// </summary>
		public override void PostUpdateEverything()
		{
			if (Main.netMode == NetmodeID.Server || Main.dedServ)
				return;

			bool show = Active && !Main.gameMenu
				&& Main.LocalPlayer.active
				&& Main.LocalPlayer.Distance(Origin) < 4000f;

			if (!show)
			{
				// leave the bar exactly as vanilla leaves it when an invasion ends, or it sticks
				if (barUp)
				{
					barUp = false;
					Main.invasionProgress = -1;
					Main.invasionProgressMode = 0;
					Main.invasionProgressNearInvasion = false;
				}
				return;
			}

			barUp = true;
			bool finale = Wave > WaveGoals.Length;
			int goal = finale ? 1 : WaveGoals[Math.Clamp(Wave, 1, WaveGoals.Length) - 1];
			int done = finale ? 1 : Kills;

			Main.invasionProgressMode = 2;
			Main.invasionProgressNearInvasion = true;
			Main.ReportInvasionProgress(done, goal, 0, Math.Min(Wave, WaveGoals.Length + 1));
		}

		private static bool barUp;

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
		/// Puts one mouth somewhere near a player. Returns false when it could not find anywhere
		/// to put it, which is the caller's cue to try again on the next tick.
		///
		/// A floor is preferred and not required. The first pass looks for one, because a mouth
		/// standing on rock is what the event is about; after that it will hang in open air, in
		/// water, indoors, anywhere the body fits. The one thing it will not do is open inside
		/// solid rock — a buried mouth is invisible and everything it spawns is stuck in stone.
		/// </summary>
		private static bool OpenMouth()
		{
			Player host = NearestPlayer();
			if (host == null)
				return false;

			Point at = host.Center.ToTileCoordinates();

			// searched close to far: a mouth that opens forty tiles away is a spawner nobody
			// finds, and the dial only works if the player can reach what they are choosing about
			for (int attempt = 0; attempt < 90; attempt++)
			{
				bool wantFloor = attempt < 30;
				int reach = 8 + attempt / 2;
				int x = at.X + Main.rand.Next(-reach, reach + 1);
				int y = at.Y + Main.rand.Next(-reach / 2 - 4, reach / 2 + 5);
				if (!WorldGen.InWorld(x, y, 30))
					continue;

				if (wantFloor)
				{
					// solid under it, three tiles of headroom over it
					if (!WorldGen.SolidTile(x, y) || !Clear(x, y - 1, 3))
						continue;
					if (!Place(x, y * 16))
						return false;
					return true;
				}

				// anywhere the body fits, floor or no floor
				if (!Clear(x, y - 1, 3))
					continue;
				if (!Place(x, y * 16 + 28))
					return false;
				return true;
			}

			return false;
		}

		/// <summary>True when <paramref name="rows"/> tiles upward from (x, y) are all passable.</summary>
		private static bool Clear(int x, int y, int rows)
		{
			for (int i = 0; i < rows; i++)
				if (WorldGen.SolidTile(x, y - i))
					return false;
			return true;
		}

		private static bool Place(int tileX, int worldY)
		{
			int index = NPC.NewNPC(new Terraria.DataStructures.EntitySource_WorldEvent(),
				tileX * 16 + 8, worldY, ModContent.NPCType<BreachMouth>());
			if (index >= Main.maxNPCs)
				return false;

			// NewNPC does not sync itself, and without this the mouths exist on the server and
			// on nobody's screen
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.SyncNPC, number: index);
			return true;
		}

		/// <summary>
		/// The finale is priced here and placed later. Every mouth still standing when the third
		/// wave closes makes the Heave bigger — that is the bill for the extra Ejecta they have
		/// been paying out all event — and then they are closed: six spawners running through a
		/// boss fight is not a boss fight.
		///
		/// The spawn itself is retried every tick rather than attempted once. The first version
		/// dropped the boss 460 px to the side and gave up if <c>NewNPC</c> failed, so it could
		/// land inside solid rock, or fail outright when the swarm had filled the NPC array —
		/// which is exactly the state the end of wave three is in.
		/// </summary>
		private static void SummonHeave()
		{
			heaveMouths = MouthsOpen();
			pendingHeave = true;
			ClearMouths();
			Announce("BreachHeave");
			Sync();
		}

		private static void TrySpawnHeave()
		{
			int type = ModContent.NPCType<Content.NPCs.Bosses.Heave>();
			if (NPC.AnyNPCs(type))
			{
				pendingHeave = false;
				return;
			}

			Player host = NearestPlayer();
			if (host == null)
				return;

			Point at = host.Center.ToTileCoordinates();
			Vector2 where = host.Center - new Vector2(0f, 90f);

			// it is 7x5 tiles: look for somewhere it actually fits, and if the cavern will not
			// give one up, put it on the player, who is standing in open space by definition
			for (int attempt = 0; attempt < 80; attempt++)
			{
				int reach = 6 + attempt / 2;
				int x = at.X + Main.rand.Next(-reach, reach + 1);
				int y = at.Y + Main.rand.Next(-reach / 2 - 3, reach / 2 + 4);
				if (!WorldGen.InWorld(x, y, 30) || !Fits(x, y))
					continue;
				where = new Vector2(x * 16 + 8, y * 16 + 8);
				break;
			}

			int index = NPC.NewNPC(new Terraria.DataStructures.EntitySource_WorldEvent(),
				(int)where.X, (int)where.Y, type);
			if (index >= Main.maxNPCs)
				return;                     // the array is full: try again next tick

			NPC heave = Main.npc[index];
			heave.lifeMax += Content.NPCs.Bosses.Heave.LifePerMouth * heaveMouths;
			heave.life = heave.lifeMax;
			heave.damage += Content.NPCs.Bosses.Heave.DamagePerMouth * heaveMouths;
			pendingHeave = false;

			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.SyncNPC, number: index);
		}

		/// <summary>Room for a 7x5 body with its feet on the tile below (x, y).</summary>
		private static bool Fits(int x, int y)
		{
			for (int dx = -3; dx <= 3; dx++)
				for (int dy = -4; dy <= 0; dy++)
					if (WorldGen.SolidTile(x + dx, y + dy))
						return false;
			return true;
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
			pendingHeave = false;
			heaveMouths = 0;
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
				tag["breachHeaveMouths"] = heaveMouths;
				if (pendingHeave)
					tag["breachPendingHeave"] = true;
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
			heaveMouths = tag.GetInt("breachHeaveMouths");
			pendingHeave = tag.ContainsKey("breachPendingHeave");
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
