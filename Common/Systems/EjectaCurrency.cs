using Terraria.GameContent.UI;
using Terraria.ModLoader;
using Terrapex.Content.Items.Materials;

namespace Terrapex.Common.Systems
{
	/// <summary>
	/// Registers Ejecta as a currency the Sifter's shop is priced in.
	///
	/// The event pays in rock rather than in coins so that what the Breach is worth cannot be
	/// earned anywhere else — a shop priced in gold is just a shop, and the run stops meaning
	/// anything the moment the player has money from somewhere.
	/// </summary>
	public class EjectaCurrency : ModSystem
	{
		/// <summary>The id handed to <c>Item.shopSpecialCurrency</c>.</summary>
		public static int Id { get; private set; }

		// PostSetupContent, not Load: the currency needs the item's type, which does not exist
		// until content is registered.
		public override void PostSetupContent()
			=> Id = CustomCurrencyManager.RegisterCurrency(
				new CustomCurrencySingleCoin(ModContent.ItemType<Ejecta>(), 999L));
	}
}
