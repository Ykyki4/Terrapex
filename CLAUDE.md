# Terrapex

tModLoader 1.4.4 mod (net8.0). Calamity-flavoured "Rift" set built around one boss,
**Хранитель Разлома / Keeper of the Rift**, plus its weapon, summon item and trash mob.
All art in this mod is hand-generated pixel art — see `Art/`.

Design docs for the bosses: **`BOSS_KEEPER.md`**, **`BOSS_WEAVER.md`** and
**`BOSS_FIRST_KEEPER.md`** (phases,
attack tables with tells, projectile table, what is still missing). Read the relevant one
before changing fight behaviour — the numbers in it and the numbers in the code are meant
to stay in sync.

## Build

```
dotnet build
```

Runs the tModLoader packaging step and drops `Terrapex.tmod` straight into
`%USERPROFILE%\Documents\My Games\Terraria\tModLoader\Mods`. In game: Workshop →
Develop Mods → Reload.

- **`TML003: Please close tModLoader…`** means the game is running. tML launches as a
  `dotnet` process, so it hides in the process list. Close it and rebuild.
- `dotnet build -t:CoreCompile` on its own **does not work here** — invoking that target
  in isolation skips the reference setup and floods you with `CS0518: Predefined type
  'System.Int32' is not defined`. Those errors are an artefact of the invocation, not the
  code. Always use plain `dotnet build`.
- `build.txt` carries `buildIgnore = Art\*, *.py, .vs\*` so the working art never ships
  inside the `.tmod`.

## Layout

| Path | What |
|---|---|
| `CONTENT_PLAN.md` | the whole mod's roadmap: 7 tiers, 4 bosses, 133 items, production order |
| `Content/Tiles/` | `FissuriteOreTile`, `CrackedStoneTile`, `RiftTorchTile` (T0) |
| `Content/Items/{Placeable,Materials,Tools,Armor,Accessories}/` | the T0 fissurite branch |
| `Common/Players/TerrapexPlayer.cs` | set-bonus hit counter, Fissure Sight ore scan |
| `Common/GlobalNPCs/CrackedGlobalNPC.cs` | the `Cracked` debuff's +15% incoming damage |
| `Common/Systems/FissuriteWorldGen.cs` | fissurite veins, inserted after the vanilla `Shinies` pass |
| `Content/NPCs/Bosses/DormantEye.cs` | boss 1: rolls on the ground, 4 plates, one phase change at 25% |
| `Content/NPCs/Bosses/EyePlate.cs` | its shell slab — orbit only, no throw |
| `Content/NPCs/Bosses/KeeperOfTheRift.cs` | the boss: 3 phases, 13 attacks, ordered rotations, enrage, desperation |
| `Content/NPCs/Bosses/KeeperPlate.cs` | orbiting destructible shell plate (a real NPC, not a sprite) |
| `Content/NPCs/{FissureSlime,FissureSlimelet}.cs` | T0 splitter. The fragment is its own type because loot rules are per type |
| `Content/NPCs/Hangstone.cs` | T0 ceiling ambusher — rattles for 34 ticks before it lets go |
| `Content/NPCs/{FacetBeetle,FacetBroodmother}.cs` | T0 ore-carrier and the T1 mini-boss that lays them |
| `Content/NPCs/Voidfly.cs` | T0, harmless, flees; the tier's rift dust |
| `Content/NPCs/Spall.cs` | T1: armoured on the face it shows you. The mod's first "where is it looking" enemy |
| `Content/NPCs/{PlateBearer,PlateShepherd}.cs` | T3: a plate as a damage budget, and the mini-boss that re-arms them |
| `Content/NPCs/RiftReaper.cs` | T3 flyer, drawn aim line then a dash |
| `Content/NPCs/Riftling.cs` | underground hardmode mob, custom approach/lunge AI |
| `Content/NPCs/Mirrorling.cs` | T2 cavern mob: replays the player's velocity from 30 ticks ago, mirrored |
| `Content/NPCs/HollowEcho.cs` | T2 dungeon ghost: ignores tiles, harmless except during its telegraphed charge |
| `Content/NPCs/Weaverling.cs` | T4 pair mob: strings a damaging thread to another Weaverling |
| `Content/NPCs/RiftColossus.cs` | T4: slow, 2600 HP, chews the tiles in front of it |
| `Common/GlobalNPCs/BoundGlobalNPC.cs` | T4's mechanic: two enemies stitched, 45% of every hit echoed down the thread |
| `Common/BossDeath.cs` | the throes all four bosses die through - shared timing, own dust each |
| `Common/RiftDraw.cs` | shared trail / bloom / ring helpers for every projectile |
| `Content/NPCs/Bosses/WeaverOfTheRift.cs` | boss 3: three phases, anchors, thread hazards |
| `Content/NPCs/Bosses/WeaverAnchor.cs` | its legs; each one standing takes 10% off the damage the body receives, and its death staggers the boss |
| `Content/NPCs/Bosses/FirstKeeper.cs` | boss 4: the Regard - a cone of sight that doubles the damage both ways inside it |
| `Content/NPCs/Bosses/KeeperLid.cs` | its eight lids. Not armour: each one alive **narrows** the cone |
| `Content/NPCs/KeeperEcho.cs` | T6 rare mob, the boss's mechanic in miniature - it teaches the fight before you fight it |
| `Content/Projectiles/GazeRay.cs` | the line of sight, made lethal. Fixed, sweeping or tracking - never two of those at once |
| `Content/Projectiles/Watcher.cs` | an eye set down in the arena: opens, commits to one line, fires once |
| `Content/Projectiles/BlinkRing.cs` | a ring thrown outward with one wedge missing - the Weaver's closing web, inside out |
| `Common/Players/RegardLayer.cs` | draws the T6 set's cone and marks what is inside it |
| `Content/Projectiles/RiftThread.cs` | hostile line hazard: 40 harmless ticks, then lethal, then 24 fading. Either end may be an NPC, a player or a fixed world point |
| `Content/Projectiles/WebCollapse.cs` | the closing 12-gon net with a two-side door — one projectile because the ring has to move |
| `Content/Projectiles/FriendlyThread.cs` | the player's half of the same idea |
| `Content/Tiles/AnchorLegTile.cs` | T5 crafting station, also counts as a Rift Altar |
| `Content/Tiles/RiftAltarTile.cs` | the mod's own crafting station — every rift recipe from T2 on |
| `Content/Projectiles/Rift{Shard,Orb,Tear,Laser,Mine}.cs` | projectiles |
| `Content/Projectiles/Rift{Flare,Ring}.png` | effect textures with no `.cs` — requested by path at draw time |
| `Content/Items/Weapons/RiftshardCleaver.cs` | boss drop, glowmask in `PostDrawInWorld`/`PostDrawInInventory` |
| `Content/Items/Consumables/FracturedEye.cs` | summon item + recipe |
| `Common/Systems/DownedBossSystem.cs` | `downedKeeper` world flag |
| `Common/GlobalProjectiles/MirrorGlobalProjectile.cs` | one reflect roll per projectile, for the Mirror Charm |
| `Common/GlobalNPCs/GlassthreadGlobalNPC.cs` | the Glassthread tag: minions crit a tagged target |
| `Common/GlobalNPCs/RiftMarkGlobalNPC.cs` | the Rift Mark debuff's +12% incoming damage |
| `Common/GlobalProjectiles/AcceleratorGlobalProjectile.cs` | Shard Accelerator: your shots wind up as they travel |
| `Content/Projectiles/GuardPlate.cs` | the orbiting shell the Riftsteel set and Carapace Charm both grant |
| `Localization/en-US_Mods.Terrapex.hjson` | all display names, tooltips, bestiary, boss chat |
| `Art/` | working art: generators, master sheets, previews, archive (never shipped) |
| `Wiki/` | the public wiki, **generated from this repo's own sources** — see `Wiki/README.md` |

