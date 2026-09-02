namespace Terrapex.Content.Projectiles
{
	/// <summary>
	/// The Rockfall Staff's stone. Same telegraph, same fall, same sprite — only the
	/// ownership flips, so the attack the boss taught the player is the one they get
	/// to use back.
	/// </summary>
	public class FriendlyRockfall : Rockfall
	{
		public override string Texture => "Terrapex/Content/Projectiles/Rockfall";

		protected override bool PlayerOwned => true;
	}
}
