using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Buffs
{
	/// <summary>
	/// Plan item #74's buff. Every attack in this mod is telegraphed, and the telegraph is
	/// always a projectile — so lighting hostile projectiles up is literally "show me the
	/// tells", without touching a single draw call.
	/// </summary>
	public class RiftSight : ModBuff
	{
		public override void SetStaticDefaults() => Main.buffNoTimeDisplay[Type] = false;

		public override void Update(Player player, ref int buffIndex)
		{
			if (player.whoAmI != Main.myPlayer)
				return;

			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				Projectile p = Main.projectile[i];
				if (!p.active || !p.hostile)
					continue;
				if (Vector2.Distance(p.Center, player.Center) > 1400f)
					continue;

				Lighting.AddLight(p.Center, 0.55f, 0.18f, 0.70f);
				if (Main.rand.NextBool(10))
				{
					Dust d = Dust.NewDustPerfect(p.Center, DustID.PurpleTorch, Vector2.Zero, 90, default, 0.9f);
					d.noGravity = true;
					d.velocity = Vector2.Zero;
				}
			}
		}
	}
}