## Conventions that are easy to get wrong

**Localization nesting.** The filename `en-US_Mods.Terrapex.hjson` *already* supplies the
`Mods.Terrapex` prefix. The file must be **flat** — top-level `Items:`, `NPCs:`,
`Projectiles:`, `Chat:`. Wrapping them in `Mods: { Terrapex: { … } }` silently produces
`Mods.Terrapex.Mods.Terrapex.*` keys and tModLoader then appends its own empty block.

**Sprite sheets are vertical.** tModLoader stacks NPC and projectile frames top to bottom.
The generators in `Art/generators/` and the LibreSprite scripts produce horizontal strips
for inspection; anything that ships gets converted. `Art/tml/` holds the converted ones.

**One atlas per NPC.** The boss's `KeeperOfTheRift.png` is a single 42-frame atlas and
`FindFrame` picks a range out of it. Frame map (constants live at the top of the class):

| Frames | What |
|---|---|
| 0–7 | phase 1, shell closed |
| 8–15 | phase 2 |
| 16–25 | phase break (plays once) |
| 26–33 | phase 3 |
| 34–41 | phase 1, eye open — during attacks |

**Sheet layouts that are not negotiable.** Three textures have to match what the game
frames them with, and none of them fail loudly — they just render wrong:

| Texture | Layout |
|---|---|
| block sheets (`FissuriteOreTile`, `CrackedStoneTile`) | **16 × 15** frames of 16×16 on an 18 px pitch → **288×270**. Columns 13–15 carry the half-brick and slope frames a hammered block needs; a 13-column sheet leaves a sloped tile with nothing to draw. Not every cell is used — the occupancy is Terraria's frame map, copied from ExampleMod's ore sheet. And frames are **not solid squares**: exposed corners are cut (200–248 opaque px of 256), which is what makes a wall read as rock instead of as a grid of tiles |
| torch tiles (`RiftTorchTile`, `_Flame`) | six 20×20 frames on a 22 px pitch → 132×22. `frameX` 0/22/44 = floor / left wall / right wall, **≥ 66 = unlit** (`ModifyLight` and `PostDraw` both test `< 66`) |
| whips (`DustLashWhip`) | vertical strip 22×92: y0 h26 handle, y26/y42/y58 h16 links, y74 h18 tip |
| head and leg equip (`_Head`, `_Legs`) | 40×1120 — twenty 40×56 player frames, stacked |
| body equip (`_Body`) | **360×224 — a 9 × 4 grid** of 40×56, torso plus the separate arm pieces the 1.4.4 composite player draws. **Not** a vertical strip, and there is no `_Arms` file any more |

**Spears follow vanilla's convention or they sit at the wrong height.** `WedgeSpear` was
hand-rolled first — own hold-out maths, own rotation, `RotatedRelativePoint`, `gfxOffY` —
and it kept drawing low no matter what the offset was. The fix was to stop positioning it
ourselves. Three things do the work, all from vanilla:

- **`ItemID.Sets.Spears[Type] = true`** on the item. This is the one that matters: it makes
  vanilla drive the held projectile's rotation and the player's arm.
- **`Projectile.CloneDefaults(ProjectileID.Spear)`** for size, scale, `hide` and
  `ownerHitCheck`, then `PreAI` returning `false` and
  `Center = player.MountedCenter + Vector2.SmoothStep(dir * min, dir * max, progress)`.
  Plain `MountedCenter` — adding a grip offset on top of it is what pushed the spear low.
- **The sprite points up-LEFT**, butt in the bottom-right corner. The `+45°` / `+135°`
  rotations keyed off `spriteDirection` assume it. A sprite drawn pointing up-right aims
  correctly but lands off the aim line, and no offset fixes that cleanly.

Reference: `ExampleMod/Content/Projectiles/ExampleSpearProjectile.cs`. With `autoReuse`, a
held projectile also needs `CanUseItem` to check `ownedProjectileCounts < 1`, or holding the
button stacks spears.

**Player art is 1:1. Item icons are 2x2. Never mix them.** The Calamity grid belongs to the
inventory sprite only — equip sheets and wings sit on a vanilla player drawn at 1:1, and
blocking them out in 2x2 is what made the first Rift Wings read as a row of tiles instead of
feathers.

**A head equip sheet is one design stamped into twenty frames.** Twelve head items in this
mod once shared **three** silhouettes, because the sheets had been copied between tiers and
only recoloured — so on the player the helmet genuinely never changed, whatever you equipped.
Colour is not enough; each head needs its own geometry. The build is mechanical once the
design exists: author a 22x28 block (the 22x20 slot box plus four rows above it for a crest
or a peak), then write it into all twenty frames at `x = 10`, `y = frame*56 + 10 - TOP`, and
**subtract 2 more** on frames 7, 8, 9, 14, 15 and 16, which ride higher. `stampHead` in the
LibreSprite session does exactly this.

**Fill the whole box, and measure the reference instead of guessing at it.** The first
bespoke pass drew 16-wide caps that stopped at y21 and they read as squashed and perched on
top of the skull. `ExampleHelmet_Head` fills `x10..31 / y10..29` in **every** frame, and its
mask is this, in design rows (row 0 = y10):

| rows | x | what |
|---|---|---|
| 0–1 | 4–15 | crown |
| 2–3 | 2–17 | dome |
| 4–5 | 2–19 | dome, widening |
| 6–9 | 0–21 | **full width — the temple and eye line** |
| 10–15 | 0–9 | neck flap, **back half only** |
| 16–19 | 6–11 → 8–11 | tail |

