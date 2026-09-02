using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Buffs;

namespace Terrapex.Content.Mounts
{
	/// <summary>
	/// Plan item #133's mount. A shell plate, ridden.
	///
	/// Only the <c>_Back</c> texture is authored — a mount draws behind the player by default
	/// and a plate is a slab under the feet, so there is nothing that would sit in front. The
	/// autoloader only loads the texture variants that exist, so the missing ones are silence
	/// rather than a load failure.
	/// </summary>
	public class PlateMount : ModMount
	{
		public override void SetStaticDefaults()
		{
			MountData.spawnDust = DustID.WhiteTorch;
			MountData.buff = ModContent.BuffType<PlateMountBuff>();

			MountData.heightBoost = 18;
			MountData.fallDamage = 0f;
			MountData.runSpeed = 11f;
			MountData.dashSpeed = 11f;
			MountData.flightTimeMax = 320;
			MountData.fatigueMax = 320;
			MountData.jumpHeight = 12;
			MountData.acceleration = 0.28f;
			MountData.jumpSpeed = 6.5f;
			MountData.blockExtraJumps = false;
			MountData.constantJump = true;
			MountData.usesHover = true;

			MountData.totalFrames = 4;
			MountData.playerYOffsets = Enumerable.Repeat(14, MountData.totalFrames).ToArray();
			MountData.xOffset = 0;
			MountData.yOffset = 6;
			MountData.bodyFrame = 3;
			MountData.playerHeadOffset = 14;

			MountData.standingFrameCount = 4;
			MountData.standingFrameDelay = 12;
			MountData.standingFrameStart = 0;
			MountData.runningFrameCount = 4;
			MountData.runningFrameDelay = 8;
			MountData.runningFrameStart = 0;
			MountData.flyingFrameCount = 4;
			MountData.flyingFrameDelay = 6;
			MountData.flyingFrameStart = 0;
			MountData.inAirFrameCount = 4;
			MountData.inAirFrameDelay = 8;
			MountData.inAirFrameStart = 0;
			MountData.idleFrameCount = 4;
			MountData.idleFrameDelay = 12;
			MountData.idleFrameStart = 0;
			MountData.idleFrameLoop = true;

			if (Main.dedServ)
				return;

			MountData.textureWidth = MountData.backTexture.Width();
			MountData.textureHeight = MountData.backTexture.Height() / MountData.totalFrames;
		}
	}
}
