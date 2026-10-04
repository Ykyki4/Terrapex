using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Reaper;

namespace Terrapex.Content.Items.Armor
{
	[AutoloadEquip(EquipType.Legs)]
	public class RoninSuneate : ModItem
	{
		public const float MoveSpeed = 0.08f;
		public const int ReapingCrit = 4;

		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 18;
			Item.value = Item.buyPrice(gold: 6);
			Item.rare = ItemRarityID.Orange;
			Item.defense = 6;
		}

		public override void UpdateEquip(Player player)
		{
			player.moveSpeed += MoveSpeed;
			player.GetCritChance<ReapingDamage>() += ReapingCrit;
		}
	}
}
