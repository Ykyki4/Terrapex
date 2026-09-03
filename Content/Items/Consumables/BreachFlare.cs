using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Common.Systems;
using Terrapex.Content.Items.Materials;
using Terrapex.Content.Tiles;

namespace Terrapex.Content.Items.Consumables
{
	/// <summary>
	/// Starts the Breach. Underground only, and not because of a rule about where events are
	/// allowed — the crack comes from below, so a surface arena would let a player fight the
	/// whole event from a platform they built in advance, which is the shape the event exists
	/// to avoid.
	/// </summary>
	public class BreachFlare : ModItem
	{
		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 3;

		public override void SetDefaults()
		{
			Item.width = 22;
			Item.height = 26;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 40);
			Item.rare = ItemRarityID.Pink;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.useTurn = false;
			Item.consumable = true;
			Item.UseSound = SoundID.Roar with { Pitch = -0.4f };
		}

		public override bool CanUseItem(Player player) => BreachSystem.CanStart(player);

		public override bool? UseItem(Player player)
		{
			if (player.whoAmI != Main.myPlayer)
				return true;

			// A client cannot open the event itself — world state belongs to the server, and
			// relying on the server to run this hook a second time is not something the mod's
			// other summon item does either. So the client asks and the server decides.
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ModPacket packet = ModContent.GetInstance<Terrapex>().GetPacket();
				packet.Write((byte)TerrapexPacket.BreachStart);
				packet.Send();
				return true;
			}

			BreachSystem.Start(player.Center);
			return true;
		}

		public override void AddRecipes()
		{
			// Deliberately NOT priced in Ejecta. Ejecta only drops inside the Breach, so a
			// flare that cost it could never buy the first one. It is priced in the tier's
			// ordinary materials instead, and Ejecta pays for everything else on the altar.
			CreateRecipe()
				.AddIngredient<RiftsteelBar>(8)
				.AddIngredient<RiftEssence>(3)
				.AddTile<RiftAltarTile>()
				.Register();
		}
	}
}
