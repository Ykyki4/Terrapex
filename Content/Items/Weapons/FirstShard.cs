using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Projectiles;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// Plan item #114. It does not hit what you swung at — it hits everything that can see you.
	///
	/// The rule is literal: on every connect, every enemy within <see cref="Sight"/> that has an
	/// unbroken line to the player takes a share of the same blow, and each one gets a drawn
	/// line back so it is obvious which. That makes it the mod's only weapon that is *better*
	/// in the open and worthless around a corner — a melee weapon whose positioning question is
	/// the opposite of every other melee weapon's.
	/// </summary>
	public class FirstShard : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		private const float Sight = 780f;
		private const float Share = 0.4f;

		public override void SetDefaults()
		{
			Item.width = 58;
			Item.height = 58;
			Item.damage = 210;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 7.5f;
			Item.useTime = 24;
			Item.useAnimation = 24;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.scale = 1.25f;
			Item.UseSound = SoundID.Item1;
			Item.value = Item.sellPrice(gold: 40);
			Item.rare = ItemRarityID.Red;
		}

		public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
		{
			if (player.whoAmI != Main.myPlayer)
				return;

			SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.5f, Pitch = 0.5f }, player.Center);
			int share = (int)(damageDone * Share);
			if (share < 1)
				return;

			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC n = Main.npc[i];
				if (!n.active || n.friendly || n.dontTakeDamage || n.whoAmI == target.whoAmI)
					continue;
				if (Vector2.DistanceSquared(n.Center, player.Center) > Sight * Sight)
					continue;
				// "sees you" is line of sight, not a radius: the sword is paid in open ground
				if (!Collision.CanHitLine(player.MountedCenter, 1, 1, n.Center, 1, 1))
					continue;

				SightLine.Draw(player.GetSource_OnHit(n), player.MountedCenter, n.Center, player.whoAmI);
				n.SimpleStrikeNPC(share, 0, false, 0f, DamageClass.Melee, true, player.luck);
			}
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<PrimordiumBar>(18)
				.AddTile(ModContent.TileType<AnchorLegTile>())
				.Register();
		}
	}
}