`headShell` lays that footprint down; a crest may sit in the four rows above it. Face
features belong on rows 6–9, which is where a visor slit or a lens band reads as covering
the eyes. And the neck flap must be a mid tone with its own lit edge — at the dark end of
the ramp it reads as a shadow behind the head rather than as part of the helmet.

**A torso is repainted whole inside its box, never half-stamped.** `stampTorso` walks
`x12–29 / y26–41` on cells (0,0) (1,0) (0,2) (1,2), uses the existing sheet **only** for its
alpha silhouette, and authors every opaque pixel from its own rule. That is the allowed half
of the warning below — stamping a feature on while keeping someone else's shading is not.

**Wings took three wrong passes; here is what `ExampleWings_Wings` actually is.** 86x248,
four frames of 86x62, and the wing is **not** a fan radiating from a shoulder. Reading its
spread frame row by row: a membrane that starts narrow at the top centre, flares down *and*
outward, and ends in ragged fingers — laid out in **2x2 blocks even on the player**, which is
the one place the Calamity grid and the equip sheet agree. Measured extents per frame:

| frame | size | what |
|---|---|---|
| 0 | 18 x 34 | folded, tucked down the back |
| 1 | 36 x 40 | opening |
| 2 | 60 x 34 | full spread |
| 3 | 36 x 40 | closing, sitting lower |

**The span more than triples across the cycle.** That is the flap. A constant-width shape
that only moves up and down reads as a plank (pass one), and a radial fan with a bright rim
reads as two paddles (pass two). `wingSheet` takes explicit row spans per frame in a 43x31
art grid, mirrors them about the centre, and draws the spars as lines out of the shoulder —
single-pixel spars read as speckle.

**The armour is our own art, anchored to ExampleMod's slots.** The equip sheets are not
ExampleMod's pixels any more — that version was rejected as too plain and too obviously
theirs. What we keep from them is the *placement*, which is the part that has to be exact:

| slot | box | notes |
|---|---|---|
| head | `x10–31 / y10–29` | frames **7, 8, 9, 14, 15, 16** ride 2 px higher (`y8–27`) |
| torso | `x12–29 / y26–41` | only on cells **(0,0) (1,0) (0,2) (1,2)** of `_Body` |
| legs | `y42–53`, x varies with the stride | |

Every other cell of `_Body` is an **arm**: that is animation geometry, so those keep
ExampleMod's own shape and only get recoloured. Draw the helm, the cuirass and their robe
counterparts freely inside the boxes above — a crest or a hood peak may sit above the head
box, there is room in the 56 px frame.

Pull the reference with

```
curl -O https://raw.githubusercontent.com/tModLoader/tModLoader/1.4.4/ExampleMod/Content/Items/Armor/ExampleHelmet_Head.png
```

(`ExampleBreastplate_Body`, `ExampleLeggings_Legs` alongside it). `ExampleBreastplate_Body.png`
is an **indexed** PNG, so convert before reading it, and `ExampleHelmet_Head.png` is
**1118 px tall**, not 1120 — the last frame is short by two rows and that is fine.

**The legs need their sheet, not just its mask.** ExampleMod fills the gap between the legs
with dark pixels instead of leaving it transparent, so the split is *drawn*, not cut. Repaint
from the mask alone and the player gets one solid trouser-block. Re-apply their dark pixels
on top of your own shading: source luminance `< 0.20` → outline.

**Shade legs along each horizontal run, never by row.** Banding a leg by `y` — light at the
top, dark at the bottom — turns the walk cycle into a layer cake. Take each contiguous run
across the width: lit edge, body, dark edge, rim.

**The two sets must not look like the same object recoloured.** Fissurite is a soldier:
crested stone helm, a lit visor slit, pauldrons, bone collar and belt, a rift seam down the
sternum, stone greaves with knee plates. Glassblower is a mage: a pointed hood whose peak
sweeps back, a cowl of shadow with two points of light for eyes, a robe with a leather cord
belt and a glass amulet, and a hem that closes across the gap between the legs — that hem is
what stops the magic set reading as armour.

Do **not** try to stamp features (visor slits, collars, knee bands) onto ExampleMod's frames
from a bounding box while keeping their mask. It was tried: the bands run past the
silhouette, the accents float, and the arm cells collect chest chips. Either draw the whole
piece yourself inside the slot box, or recolour theirs — never half of each.

**A missing texture is a load error, not a build error, and `dotnet build` will not catch
it.** The compile and the packaging both succeed; the mod then fails at *Adding Content* with
`MissingResourceException` and tModLoader disables it. Two that are easy to forget because no
`.cs` line mentions them:

- **`[AutoloadBossHead]` needs `<Boss>_Head_Boss.png`** — the 32x32 map icon.
- **Every `ModBuff` needs its own icon**, even one that is never shown with a timer.

The check is mechanical, so run it rather than reading files: for each class deriving from
`ModItem` / `ModNPC` / `ModProjectile` / `ModBuff` / `ModTile` without an `override string
Texture`, a `.png` of the same name must sit beside the `.cs`; `[AutoloadEquip(EquipType.X)]`
needs `<Name>_X.png`; `[AutoloadBossHead]` needs `<Name>_Head_Boss.png`. Textures fetched by
string through `ModContent.Request` are invisible to that scan and need grepping for
separately.

**Glowmasks** are `<Texture>_Glow.png` next to the sprite, drawn in `PostDraw`. The boss's
loader code exists but `KeeperOfTheRift_Glow.png` has not been drawn yet, so the draw is
guarded by `ModContent.HasAsset(Texture + "_Glow")`.

**Additive draw passes.** `RiftLaser` and `RiftTear` end and restart the sprite batch to
draw glow layers additively:

```cs
Main.spriteBatch.End();
Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
// … additive draws …
Main.spriteBatch.End();
Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, /* same rest */);
```

Always restore `AlphaBlend` on both branches or the rest of the frame renders wrong.

Two silent ways an additive effect draws nothing at all, both cost a playtest on `Riftflow`:

- **`A = 0` means the opposite thing in the two batches, and mixing them up is silent.**
  Terraria's default batch is *premultiplied* `AlphaBlend`, so `new Color(r, g, b, 0)` there
  adds light — that is the cheap glow, and it is what `RiftShard`'s afterimages and everything
  in `RiftDraw` rely on. Inside an explicit `BlendState.Additive` batch the blend is
  `SourceAlpha`/`One`, and the same colour multiplies the whole layer away to nothing. There,
  write `new Color(r, g, b) * strength`, which is why every layer in `RiftLaser` and
  `RiftflowBeam` is a colour times a float.
- **`Projectile.hide = true` skips the draw loop entirely.** A hidden projectile is only
  drawn if the mod pushes it into a cache from `DrawBehind`, and until it does, `PreDraw` is
  never called once. `RiftLaser` sets `hide = false` explicitly for this reason.

