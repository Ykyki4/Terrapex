using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;

namespace Terrapex.Content.Items.Mounts
{
	/// <summary>Plan item #133. One of the shell plates, big enough to stand on.</summary>
	public class RidingPlate : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 32;
			Item.height = 32;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.value = Item.sellPrice(gold: 20);
			Item.rare = ItemRarityID.Red;
			Item.UseSound = SoundID.Item79;
			Item.noMelee = true;
			Item.mountType = ModContent.MountType<global::Terrapex.Content.Mounts.PlateMount>();
			Item.buffType = ModContent.BuffType<PlateMountBuff>();
		}
	}
}
