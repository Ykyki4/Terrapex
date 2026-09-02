using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;
using Terrapex.Content.Projectiles;

namespace Terrapex.Content.Items.Pets
{
	/// <summary>Plan item #132. A riftling that decided to follow you home.</summary>
	public class Shardling : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 24;
			Item.height = 24;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.value = Item.sellPrice(gold: 4);
			Item.rare = ItemRarityID.Red;
			Item.UseSound = SoundID.Item2;
			Item.buffType = ModContent.BuffType<ShardlingBuff>();
			Item.shoot = ModContent.ProjectileType<ShardlingCompanion>();
		}

		public override void UseStyle(Player player, Microsoft.Xna.Framework.Rectangle heldItemFrame)
		{
			if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
				player.AddBuff(Item.buffType, 3600);
		}
	}
}