**Glow is a progression signal, so do not give every projectile the same one.** `RiftDraw`
(`Common/RiftDraw.cs`) is the shared helper — `Trail`, `Head`, `Bloom`, `Ring`, `Glow` — and
its `Trail` takes the same three things Spirit Mod's does: a colour along the length, a width
along the length, and how far back it reaches, as `Func<float, …>` handed 1 at the head and 0
at the tail. It draws in whatever batch is already open, so it never breaks the sprite batch
and never leaves the wrong blend state behind. Spirit's own trails are vertex primitives
behind fifty-odd compiled `.fx` shaders — the API shape is worth borrowing, the renderer is
not, for a mod built out of hand-laid pixels.

What each weapon does with it has to differ, for the same reason thirteen tools must not share
one diagonal ramp:

| projectile | trail |
|---|---|
| `CasterShard` | grows and brightens with the wind-up, so the acceleration is legible |
| `RibArrow` | bone head lit by the world, rift tail; the mark landing gets its own dust ring |
| `GlassEdge` | tint slides down the glass ramp along the length instead of fading one colour |
| `BoneSplinter` | `lightColor`-tinted motion blur, **no glow** — bone is not energy |
| `CleftBolt` | falls off as `f²`, short and hard, so the three-way split stays readable |
| `WardenPlateMinion` | only while ramming; its bloom dims while the block is on cooldown |
| `GuardPlate` | always on, each seat breathing on its own phase |

`RiftDraw` also carries the two helpers the Weaver is built out of. **`Line`** is a flat
MagicPixel run between two world points — every aim telegraph in the mod. **`Silk`** is the
strand: a haze, a body and a white core down a segmented path, with three bright pulses
travelling along it on a phase you pass in. The pulses are the cheapest thing in it and they
do most of the work — a static line looks painted onto the background, a line with light
running down it looks like it is under tension. The sag is a real downward bias in *world*
space, not perpendicular to the strand, so a horizontal thread bellies and a vertical one
does not; that is the single detail that stops a thread reading as a laser.

T0 and T1 stone stays matte on purpose. If the first tier glows, reaching the rift tier
signals nothing.

**A hazard that is a line, not a projectile, needs a telegraph or it is unfair.** `RiftThread`
is harmless for its first 40 ticks — `Colliding` simply returns false — and visibly tightens
while it draws. Without that, a thread appearing across the arena kills whoever was already
standing on that line through no fault of their own. Endpoints are handles rather than
positions: `>= 0` is an NPC index, negative is a player, and `Pinned` means the fixed world
point in `Projectile.velocity`. Phase three needs the pinned form — a thread tied to the
player would sit on them and tick forever.

`ProjectileID.Sets.TrailCacheLength` only *fills* `oldPos`; nothing draws it for you.
`CasterShard` set the cache and drew no trail at all for a full tier. And `lightColor` is a
`ref` parameter, so it cannot be captured by the lambdas — copy it to a local first.

**A beam is layers plus motion, not a coloured quad.** One stretched rectangle reads as a
stick whatever colour it is. `RiftflowBeam` is the reference build, stacked the way vanilla's
Last Prism is: two haze layers a hair off axis, a **tiled** body, a flat body, a white core,
a churning muzzle bloom, and an impact bloom with rings on a recycling phase. The moving
layer is the one that matters, and it needs no new texture — begin the batch with
`SamplerState.PointWrap` and hand the draw a source rectangle **taller than the texture**
(`new Rectangle(0, -(int)(timer * 6) % tex.Height, tex.Width, (int)Length)`). The strip then
tiles down the beam and the taper already baked into `RiftLaser` reads as pulses running
outward. Scale is then `(width / tex.Width, 1f)` — the source height already carries the
length, so do not divide by it a second time.

**Readability rules the fight follows.** They were fixed in response to playtesting, so
keep them when adding attacks:

- Every attack has a **tell** — an eye flash, a windup, a telegraph line, a shrinking ring —
  before anything can hurt the player. `RiftLaser` is harmless for its first 46 ticks;
  `RiftTear` has no hitbox before frame 4.
- Overlapping beams were the main visual failure. `RiftLaser` starts **42 px out from the
  core** and its texture fades in over the first stretch, so three or four beams sharing an
  origin read as light instead of a white block.
- Shards launch **slow and accelerate** (`AccelDelay`, `MaxBoost` in `RiftShard.cs`) rather
  than spawning at full speed. Launch speeds in the boss sit around 4.6–5.8; do not raise
  them, raise `MaxBoost` if the fight needs more pressure.
- Plates keep advancing their orbit slot while thrown, so they return into formation. If
  you add a plate mode, advance `Angle` in it too.

**Never gate a boss's health bar behind adds.** The Weaver's phase two was
`dontTakeDamage = anchorsAlive > 0` with the anchors regrowing eight seconds after the last
one fell. Worked out at ~18 s of clearing for an 8 s window, thirteen times over: about six
minutes for one phase, and the playtest verdict was "очень скучный, однообразный". The fix
is a **dial, not a gate** — every standing anchor takes 10% off the damage the body receives
(`WeaverAnchor.Shelter`, floored at 30%), computed from a `CountAnchors()` scan every client
can run, so nothing needs syncing. Clearing the web is then a choice the player prices
themselves, and it is worth it because a dying anchor also **staggers** the boss: it drops
the attack, cuts the standing threads, and takes ×1.35 for 70 ticks. The reward is a window
you fight *for*, not a wall you wait out.

**Health has to be read against the vanilla boss either side of it, in master.** The Weaver
was authored at 52 000 from `CONTENT_PLAN.md` and landed at ~100 000 in master — double
Plantera, who comes immediately before it. The multiplier is what the spreadsheet misses:
`lifeMax × yourFactor × balance`, and master's `balance` runs near 2.75 for one player. Work
backwards from where the fight should sit, not forwards from the tier number. And give adds
their own `ApplyDifficultyAndPlayerScaling` — at the default scaling six anchors cost more
than the shelter they were worth and nobody would ever cut one.

**Nothing a patterned boss fires should home.** The other half of that playtest was "розовые
штуки вообще непредсказуемые" — `RiftOrb` lerps toward the player with no telegraph and
bursts into a cross of shards when killed, so shooting it is punished and ignoring it is
unreadable. It is out of the Weaver entirely. Volleys are flat fans of `RiftShard` laid on
the player's position at the instant of firing: the player leads the shot, which is a skill,
instead of watching a thing wander, which is not.

**A move needs a tell, a commit and a recovery — and the tell must be drawn, not implied.**
Every Weaver move telegraphs in one of three ways, and they are deliberately different so
they do not blur: an **eye flash** 20 ticks before a volley, a **drawn aim line**
(`RiftDraw.Line`, brightening as it runs out) 36–44 ticks before anything that dashes, and
the thread's own 40-tick tightening for anything made of silk. Attack order is a fixed
per-phase rotation, never random — a boss you can learn.

