using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Weapons.Reaper;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// Plan item #82. The Colossus's reach, at 4% - and since the reaper class, a scythe of that
	/// class rather than a melee weapon. It was a reaper's weapon before the class existed.
	///
	/// It does not stitch anything - the tier already has three weapons that do. It reaps instead:
	/// after each cut a crescent flies out of the point, turns, and comes **back**, and every enemy
	/// it passes through on either leg banks a stack. Stacks make the next stroke hit harder and the
	/// next crescent fly wider, and they lapse in four seconds. So the scythe is worthless against
	/// one target and terrifying in a pack, which is what a drop-only weapon should be when the
	/// crafted ones already cover the ordinary case.
	///
	/// The file stays in Weapons/ rather than Weapons/Reaper/ so the class keeps its name and its
	/// place in every save that already holds one.
	/// </summary>
	public class RiftScythe : ScytheItem
	{
		public override float Reach => 116f;

		public override float RangeTiles => 20f;

		public override Color SlashColor => new(127, 242, 224);

		protected override void SetScytheDefaults()
		{
			Item.width = 52;
			Item.height = 52;
			Item.damage = 80;
			Item.knockBack = 7f;
			Item.useTime = 28;
			Item.useAnimation = 28;
			Item.value = Item.sellPrice(gold: 14);
			Item.rare = ItemRarityID.Yellow;
		}

		public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
			=> damage *= 1f + player.GetModPlayer<TerrapexPlayer>().reaped * TerrapexPlayer.ReapPerStack;

		/// <summary>The crescent, out of the point and back, born wider for every stack already banked.</summary>
		public override void AfterStrike(Player player, Projectile slash, Vector2 aim, bool edge)
		{
			int reaped = player.GetModPlayer<TerrapexPlayer>().reaped;
			Projectile.NewProjectile(slash.GetSource_FromThis(), slash.Center, aim * 11f,
				ModContent.ProjectileType<ScytheArc>(), (int)(slash.damage * 0.6f), slash.knockBack,
				player.whoAmI, 0f, reaped);
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips)
		{
			base.ModifyTooltips(tooltips);
			int reaped = Main.LocalPlayer.GetModPlayer<TerrapexPlayer>().reaped;
			tooltips.Add(new TooltipLine(Mod, "Reaped",
				Language.GetTextValue("Mods.Terrapex.Common.Reaped", reaped, TerrapexPlayer.MaxReaped)));
		}
	}
}
