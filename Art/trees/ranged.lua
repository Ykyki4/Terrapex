-- The ranged tree of CRAFTING_TREES.md: Wayfinder -> Compass Rose -> Trade Wind -> Horizon,
-- plus the four wind arrows, the spray pellet and the frost icicle. Run from the repo root:
--
--   aseprite -b --script-param root=<repo> --script Art/trees/ranged.lua
--
-- Hand-drawn half-size maps, outlined in the art buffer, blitted 2x2. Every bow is drawn as it
-- looks facing right: string on the left, limbs bowing out to the right.
--
-- What each node carries, so the tree reads on the sprite:
--   Wayfinder   - assembled, not designed: the Sunwing's gilded limb with its blue wing-case on
--                 top, the Hoarfrost's iced limb below, the Blunderbuss's barnacled stock and
--                 iron bands as the grip, the Cleft's rift crystal set in the arrow rest.
--   CompassRose - T4 void with thread: the Rib's bone spurs along the inside of both limbs, and
--                 an eight-point compass star for a riser with the four winds' gems at N/E/S/W.
--   TradeWind   - T5 silk: the limbs carry sails, filled and bellying out - the silhouette is
--                 the wind. The compass stays, re-cut in silk, with the Shotbow's green at the
--                 centre and the Weft's turquoise thread at the diagonals.
--   Horizon     - T6 prime: a doubled limb (the Phantasm's), and the riser is the Unblinking's
--                 eye with sun rays off it - the horizon at sunrise. Warm only in the iris, the
--                 rays and the tip studs.

dofile(app.params.root .. "/Art/reaper/lib.lua")

local DIR = "Content/Items/Weapons/Trees/Ranged/"
local PDIR = "Content/Projectiles/Trees/Ranged/"

local function draw(rows, pal, name)
  local g = newGrid(1, 1)
  stamp(g, rows, pal, 0, 0)
  outline(g)
  save(g, DIR .. name .. ".png")
end

-- the lower limb is the upper one turned over; the riser sits between
local function mirrored(upper, riser)
  local out = {}
  for _, r in ipairs(upper) do out[#out + 1] = r end
  for _, r in ipairs(riser) do out[#out + 1] = r end
  for i = #upper, 1, -1 do out[#out + 1] = upper[i] end
  return out
end

-- a fixed-size vertical sheet: frames are not cropped, so every frame keeps the same box
local function sheet(frames, pal, fw, fh, path)
  local img = Image(fw * 2, fh * 2 * #frames, ColorMode.RGB)
  for f, rows in ipairs(frames) do
    local g = newGrid(fw, fh)
    stamp(g, rows, pal, 0, 0)
    outline(g)
    for _, c in ipairs(cells(g)) do
      if c[1] >= 0 and c[1] < fw and c[2] >= 0 and c[2] < fh then
        for sy = 0, 1 do
          for sx = 0, 1 do img:drawPixel(c[1] * 2 + sx, ((f - 1) * fh + c[2]) * 2 + sy, c[3]) end
        end
      end
    end
  end
  local spr = Sprite(img.width, img.height, ColorMode.RGB)
  spr.cels[1].image = img
  spr:saveAs(root .. "/" .. path)
end

-------------------------------------------------------------------------------
-- Wayfinder: sun limb over a barnacled stock over a frost limb, rift in the rest
draw({
  "...dG...........",
  "..SogG..........",
  "..S.ogG.........",
  "..S..ogG........",
  "..S..oggG.......",
  "..S...obgG......",
  "..S...obBgG.....",
  "..S....obBgG....",
  "..S....obBBG....",
  "..S.....obBgG...",
  "..S.....oogGG...",
  "..S......dggG...",
  "..S......lLLLl..",
  "..S.....rKKKkkn.",
  "..S....RrKkkkqNn",
  "..S....RpKkkkqnm",
  "..S.....rKkkqtn.",
  "..S......lLLLl..",
  "..S......vwwWI..",
  "..S.....vvwWWIi.",
  "..S.....vwwWW...",
  "..S....vwwWI....",
  "..S....vwwWIi...",
  "..S...vwwWW.....",
  "..S...vwwW......",
  "..S..vwwWIi.....",
  "..S..vwWI.......",
  "..S.vwW.........",
  "..SvwI..........",
  "...jI...........",
}, {
  S = hex("f4efe6"),
  G = hex("fff1a8"), g = hex("f5c04a"), o = hex("c7802a"), d = hex("7a3e1e"),
  B = hex("8ab4ff"), b = hex("3a5ac0"),
  W = hex("e8cf94"), w = hex("c0a066"), v = hex("8a6b45"),
  I = hex("eaffff"), i = hex("8ae4f8"), j = hex("3a8ac0"),
  K = hex("d18a4a"), k = hex("9a5140"), q = hex("5e2f45"),
  L = hex("d9d4de"), l = hex("7a7488"),
  N = hex("f4efe6"), n = hex("c9bdd1"), m = hex("635478"), t = hex("2f8aa0"),
  R = hex("ff9ad9"), r = hex("d94ecb"), p = hex("8a2f9c"),
}, "Wayfinder")

-------------------------------------------------------------------------------
-- Compass Rose: void limbs with bone spurs inside, thread wraps, a compass-star riser
draw(mirrored({
  "...EA.............",
  "..SeaA............",
  "..S.TtH...........",
  "..S..eaA..........",
  "..S..eaaA.........",
  "..S...eaAA........",
  "..S...eaaA........",
  "..S..xXeaaA.......",
  "..S....eaaAA......",
  "..S.....eaaA......",
  "..S...xXXeaaA.....",
  "..S......eaaAA....",
  "..S......TtTHH....",
  "..S.......eaaAA...",
}, {
  "..S........EgE....",
  "..S......TEeHeET..",
  "..S......EeHHHeE..",
  "..S......RHHPHHb..",
  "..S......EeHHHeE..",
  "..S......TEeHeET..",
  "..S........EiE....",
}), {
  S = hex("7ff2e0"),
  A = hex("9fb4bb"), a = hex("61818c"), e = hex("3d5560"), E = hex("22343e"),
  T = hex("35c9b8"), t = hex("1c7d78"), H = hex("d8fff7"),
  X = hex("fff0c8"), x = hex("c0a066"),
  P = hex("ffffff"),
  g = hex("f5c04a"), b = hex("4aa8e0"), i = hex("eaffff"), R = hex("ff9ad9"),
}, "CompassRose")

-------------------------------------------------------------------------------
-- Trade Wind: silk limbs with filled sails bellying out behind them
draw(mirrored({
  "...Fs...............",
  "..SFsL..............",
  "..S.FslL............",
  "..S.FslzL...........",
  "..S..FslllL.........",
  "..S..FslzllL........",
  "..S...FslzllL.......",
  "..S...FsllzllL......",
  "..S....FslllzlL.....",
  "..S....FsllllzL.....",
  "..S.....FsllllzL....",
  "..S.....FslllllL....",
  "..S......FsllllL....",
  "..S......FslllL.....",
  "..S.......FsllL.....",
  "..S.......FslL......",
}, {
  "..S.........FLF.....",
  "..S.......TFlslFT...",
  "..S.......FFlllFF...",
  "..S.......LllCllL...",
  "..S.......FFlllFF...",
  "..S.......TFlslFT...",
  "..S.........FLF.....",
}), {
  S = hex("d8fff7"),
  L = hex("eef9f6"), l = hex("c2e0da"), s = hex("93b8b6"), z = hex("648d90"),
  F = hex("3d6068"),
  T = hex("35c9b8"),
  C = hex("8ae05a"),
}, "TradeWind")

-------------------------------------------------------------------------------
-- Horizon: a doubled prime limb, and the Unblinking's eye rising with rays off it
local savedOutline = OUTLINE
OUTLINE = hex("0d0f1c")
draw(mirrored({
  "...WA.................",
  "..SFCA................",
  "..S.FCA...............",
  "..S.FCAG..............",
  "..S..FCAGD............",
  "..S..FCCAGD...........",
  "..S...FCAGD...........",
  "..S...FCCAGD..........",
  "..S....FCAGD..........",
  "..S....FCCAGD.........",
  "..S.....FCAGD.........",
  "..S.....FCCAGD........",
  "..S......FCAGD........",
  "..S......FCCAGD.......",
  "..S.......FCAGD.......",
  "..S.......FCCAGW......",
  "..S........FCAG.......",
  "..S........FCCA.......",
}, {
  "..S.........FDDDF.....",
  "..S........FCBBBCF.W..",
  "..S.......FCBWWWBCFw..",
  "..S.......DBWwGwWBDD..",
  "..S.......DBWGAGWBDwWA",
  "..S.......DBWwGwWBDD..",
  "..S.......FCBWWWBCFw..",
  "..S........FCBBBCF.W..",
  "..S.........FDDDF.....",
}), {
  S = hex("eef1f8"),
  A = hex("ffffff"), B = hex("eef1f8"), C = hex("c8cee0"), D = hex("9aa2bc"),
  E = hex("666e8c"), F = hex("3b4260"), G = hex("22263c"),
  W = hex("ffd79a"), w = hex("ff9f4a"),
}, "Horizon")
OUTLINE = savedOutline

-------------------------------------------------------------------------------
-- the four winds: one 7x16 art frame each, stacked - spray, sun, frost, cleft
sheet({
  { -- spray: a shell-chip head, driftwood shaft, sea-teal fletching
    ".......",
    "...N...",
    "..NNn..",
    "..nnm..",
    "...m...",
    "...K...",
    "...k...",
    "...K...",
    "...k...",
    "...K...",
    "..tkt..",
    ".tTkTt.",
    ".t.k.t.",
    "...k...",
    "...q...",
    ".......",
  },
  { -- sun: a gilded scarab head, gold shaft, blue wing-case fletching
    ".......",
    "...G...",
    "..GGg..",
    "..ggo..",
    "...o...",
    "...g...",
    "...o...",
    "...g...",
    "...o...",
    "...g...",
    "..bob..",
    ".bBoBb.",
    ".b.o.b.",
    "...o...",
    "...d...",
    ".......",
  },
  { -- frost: a long icicle head on pale wood, white fur fletching
    ".......",
    "...I...",
    "...I...",
    "..IIi..",
    "..Iij..",
    "...j...",
    "...w...",
    "...v...",
    "...w...",
    "...v...",
    "..fvf..",
    ".fFvFf.",
    ".f.v.f.",
    "...v...",
    "...v...",
    ".......",
  },
  { -- cleft: a rift crystal head, dense stone shaft, bone fletching
    ".......",
    "...R...",
    "..RRr..",
    "..Rrp..",
    "..rpp..",
    "...c...",
    "...C...",
    "...c...",
    "...C...",
    "...c...",
    "..xcx..",
    ".xXcXx.",
    ".x.c.x.",
    "...c...",
    "...R...",
    ".......",
  },
}, {
  N = hex("f4efe6"), n = hex("c9bdd1"), m = hex("635478"), t = hex("2f8aa0"), T = hex("7fd4e0"),
  K = hex("d18a4a"), k = hex("9a5140"), q = hex("5e2f45"),
  G = hex("fff1a8"), g = hex("f5c04a"), o = hex("c7802a"), d = hex("7a3e1e"),
  B = hex("8ab4ff"), b = hex("3a5ac0"),
  I = hex("eaffff"), i = hex("8ae4f8"), j = hex("3a8ac0"), w = hex("e8cf94"), v = hex("c0a066"),
  F = hex("f4f7ff"), f = hex("8ea2c4"),
  R = hex("ff9ad9"), r = hex("d94ecb"), p = hex("8a2f9c"), C = hex("847696"), c = hex("584a6e"),
  X = hex("fff0c8"), x = hex("c0a066"),
}, 7, 16, PDIR .. "WindArrow.png")

-- the spray's pellet: a chip of shell in a bead of sea water
sheet({ {
  ".....",
  "..N..",
  ".NTt.",
  ".Ttj.",
  ".....",
} }, { N = hex("f4efe6"), T = hex("7fd4e0"), t = hex("2f8aa0"), j = hex("1d5a7a") }, 5, 5, PDIR .. "WindPellet.png")

-- the frost wind's icicle, hanging point down
sheet({ {
  ".......",
  ".IIIii.",
  ".IiIij.",
  "..Iij..",
  "..Iij..",
  "..iij..",
  "...ij..",
  "...ij..",
  "...i...",
  "...j...",
  ".......",
} }, { I = hex("eaffff"), i = hex("8ae4f8"), j = hex("3a8ac0") }, 7, 11, PDIR .. "WindIcicle.png")
