using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Scarab;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// The Scarab Sultan's expert drop. A piece of its jewelled shell: +4 defence, and when you are
	/// hit the shell answers with a ring of eight sun motes - the cracked sun's burst, yours now.
	/// Once every <see cref="Cooldown"/> ticks, so it is a reply to a hit, not a hit farm.
	/// </summary>
	public class SultansCarapace : ModItem
	{
		public const int Motes = 8;
		public const int MoteDamage = 22;
		public const int Cooldown = 180;

		public override void SetDefaults()
		{
			Item.width = 28;
			Item.height = 26;
			Item.accessory = true;
			Item.defense = 4;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Expert;
			Item.expert = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
			=> player.GetModPlayer<SultansCarapacePlayer>().carapace = true;
	}

	/// <summary>The carapace's state on the player: whether it is worn, and its cooldown.</summary>
	public class SultansCarapacePlayer : ModPlayer
	{
		public bool carapace;
		private int cooldown;

		public override void ResetEffects() => carapace = false;

		public override void PostUpdate()
		{
			if (cooldown > 0)
				cooldown--;
		}

		public override void OnHurt(Player.HurtInfo info)
		{
			if (!carapace || cooldown > 0 || Player.whoAmI != Main.myPlayer)
				return;
			cooldown = SultansCarapace.Cooldown;
			int damage = (int)Player.GetTotalDamage(DamageClass.Generic).ApplyTo(SultansCarapace.MoteDamage);
			for (int i = 0; i < SultansCarapace.Motes; i++)
			{
				Vector2 dir = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / SultansCarapace.Motes + MathHelper.Pi / SultansCarapace.Motes);
				Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, dir * 3f,
					ModContent.ProjectileType<SunMote>(), damage, 2f, Player.whoAmI);
			}
		}
	}
}
