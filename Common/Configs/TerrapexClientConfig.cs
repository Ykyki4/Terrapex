using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace Terrapex.Common.Configs
{
	/// <summary>How much of the reaper's reach is drawn round the player.</summary>
	public enum ReachStyle
	{
		/// <summary>Only the stretch of rim facing the cursor, fading out to either side.</summary>
		Arc,
		/// <summary>The whole circle, faint, with the facing stretch brighter.</summary>
		Full,
		/// <summary>No rim at all - only the mark where the cut will land.</summary>
		Reticle,
	}

	public class TerrapexClientConfig : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;

		public static TerrapexClientConfig Instance => ModContent.GetInstance<TerrapexClientConfig>();

		[DefaultValue(ReachStyle.Arc)]
		public ReachStyle Reach;

		[Range(0, 100)]
		[Slider]
		[DefaultValue(60)]
		public int ReachOpacity;

		/// <summary>0..1 multiplier every reach overlay is drawn at.</summary>
		public static float Opacity => (Instance?.ReachOpacity ?? 60) / 100f;

		public static ReachStyle Style => Instance?.Reach ?? ReachStyle.Arc;
	}
}