**One projectile per shape that moves.** `WebCollapse` is a twelve-sided ring rather than
twelve `RiftThread`s because the ring contracts and rotates, and a thread is pinned by
definition. Owning all twelve sides is also what lets the door be *designed*: exactly two
sides, drawn from the first frame, with a lit post at each end of it.

**Every set has a class, and no set has all of them.** The first pass gave Fissurite,
Dustseeker and Darner `DamageClass.Generic`, which made each of them the best armour in the
mod for every build at once and left the player nothing to pick. The current split:

| tier | set | class | heads |
|---|---|---|---|
| T0 | Fissurite | melee | 1 |
| T1 | Glassblower | magic | 1 |
| T2 | Dustseeker | ranged | 1 |
| T3 | Riftsteel | all four | Helm / Visor / Hood / Crown |
| T4 | Darner | all four | Mask / Visor / Hood / Cowl |

Defense totals follow `CONTENT_PLAN.md` exactly: 10 / 14 / 20 / 30 / 44. T1, T2 and T3 were
all off their targets and have been corrected.

**Two adjacent tiers must differ in kind, not by one notch.** The Weaver set was authored as
Darner **+1 on every single line** — same stat shape, same silhouette, same ramp — and three
of its four heads carried a *literally identical* set-bonus string. Check a new tier against
the one below it on all three axes:

| | Darner (T4) | Weaver (T5) |
|---|---|---|
| what it spends on | crit and speed — the sharp set | raw damage, defence, life, utility. **No crit anywhere** |
| defence | 44 | 56, and body-heavy (28 of it in the chasuble) |
| mechanic | the **Stitch**: joins two *enemies* and waits for you to hit one | the **Loom**: plants anchors in *space* and strings your own thread between them |
| art | dark machine, thread as lights | pale garment, dark frame |

The mechanics are the load-bearing half. `TerrapexPlayer.Weave` takes every hit and plants an
anchor every second or third one; when the frame is full it strings `FriendlyThread` between
the points. Two anchors make a line, and the magic head's third makes a triangle. It is the
boss's own mechanic handed to the player, and it is about the arena where the Stitch is about
targets — so wearing one after the other does not feel like wearing the same thing twice.

Per-head twists rewrite the loom rather than restating it: melee weaves every **second** hit
and cuts half again as hard, ranged's threads stand **twice as long**, magic holds **three**
anchors, and summon lets **minions** work the loom. The legs took over the flat
"threads last 1.5x" line (`weaverTreads`, not `weaverSet`), which is what freed the four
heads to say four different things.

**A set with four heads shares one bonus and four twists.** Riftsteel's four heads all grant
the same shell; Darner's four all grant the same +20% against a stitched target, and then each
rewrites what the thread is *for* — melee always crits a stitched target, ranged ricochets
down the thread to the other end, magic pays for the stitching in mana, summon adds 25% again
for minions. That is what makes a class choice change how the tier plays rather than only
which number goes up. Per-head strings are `SetBonus.Darner{Melee,Ranged,Magic,Summon}`.

**T4 is one mechanic with three delivery systems.** The Seam (melee), the Threadcaster
(ranged) and the Stitch (magic) all read and write the *same* remembered target, held on
`TerrapexPlayer.seamTarget` rather than on any item. Swapping weapons mid-fight therefore
finishes a stitch instead of starting over, because the thread belongs to the player, not to
the weapon. `BoundGlobalNPC.Bind` always makes a **pair**, never a chain — it cuts both ends
first — and `Pass` guards on a static `echoing` flag, or two bound targets echo into each
other forever. The tell is the drawn thread, never a debuff icon: an icon in the corner
cannot say *which two* things are joined.

The Seam's own beat is the **third** swing: hitting something already stitched calls
`BoundGlobalNPC.Yank`, which drags the pair into each other, damages both, and burns the
thread. Stitch, stitch, pull. Binding on its own was bookkeeping — the pull is the part the
player aims. `Yank` cuts the thread deliberately so the loop repeats instead of paying out
free echo damage forever.

The Rift Scythe is the tier's crowd weapon and is paid in bodies. Its arc flies out, **turns,
and comes back through the same pack**, and every enemy either leg touches banks a reap stack
on `TerrapexPlayer.reaped` (max 8, +6% each, lapsing after four seconds). Stacks make the next
swing hit harder and the next arc fly wider. It is deliberately poor against a single target:
a drop-only weapon that merely repeats the three crafted ones is a wasted slot.

Its outbound leg decays at **0.982**, not 0.955: the first tuning spent four fifths of the
arc's speed before the turn, so it died almost on top of the player. At `shootSpeed = 13`
and `TurnAt = 38` it now reaches about 360 px — roughly 22 tiles — before homing back.

**A scythe hangs its blade DOWN off the neck, and it took five passes to get there.** A
chunky wedge read as a hook; a blade the same width as the shaft read as a hockey stick; and
a broad blade that curled *upward* off the top of the haft was still a hockey stick, because
up-and-right is a stick shape no matter how wide the end is. What works: a long thin dark
haft, and a broad bright blade that comes out of the neck, sweeps right and then **drops
below it** to a point, with the concave cutting edge facing back toward the wielder and the
heavy convex back on the outside. Curve that back too — a flat outer edge turns the blade
into a brick — and draw the rib as a line following the curve, never as scattered pixels.

## Art pipeline

**All sprites are drawn in LibreSprite.** Python is for inspection only — contact
sheets, GIF export, sheet assembly — and never paints a shipping pixel.

**LibreSprite via MCP** (`libresprite-mcp`) is the tool. Start it with
`uvx --with "mcp[cli]<2" libresprite-mcp` — the package pins `mcp>=1.12.2` with no upper
bound and mcp 2.x renamed FastMCP, so a bare `uvx libresprite-mcp` dies with
`ModuleNotFoundError: No module named 'mcp.server.fastmcp'`.

**The colour API is `app.pixelColor`, not a global `pixelColor`.** Aseprite's global does
not exist in this build — `pixelColor.rgba(...)` dies with `pixelColor is not defined`.
`app.activeSprite`, `app.activeImage` and `app.command` are likewise only on `app`, and a
`for…in` over `app` lists none of them, so probe with `typeof` instead of enumerating.

**Never call `app.activeDocument.close()` in a script.** Closing a document LibreSprite
considers dirty pops a "save changes?" modal, and a modal blocks the scripting bridge for
the rest of the session — every later call queues forever, including a bare `console.log`.
Leave the tabs open. If the bridge does hang, restarting the MCP server is not enough:
LibreSprite keeps the stale sockets, so **restart LibreSprite first, then reconnect the
server**, in that order.

