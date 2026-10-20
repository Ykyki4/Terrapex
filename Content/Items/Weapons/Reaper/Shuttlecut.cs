using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles.Reaper;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons.Reaper
{
	/// <summary>
	/// The Weaver's scythe: a shuttle for a blade, pale silk on a dark frame. After the cut it
	/// strings thread from the point to the two nearest enemies, and the threads cut whatever
	/// crosses them - the boss's mechanic handed to the reaper, the way the Weaver set hands over
	/// the loom.
	/// </summary>
	public class Shuttlecut : ScytheItem
	{
		public override float Reach => 118f;

		public override float RangeTiles => 22f;

		public override Color SlashColor => new(194, 224, 218);

		protected override void SetScytheDefaults()
		{
			Item.width = 56;
			Item.height = 56;
			Item.damage = 112;
			Item.knockBack = 5.5f;
			Item.useTime = 22;
			Item.useAnimation = 22;
			Item.value = Item.sellPrice(gold: 10);
			Item.rare = ItemRarityID.Yellow;
		}

		/// <summary>Threads from the point to the two nearest enemies within reach of it.</summary>
		public override void AfterStrike(Player player, Projectile slash, Vector2 aim, bool edge)
		{
			const float reach = 360f;
			int first = -1, second = -1;
			float d1 = reach, d2 = reach;
			foreach (NPC npc in Main.ActiveNPCs)
			{
				if (!npc.CanBeChasedBy())
					continue;
				float d = npc.Distance(slash.Center);
				if (d < d1) { second = first; d2 = d1; first = npc.whoAmI; d1 = d; }
				else if (d < d2) { second = npc.whoAmI; d2 = d; }
			}
			foreach (int t in new[] { first, second })
			{
				if (t >= 0)
					ReapThread.Between(slash.GetSource_FromThis(), slash.Center, Main.npc[t].Center,
						(int)(slash.damage * 0.35f), player.whoAmI);
			}
		}
	}
}
