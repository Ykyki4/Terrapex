using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Scarab;

namespace Terrapex.Content.Items.Weapons.Scarab
{
	/// <summary>
	/// The Sultan's khopesh - the melee drop. A hooked sword: every hit sets the target alight, and
	/// every <see cref="FlareEvery"/>th hit lets a sun flare go off on it, a burst that catches what
	/// stands next to it too. Counted across targets, so a crowd brings the flare round faster.
	/// </summary>
	public class SultansKhopesh : ModItem
	{
		public const int FlareEvery = 3;
		public const float FlareShare = 0.75f;

		private int hits;

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 44;
			Item.height = 44;
			Item.damage = 28;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 5.5f;
			Item.useTime = 22;
			Item.useAnimation = 22;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.UseSound = SoundID.Item1;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.Green;
		}

		public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(BuffID.OnFire, 180);
			if (++hits < FlareEvery)
				return;
			hits = 0;
			Projectile.NewProjectile(player.GetSource_ItemUse(Item), target.Center, Vector2.Zero,
				ModContent.ProjectileType<KhopeshFlare>(), (int)(player.GetWeaponDamage(Item) * FlareShare), 3f, player.whoAmI);
		}

		public override void MeleeEffects(Player player, Rectangle hitbox)
		{
			if (Main.rand.NextBool(3))
				Dust.NewDustDirect(hitbox.TopLeft(), hitbox.Width, hitbox.Height, DustID.GoldFlame, 0f, 0f, 100, default, 1f).noGravity = true;
		}
	}
}