Inside a `run_script` call, **never touch `app.command.NewFile()`** — it blocks on a modal
dialog and hangs the script forever. Scripts `app.open()` a PNG that already exists,
repaint it and `saveAs` over it, so **to add a texture, create the empty PNG at its final
size first** (Python, one line). Changing a texture's size means a new blank, not a resize.

**The PNGs under `Content/` are the source of truth. There is no checked-in generator.**
An earlier `Art/libresprite/t0.js` tried to be one and was deleted: it encoded a generic
shader that no longer matches how the art is drawn, and a stale generator that claims to
reproduce the art is worse than none. Sprites are blocked out by hand — explicit row spans
and single pixels — not derived from a formula.

**Python + PIL** for inspection only, plus the two older parametric projectile sheets:

```
uv run --with pillow --python 3.12 python Art/generators/preview_t0.py
```

`preview_t0.py` builds the T0 contact sheets in `Art/preview/` (items, tiles laid edge to
edge, and the equip sheets composited into a player). `gen_shard24.py` renders the 8-frame
rotating shard and `gen_beam.py` the beam strip, flare and ring — these predate the
LibreSprite pipeline and are the last two generators that still paint.

Four mistakes this pipeline has already made, all silent — the script reports success and
the result is wrong:

- **Indexed PNGs read as zero alpha.** `app.open()` on a paletted PNG gives `colorMode 2`,
  and `pixelColor.rgbaA(getPixel(...))` then returns garbage. A whole equip sheet came out
  empty this way. Check `sprite.colorMode` and convert first:
  `app.command.setParameter("format","rgb"); app.command.ChangePixelFormat();`
- **`sprite.crop()` does not survive `saveAs`.** It succeeded and the file kept its old
  size. Crop in Python, or draw into a correctly sized blank.
- A capsule thinner than **half a pixel misses every pixel centre and disappears** — the
  bow string vanished at `hw = 0.35`. Put thin lines on an `x.5` centre with `hw ≥ 0.55`.
- Walking a 45° shape by stepping along the diagonal and painting offsets leaves
  **corner-touching gaps** — a checkerboard blade. Rasterise the whole region instead.

Blocks get **no outline**: an outlined tile draws a grid across the whole cavern.

### The style

Every shipping texture now uses one palette. It was approved on `DormantEyeBag` and then
applied to the whole mod; the older, greyer ramps below it are history, not a target.

| ramp | colours | where |
|---|---|---|
| stone | `#f4efe6 #c9bdd1 #93849f #635478 #3f3357 #281e3c #1f1930` | structural stone, armour plate, helms |
| dense stone | `#baacc4 #847696 #584a6e #3b3050` | hammers, shields, plates — the heavier grade |
| bone | `#fff0c8 #e8cf94 #c0a066 #8a6b45 #5a4030` | fittings, rims, ties, sclera |
| wood / leather | `#fff3d6 #f0c07a #d18a4a #9a5140 #5e2f45 #3a1c33` | hafts, straps, plaques |
| glass (T1) | `#eaf4ff #b9c6ee #8a92d0 #5d5c9e #3a3466` | riftglass, glassblower cloth |
| rift | `#ffffff #fff2fb #ff9ad9 #d94ecb #8a2f9c #5e1a72` | rift energy only |
| bounce | `#b56a5e` (wood) `#8a6a86` (stone) | the lit underside of a bottom edge |
| outline | `#241733` | hard 8-neighbour outline |

T4 adds its own two ramps, because the plan's colour line turns at this tier — turquoise
thread on black:

| ramp | colours | where |
|---|---|---|
| thread | `#d8fff7 #7ff2e0 #35c9b8 #1c7d78` | thread, seams, edges — the only saturated thing |
| void body | `#9fb4bb #61818c #3d5560 #22343e #0f1c26` | every T4 body: hafts, receivers, housings |

T5 inverts T4 rather than extending it, which is the only reason the two tiers read as
different armour rather than as one set recoloured:

| ramp | colours | where |
|---|---|---|
| silk | `#eef9f6 #c2e0da #93b8b6 #648d90 #3d6068` | every T5 garment: helms, chasuble, treads |
| frame | `#22343e #0a1620` | the cowl shadow and the outline under it |

T6 drops hue entirely, which is the colour line's own destination — the crack came from
something that predates the violet the rest of the mod is painted in:

| ramp | colours | where |
|---|---|---|
| prime | `#ffffff #eef1f8 #c8cee0 #9aa2bc #666e8c #3b4260 #22263c` | every T6 surface: shell, cloth, blade, bar |
| prime outline | `#0d0f1c` | a harder near-black than the `#241733` the tiers below use |
| regard | `#ffd79a #ff9f4a` | warm, and only ever a few pixels: an iris ring, a visor slit, a stud |

The warm accent is not decoration — it is the same colour the boss's own halo uses when it
has you in the cone, so the tier's one saturated tone means *being seen* everywhere it
appears. It is the only hue in the tier, which is why it can carry a meaning at all.

**Darner is a dark machine with thread for lights; the Weaver is a pale garment with a dark
frame.** Same family, opposite value structure. Before this, the two sheets were **99%
pixel-identical** — the Weaver was the Darner mask with the grey ramp swapped for the
turquoise one, which also put 2 420 px of `#35c9b8` into a helmet and broke the rule below.

The split is not decoration. The first pass painted whole weapons out of the dark end and
they vanished: a near-black machine on a cavern background is a silhouette nobody can see.
Bodies live in the desaturated ramp with real mid-tones, and the turquoise is reserved for
the part that is literally thread.

Shadows hue-shift toward blue-violet, lit ends toward warm — a ramp that only darkens is
what the earlier passes got wrong.

Two failure modes the user called out by name, both from generating a family at once:

- **Every haft the same striped bar.** Light a handle *across* its width — lit edge, body,
  dark edge — and put the only lengthwise detail in two or three grain ticks whose
  positions come from a per-item seed. Never a `%2` band.
- **Every head one colour on one diagonal ramp.** A shared `((x-x0)+(y-y0))/span` gradient
  makes thirteen tools look like one tool. Light each head by its own geometry (a bar by
  its top edge, a wedge by its bit frame, a block as three faces with hard breaks) and
  spread the family across the three stone grades.

Tiles get the same treatment but must not band: shading a 16×16 frame by `y` puts a
visible stripe every 16 px across a whole cavern wall. Use a flat base with clumped
patches, and let the ore veins be the only loud thing.

### Reading like a Calamity weapon

