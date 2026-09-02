using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Players;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// Plan item #82. The Colossus's reach, at 4%.
	///
	/// It does not stitch anything — the tier already has three weapons that do. It reaps
	/// instead: the arc it throws flies out and comes **back**, and every enemy it passes
	/// through on either leg banks a stack. Stacks make the next swing hit harder and the next
	/// arc fly wider, and they lapse in four seconds. So the scythe is worthless against one
	/// target and terrifying in a pack, which is what a drop-only weapon should be when the
	/// crafted three already cover the ordinary case.
	/// </summary>
	public class RiftScythe : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 52;
			Item.height = 52;
			Item.damage = 88;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 7f;
			Item.useTime = 32;
			Item.useAnimation = 32;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.scale = 1.4f;
			Item.UseSound = SoundID.Item1;
			Item.value = Item.sellPrice(gold: 14);
			Item.rare = ItemRarityID.Yellow;
			Item.shoot = ModContent.ProjectileType<ScytheArc>();
			Item.shootSpeed = 13f;
		}

		public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
			=> damage *= 1f + player.GetModPlayer<TerrapexPlayer>().reaped * TerrapexPlayer.ReapPerStack;

		public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
		{
			int reaped = Main.LocalPlayer.GetModPlayer<TerrapexPlayer>().reaped;
			tooltips.Add(new TooltipLine(Mod, "Reaped",
				Language.GetTextValue("Mods.Terrapex.Common.Reaped", reaped, TerrapexPlayer.MaxReaped)));
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback)
		{
			int reaped = player.GetModPlayer<TerrapexPlayer>().reaped;
			Projectile.NewProjectile(source, position, velocity,
				ModContent.ProjectileType<ScytheArc>(), (int)(damage * 0.7f), knockback,
				player.whoAmI, 0f, reaped);
			return false;
		}
	}
}
