using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Terrapex.Content.Projectiles
{
	// RendWhip.png, the same vertical strip every whip in the mod uses:
	//   y 0  h 26  handle
	//   y 26 h 16  link, thick
	//   y 42 h 16  link, thin
	//   y 58 h 16  link, glowing
	//   y 74 h 18  tip
	public class RendWhip : ModProjectile
	{
		public override void SetStaticDefaults() => ProjectileID.Sets.IsAWhip[Type] = true;

		public override void SetDefaults()
		{
			Projectile.DefaultToWhip();
			Projectile.WhipSettings.Segments = 24;
			Projectile.WhipSettings.RangeMultiplier = 1.2f;
		}

		public override void OnSpawn(IEntitySource source)
		{
			// the reach is read once, at the swing: recomputing it mid-flight would make the
			// whip change length in the middle of its own arc
			Player owner = Main.player[Projectile.owner];
			int minions = 0;
			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				Projectile p = Main.projectile[i];
				if (p.active && p.owner == Projectile.owner && p.minion && p.minionSlots > 0f)
					minions++;
			}
			Projectile.WhipSettings.RangeMultiplier = 1.2f + 0.22f * minions;
			Projectile.WhipSettings.Segments = 24 + 4 * minions;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;
			Projectile.damage = (int)(Projectile.damage * 0.75f);
		}

		private float Timer
		{
			get => Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			List<Vector2> points = new List<Vector2>();
			Projectile.FillWhipControlPoints(Projectile, points);

			Main.instance.LoadProjectile(Type);
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Vector2 pos = points[0];

			for (int i = 0; i < points.Count - 1; i++)
			{
				Rectangle frame = new Rectangle(0, 0, 22, 26);
				float scale = 1f;

				if (i == points.Count - 2)
				{
					frame.Y = 74;
					frame.Height = 18;

					Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out int _, out float _);
					float t = Timer / timeToFlyOut;
					scale = MathHelper.Lerp(0.6f, 1.8f,
						Utils.GetLerpValue(0.1f, 0.7f, t, true) * Utils.GetLerpValue(0.9f, 0.7f, t, true));
				}
				else if (i > 15)
				{
					frame.Y = 58;
					frame.Height = 16;
				}
				else if (i > 8)
				{
					frame.Y = 42;
					frame.Height = 16;
				}
				else if (i > 0)
				{
					frame.Y = 26;
					frame.Height = 16;
				}

				Vector2 element = points[i];
				Vector2 diff = points[i + 1] - element;
				float rotation = diff.ToRotation() - MathHelper.PiOver2;
				Color color = Lighting.GetColor(element.ToTileCoordinates());
				Vector2 origin = new Vector2(frame.Width / 2f, frame.Height / 2f);

				Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color, rotation,
					origin, scale, SpriteEffects.None, 0);
				pos += diff;
			}
			return false;
		}
	}
}