**Calamity draws at half resolution and doubles it.** Every one of the sixteen reference
sprites pulled to `Art/reference/calamity/` is **100% 2x2 blocks on the (0,0) grid** — a
measurable fact, not an impression. Their Arbalest is 82x34 pixels but only **41x17 art
pixels**, which is *fewer* than a 40x34 sprite of ours. That single property is the style;
everything else follows from it.

Two wrong theories were chased first, and both made the art worse:

- *"The style is thinness."* It is not. Slimming the weapons only made them poorer.
- *"The style is detail density, so the canvas is the budget."* Backwards. The chunky grid
  is what makes a Calamity sprite look big and bold, and it **halves** the detail a sprite
  can hold. Adding fine detail is the opposite of the target.

So T2's weapons are authored in an art-space buffer at half size — 28x28, 36x18, 18x21,
23x23, 22x22 — outlined **in that buffer**, and only then blitted as 2x2 blocks. Outlining
after the blit gives a one-pixel outline around a two-pixel grid and breaks the look. Check
any new sprite by measuring the share of aligned 2x2 blocks that are a single colour; it
must be 100%.

T3 was drawn the same way, and two things about that buffer are easy to get wrong:

- **Leave two empty art cells between separate parts, not one.** The rim is 4-neighbour, so
  a single-cell gap gets filled from both sides and the parts weld together. The Carapace
  Charm's ring of four plates came out as a barbell for exactly this reason and had to be
  redrawn as stacked layers.
- **A tile object is authored whole and scattered afterwards.** Terraria frames a 3x3 object
  on an **18 px pitch**, so drawing straight into the sheet means every shape jumps 2 px at
  each cell edge. Build the logical image — art buffer, selout, 2x2 blit — at `cols*16` by
  `rows*16`, then place logical pixel `(lx, ly)` at `((lx/16)*18 + lx%16, (ly/16)*18 + ly%16)`.
  `KeeperTrophyTile` (24x24 art) and `RiftlingBannerTile` (8x24) are both built this way.

And one value trap: **a near-black bottom edge on a dark background reads as prongs, not as
a body.** The Warden Plate's underside was `#171029` and the only thing visible was its own
rim, so the plate looked like it had legs. Bottom out plate silhouettes at `#584a6e`.

Within that grid, three things do carry over from their palettes:

- **A large near-black mass.** 58% of Arbalest's pixels are one near-black. `#171029` is the
  mod's mass colour, distinct from the `#241733` outline.
- **Saturated accent, a few pixels at a time.** Arbalest carries pure red, green, blue and
  magenta at S=1.00, twelve to twenty-four pixels each, reading as lights on a machine.
  `#ff2fb0` is the mod's hot accent: a cell, a node, a bead, nothing more.
- Colour *count* is not the difference. Theirs run 7-31; ours already ran 12-16.

Two shapes that cost several passes on the crossbow:

- **A curve that moves faster across than down still breaks up when scanned by row.** The
  limbs came out as dotted zigzags twice. Rasterise a limb as a *region* in (u,w) space, the
  same way the blades are drawn, or give it an explicit row-span table.
- **A string parallel to the prod is invisible.** Two lines at the same angle read as one
  thing. The prod ended up near-vertical with the string a shallow V back to a nock inside
  the receiver, so the angles differ enough to separate.

**Repainting existing art.** For sheets whose geometry is already right (equip sheets, the
Keeper set), remap colours instead of redrawing: bucket each source colour by luminance
onto the ramp above, and pick the ramp by hue. The hue test has to be strict —
`sat > 0.42 && r > g*1.35 && b > g*1.12` for rift — or the Keeper's desaturated violet
stone body reads as energy and the whole boss turns magenta.

The **warm** branch of that remap is the other half of the trap and points the opposite way.
Every Darner head came out a tan scout dome because the branch was still routing Dustseeker's
leather to the bone ramp — right for a T2 scout, wrong for a T4 cloth head, where nothing
should stay warm at all. When a recolour crosses tiers, decide per sheet whether warm pixels
are *fittings* (keep bone) or *the garment itself* (send them to the target ramp); passing the
target ramp in as the bone ramp is how you say the latter.

Item sprites for swung and held weapons are authored **as they look when the player faces
right** — tip up and to the right, grip bottom-left. The Rift Scythe was drawn with its blade
fanning up-*left* and read backwards in the hand; the fix was to keep the haft running
bottom-left to top-right and sweep the blade off the top of it to the right. `Seam` and
`RiftshardCleaver` are the reference for the angle.

The first T0 passes were rejected as "Minecraft, not Terraria". The causes, all fixable
and all worth avoiding again:

The first T0 passes were rejected as "Minecraft, not Terraria". The causes, all fixable
and all worth avoiding again:

- **Pillow shading.** Shading by distance-to-silhouette lights every shape from its own
  middle outwards. It is the single biggest tell of generated art. Light objects by an
  authored rule in their **own** space instead — a blade banded across its blade axis, a
  dome by its own normal, a plate top-to-bottom.
- **Soft ramps plus dithering.** Six interpolated steps with Bayer noise turns to mush at
  16–40 px. Quantise hard into 3–5 tones, no dithering.
- **No dark end and no warm accent.** Everything sat in grey-lavender mid-tones. The value
  range has to reach near-black, and bone is what keeps it from going monochrome purple.
- **Tiles as uniform noise.** That is literally Minecraft stone. Use a flat dark base with
  a few clumped blots, and let the ore be the only loud thing in the block.

Techniques worth reusing:

- **45° parametric drawing**: `u = (dx-dy)*SQRT1_2` along the blade, `w = (dx+dy)*SQRT1_2`
  across, with a `hw(u)` half-width profile. Gives clean diagonal pixel edges.
- **Outlines**: T0 uses a hard 8-neighbour near-black (`#141024`), which is what the
  cleaver reads as. The older selout — 1 px 4-neighbour tinted from the fill it touches —
  is still what the boss and projectile sheets use; do not mix the two inside one tier.
- **Crystals need their own rim.** A gem drawn inside another shape gets no silhouette, so
  it reads as a flat sticker unless you ring it in the outline colour yourself.
- Eyes read only with a **large bright iris, a small round pupil and a pale sclera ring**.
  This has been the cause of every "the eye is unreadable" iteration.

**A dial the player can turn BOTH ways is the last thing an add can be.** The Keeper's plates
were a gate, and the Weaver's anchors were a dial that only ever pointed one way — cutting was
always right, so the "choice" was really a chore with a delay on it. The First Keeper's lids
narrow its cone of sight: cutting one opens the eye, which is *more* damage dealt and *more*
damage taken, and lids grow back so the player can close it again by simply not cutting for
half a minute. That is the first version of the mechanic where standing there with the adds
alive is a real answer. Check any new add against it: if there is never a reason to leave one
standing, it is not a dial.

