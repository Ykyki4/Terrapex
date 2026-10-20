using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;
using Terrapex.Content.Items.Weapons.Reaper;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons.Trees.Reaper
{
	/// <summary>
	/// Node 3, T5: the Eclipse quenched in the Mourning Scythe's ash and the Death Sickle's dark.
	/// Everything the Eclipse did, and the Reap now brings three seconds of night: the reach circle
	/// darkens and every cut inside it lands as if on the edge - so every cut of the night is also
	/// a double rite. The Reap throws the Death Sickle's three sickles as well.
	/// </summary>
	public class LongNight : MoonScythe
	{
		public const float SickleShare = 0.6f;

		public override int Node => 3;
		public override float Reach => 120f;
		public override float RangeTiles => 22f;
		public override Color SlashColor => new(190, 200, 255);

		protected override void SetScytheDefaults() => Defaults(52, 116, 22, 6f, ItemRarityID.Yellow, 12);

		protected override void ReapExtra(Player player, IEntitySource source, Vector2 center, Vector2 dir,
			int damage, float knockback)
		{
			for (int i = -1; i <= 1; i++)
			{
				int p = Projectile.NewProjectile(source, center, dir.RotatedBy(i * 0.3f) * 12f, ProjectileID.DeathSickle,
					(int)(damage * SickleShare), knockback * 0.5f, player.whoAmI);
				if (p >= 0 && p < Main.maxProjectiles)
					Main.projectile[p].DamageType = ModContent.GetInstance<ReapingDamage>();
			}
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<Eclipse>()
				.AddIngredient<MourningScythe>()
				.AddIngredient(ItemID.DeathSickle)
				.AddTile<AnchorLegTile>()
				.Register();
		}
	}
}
