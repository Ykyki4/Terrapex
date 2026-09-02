using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.GlobalNPCs;
using Terrapex.Common.Players;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// Plan item #78. The tier's opening statement: hit one thing, then another, and they are
	/// stitched. Damage on either now lands on both.
	///
	/// The remembered target lives on the player rather than on the item, and every binding
	/// weapon in T4 reads the same memory. That is deliberate — the thread is something the
	/// player is holding, not something each weapon owns, so swapping from the Seam to the
	/// Stitch mid-fight finishes the stitch instead of starting over.
	///
	/// The sword's own beat is the third swing: hit something already stitched and it drags
	/// the pair into each other and burns the thread. Stitch, stitch, pull. Binding alone was
	/// bookkeeping; this is the part you aim.
	/// </summary>
	public class Seam : ModItem
	{
		/// <summary>Share of the swing that lands on *both* ends when the thread is pulled.</summary>
		public const float YankShare = 0.9f;

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 44;
			Item.height = 44;
			Item.damage = 84;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 5.5f;
			Item.useTime = 22;
			Item.useAnimation = 22;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.UseSound = SoundID.Item1;
			Item.value = Item.sellPrice(gold: 12);
			Item.rare = ItemRarityID.Yellow;
		}

		public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
		{
			// third beat: the target is already stitched, so this swing pulls the thread
			TerrapexPlayer mp = player.GetModPlayer<TerrapexPlayer>();
			BoundGlobalNPC bound = target.GetGlobalNPC<BoundGlobalNPC>();
			if (bound.IsBound)
			{
				float share = YankShare * (mp.darnerMelee ? 1.5f : 1f);
				BoundGlobalNPC.Yank(target, Main.npc[bound.Partner], (int)(damageDone * share));
				return;
			}

			NPC other = mp.TakeSeamTarget(target);
			if (other != null)
				BoundGlobalNPC.Bind(other, target);
		}

		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<VoidCloth>(6)
				.AddIngredient(ItemID.HallowedBar, 12)
				.AddIngredient(ItemID.SoulofMight, 8)
				.AddTile(ModContent.TileType<RiftAltarTile>())
				.Register();
		}
	}
}