**The damage multiplier goes on the attacker, not on the boss.** `ModifyHitByItem` and
`ModifyHitByProjectile` both hand you the player; `ModifyIncomingHit` does not. A conditional
like "double damage while it is looking at you" computed once for the NPC is wrong the moment
a second player is in the arena — one of them hides and the other collects the bonus. Per
attacker is the only version that survives multiplayer, and it costs nothing.

**A ray may sweep or it may track. Never both.** `GazeRay` guards this explicitly: it turns
toward the player only when its spin is zero. A beam that sweeps on a fixed rate *and* creeps
toward you is a homing attack wearing a pattern's clothes, and it fails the rule the Weaver's
playtest bought — nothing a patterned boss fires homes. One move in the whole fight follows,
it is announced as the one that does, and its turn rate is slow enough to outrun.

**`MinionContactDamage() => false` switches off the minion's whole damage pass, not just its
contact damage.** `KeeperStaff` shipped dealing zero: its minion's weapon is a line checked in
`Colliding`, and returning false there meant tModLoader never called `Colliding` at all, so the
beam was decoration. The rule in both directions:

- a minion with a **hitbox of its own** (even a strange one) must return **true** and then
  narrow the hitbox inside `Colliding` — `WardenPlateMinion` and `SpindleMinion` do this;
- a minion that returns **false** must deal its damage through a **separate projectile** —
  `SailclothMinion` does this with its thread.

And `Colliding` is called once per NPC per tick, so anything expensive inside it is a
two-hundred-times-a-tick scan. Settle the aim once in `AI` and cache it in a field.

**A glowmask is picked from the sheet's own palette by name, never by a hue or saturation
test.** The mod's ramps overlap: the Keeper's body is desaturated violet *stone* and its eye is
violet *rift*, and any threshold loose enough to catch the eye turns the whole boss magenta.
Enumerate the exact colours instead.

One of them also needs a **region**, not just a colour. The Weaver's `#35c9b8` is 352 px of
light bars inside the head panel and four thousand px of dome everywhere else; picked by colour
alone the boss lit up like a lantern. That one is scoped to the panel box and the two other
tones are global. Sample the sheet before writing the mask — the palettes are 9-20 colours and
the answer is always visible in the counts.

**Every boss dies through `BossDeath`.** Vanilla removes an NPC on the frame its health hits
zero, which throws away the moment the fight was built toward. The helper holds it at one hit
point, takes its attacks away, and spends 110-170 ticks coming apart. Two things it must do,
both learned the hard way elsewhere in this file: `onBegin` drops the adds and standing hazards
(a web left up keeps killing through the part of the fight that is over), and `AI` returns
immediately while `Dying` so no phase check or attack runs. The timing is shared on purpose —
four bosses whose deaths land on different rhythms read as four different mods — and only the
dust and the light colour differ.

**`AnimationType` borrows the whole frame count, not just the walk.** Setting
`AnimationType = NPCID.Zombie` next to `Main.npcFrameCount[Type] = 4` makes the game drive
`frame.Y` up to the zombie's sixteenth frame on a four-frame sheet, and it reads straight past
the end of the texture. Either match the borrowed NPC's frame count exactly or drop
`AnimationType` and write `FindFrame` yourself — `aiStyle` and `AIType` still do all the
walking, which is the part actually worth borrowing.

**A mob that splits needs a second NPC type, not a flag in `ai`.** Loot rules are registered
per type, so a flagged fragment drops the parent's whole table — three times over, on a mob
that splits into three. `FissureSlimelet` exists only for that reason and borrows the parent's
sheet through `override string Texture`, so the second type costs no art.

## The wiki

`Wiki/` builds a bilingual static site (English at the root, Russian under `/ru/`)
out of `Content/`, `Localization/` and two authored tables. Nothing on it is
written twice: item stats come from `SetDefaults`, recipes from `AddRecipes`,
drops from `ModifyNPCLoot`, names and tooltips from the hjson. 203 pages per
language, published to GitHub Pages by `.github/workflows/wiki.yml`.

```
uv run --with pillow --python 3.12 python Wiki/generate.py
```

**Adding content costs two lines.** A new item, NPC or buff is picked up
automatically, but the generator prints `! no tier assigned` until it appears in
`Wiki/data/tiers.json`, and `! N missing translations` until it appears in
`Wiki/data/ru.json`. Both are warnings — the page still builds, falling back to
English and to no tier badge.

The rest — why the URLs are flat, what the drop parser does and does not
understand, how to override a generated page with a hand-written one — is in
`Wiki/README.md`.

## Progression

The Keeper is the mod's first hardmode boss. `FracturedEye` costs 25 Stone Block +
3 Lens + 5 Soul of Night at a **Mythril Anvil**, which gates it to post-Wall-of-Flesh,
after the second hardmode ore tier, before the mechanical bosses.

**The tuning debt is closed.** `RiftshardCleaver` is **52** damage and the Keeper has
**28 000** HP, which puts the sword between Night's Edge (42) and Excalibur (66) and keeps
the fight where it was designed to sit — post-Wall, the emptiest stretch of vanilla
progression. The `FracturedEye` recipe is unchanged: the Mythril Anvil and Souls of Night
already gate it correctly. See `CONTENT_PLAN.md` for the tier damage targets.

## Still missing

- **T2-T6 have been played through once** and the tier is where it should be. What that pass
  has not covered is multiplayer, and every "per attacker" rule in T6 exists precisely for the
  case a single-player run cannot exercise.
- **The leg sheets are still one silhouette in seven colours.** All seven `*_Legs.png` are
  **100% pixel-identical in alpha**; only the paint differs. Two attempts to fix it in a polish
  pass both came out worse and were reverted, and the reasons are worth keeping:

  - *Repaint the interior by runs and restore the split from the dark pixels.* The sheets have
    already been run-shaded once, so their dark tones sit along the right quarter of **every**
    row, not only in the gap between the legs. The "interior dark pixel" test caught all of it
    and shredded the legs into vertical stripes.
  - *Leave the interior alone and only grow or shrink each row's run per tier.* Safe, but the
    silhouettes still measured 98-99% identical — 1-2 px does not register at this size — and
    the shrink chewed ragged notches into the Darner boots while its widened near-black sole
    read as a slab lying under the foot rather than as a sole.

  What it actually needs is authored per-tier leg geometry, which means re-deriving the walk
  cycle against `ExampleLeggings_Legs` rather than editing the existing one. That is its own
  session, not a polish item. Backups of the current sheets are in `Art/archive/legs_before/`.
- **T0 armour on the player has never been seen in game.** The geometry is no longer a
  guess — every cell is ExampleMod's own, re-valued — so the fit should be exact. Still
  worth equipping a set, walking and swinging once to confirm.
- Music. Every boss is on a vanilla track; this one needs audio files, which are not
  something the pipeline here can author.
