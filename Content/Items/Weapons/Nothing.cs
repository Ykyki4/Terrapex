using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terrapex.Common.Players;

namespace Terrapex.Content.Items.Weapons
{
	/// <summary>
	/// Plan item #119. Its damage doubles over twelve untouched seconds and returns to nothing
	/// the instant anything lands on you — armour, dodges and immunity frames included, because
	/// the counter is reset by the hurt itself.
	///
	/// It is the only weapon in the mod whose number the player cannot raise by playing better
	/// with the weapon, only by playing better with everything else. That is the point: it is a
	/// drop, it is optional, and it exists to reward a player who has stopped being hit.
	/// </summary>
	public class Nothing : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults()
		{
			Item.width = 54;
			Item.height = 54;
			Item.damage = 190;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 6f;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.scale = 1.15f;
			Item.UseSound = SoundID.Item1;
			Item.value = Item.sellPrice(gold: 48);
			Item.rare = ItemRarityID.Purple;
		}

		public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
			=> damage *= 1f + player.GetModPlayer<TerrapexPlayer>().UntouchedShare;

		public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
		{
			float share = Main.LocalPlayer.GetModPlayer<TerrapexPlayer>().UntouchedShare;
			tooltips.Add(new TooltipLine(Mod, "NothingCharge",
				Language.GetTextValue("Mods.Terrapex.Common.Untouched", (int)(share * 100f))));
		}
	}
}
