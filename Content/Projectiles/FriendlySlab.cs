namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// The Slabmaul's plate. Same stone, same wind-up, owned by the player — the pattern the
	/// Rockfall Staff already uses, so the weapon costs the mod no second sprite and the boss's
	/// move stays legible when the player is the one making it.
	/// </summary>
	public class FriendlySlab : HeaveSlab
	{
		public override string Texture => "Terrapex/Content/Projectiles/HeaveSlab";

		protected override bool PlayerOwned => true;
	}
}
