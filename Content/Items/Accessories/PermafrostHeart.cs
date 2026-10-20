using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terrapex.Content.Projectiles.Rime;

namespace Terrapex.Content.Items.Accessories
{
	/// <summary>
	/// The Rime's expert accessory. Stand still for a second and frost grows over you into a
	/// shell; the next hit you take is cut by <see cref="Cut"/> and breaks the shell into six
	/// splinters. The mountain's own way of fighting - stay put, take it, give it back.
	/// Also: +3 defence, immunity to Chilled and Frozen.
	/// </summary>
	public class PermafrostHeart : ModItem
	{
		public const int GrowTicks = 60;
		public const float Cut = 0.3f;
		public const int Splinters = 6;
		public const int SplinterDamage = 30;

		public override void SetDefaults()
		{
			Item.width = 26;
			Item.height = 26;
			Item.accessory = true;
			Item.defense = 3;
			Item.expert = true;
			Item.rare = ItemRarityID.Expert;
			Item.value = Item.sellPrice(gold: 3);
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
			player.GetModPlayer<PermafrostHeartPlayer>().equipped = true;
			player.buffImmune[BuffID.Chilled] = true;
			player.buffImmune[BuffID.Frozen] = true;
		}
	}

	/// <summary>The Permafrost Heart's shell: how long the player has stood still, and whether the frost is up.</summary>
	public class PermafrostHeartPlayer : ModPlayer
	{
		public bool equipped;
		public int still;
		public bool shell;

		public override void ResetEffects() => equipped = false;

		public override void PostUpdate()
		{
			if (!equipped)
			{
				still = 0;
				shell = false;
				return;
			}

			if (!shell)
			{
				bool standing = Player.velocity.LengthSquared() < 0.04f;
				still = standing ? still + 1 : 0;
				if (still > 0 && Main.rand.NextBool(4))
				{
					// frost creeping up from the feet while it grows
					Dust d = Dust.NewDustPerfect(Player.Bottom + new Vector2(Main.rand.NextFloat(-12f, 12f),
						-still / (float)PermafrostHeart.GrowTicks * Player.height), DustID.Ice, Vector2.Zero, 120, default, 0.8f);
					d.noGravity = true;
				}
				if (still >= PermafrostHeart.GrowTicks)
				{
					shell = true;
					SoundEngine.PlaySound(SoundID.Item30 with { Pitch = 0.4f, Volume = 0.6f }, Player.Center);
				}
				return;
			}

			Lighting.AddLight(Player.Center, 0.1f, 0.22f, 0.32f);
			if (Main.rand.NextBool(5))
			{
				Dust d = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2CircularEdge(22f, 28f), DustID.Ice,
					Vector2.Zero, 100, default, 0.9f);
				d.noGravity = true;
			}
		}

		public override void ModifyHurt(ref Player.HurtModifiers modifiers)
		{
			if (shell)
				modifiers.FinalDamage *= 1f - PermafrostHeart.Cut;
		}

		public override void OnHurt(Player.HurtInfo info)
		{
			if (!shell)
			{
				still = 0;
				return;
			}
			shell = false;
			still = 0;
			SoundEngine.PlaySound(SoundID.Item27, Player.Center);
			for (int i = 0; i < 16; i++)
				Dust.NewDustPerfect(Player.Center, DustID.Ice, Main.rand.NextVector2Circular(5f, 5f), 80, default, 1.2f);
			if (Player.whoAmI != Main.myPlayer)
				return;
			for (int i = 0; i < PermafrostHeart.Splinters; i++)
			{
				Vector2 v = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / PermafrostHeart.Splinters) * 8f;
				Projectile.NewProjectile(Player.GetSource_Accessory(new Item(ModContent.ItemType<PermafrostHeart>())),
					Player.Center, v, ModContent.ProjectileType<RimeSplinter>(), PermafrostHeart.SplinterDamage, 3f, Player.whoAmI);
			}
		}
	}
}
