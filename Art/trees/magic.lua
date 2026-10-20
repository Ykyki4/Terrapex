-- The magic tree of CRAFTING_TREES.md: Almanac -> Equinox Almanac -> Solstice -> Cosmogony,
-- plus the four page glyphs and the loose page the Cosmogony orbits. Run from the repo root:
--
--   aseprite -b --script-param root=<repo> --script Art/trees/magic.lua
--
-- Hand-drawn half-size maps, outlined in the art buffer, blitted 2x2 - the scythes' grid.
-- Each book is drawn out of its ingredients:
--   Almanac   a leather book with the four tomes in its corners: the conch's shell, the
--             scepter's sun, the staff's snowflake, the tome's scree; a riftglass clasp
--   Equinox   the Almanac clasped to a second, void-bound book by a thread strap, the
--             Riftflow's crystal where the strap crosses
--   Solstice  a codex open on two pages - sun left, moon right - stood inside the Loom
--             Staff's frame, a leaf (the Leaf Blower) hanging as the bookmark
--   Cosmogony the T6 open tome, night-sky pages, the Regard's eye risen over the gutter and
--             the four pages loose around it in their glyph colours

dofile(app.params.root .. "/Art/reaper/lib.lua")

local ITEMS = "Content/Items/Weapons/Trees/Magic/"
local PROJ = "Content/Projectiles/Trees/Magic/"

local function check(rows, name)
  for i, r in ipairs(rows) do
    if #r ~= #rows[1] then error(name .. " row " .. (i - 1) .. " is " .. #r .. " wide, not " .. #rows[1]) end
  end
end

local function draw(rows, pal, name)
  check(rows, name)
  local g = newGrid(1, 1)
  stamp(g, rows, pal, 0, 0)
  outline(g)
  save(g, ITEMS .. name .. ".png")
end

-- a fixed-size save, for sheets whose frames must sit on an exact pitch
local function saveBox(g, w, h, path)
  local img = Image(w * 2, h * 2, ColorMode.RGB)
  for _, c in ipairs(cells(g)) do
    if c[1] >= 0 and c[2] >= 0 and c[1] < w and c[2] < h then
      for sy = 0, 1 do
        for sx = 0, 1 do img:drawPixel(c[1] * 2 + sx, c[2] * 2 + sy, c[3]) end
      end
    end
  end
  local spr = Sprite(img.width, img.height, ColorMode.RGB)
  spr.cels[1].image = img
  spr:saveAs(root .. "/" .. path)
end

-- ramps (CLAUDE.md) --------------------------------------------------------------------
local LEATHER = { hex("f0c07a"), hex("d18a4a"), hex("9a5140"), hex("5e2f45"), hex("3a1c33") }
local BONE = { hex("fff0c8"), hex("e8cf94"), hex("c0a066"), hex("8a6b45") }
local GOLD = { hex("fff1a8"), hex("f5c04a"), hex("c7802a") }
local TEAL = { hex("7fd4e0"), hex("2f8aa0"), hex("1d5a7a") }
local ICE = { hex("eaffff"), hex("8ae4f8"), hex("3a8ac0") }
local STONE = { hex("c9bdd1"), hex("93849f"), hex("635478") }
local RIFT = { hex("ffffff"), hex("ff9ad9"), hex("d94ecb"), hex("8a2f9c") }
local THREAD = { hex("d8fff7"), hex("7ff2e0"), hex("35c9b8"), hex("1c7d78") }
local VOID = { hex("9fb4bb"), hex("61818c"), hex("3d5560"), hex("22343e"), hex("0f1c26") }
local SILK = { hex("eef9f6"), hex("c2e0da"), hex("93b8b6"), hex("648d90"), hex("3d6068") }
local FRAME = { hex("22343e"), hex("0a1620") }
local LEAF = { hex("b8f070"), hex("6cc04a"), hex("3a7a3a") }
local PRIME = { hex("ffffff"), hex("eef1f8"), hex("c8cee0"), hex("9aa2bc"), hex("666e8c"), hex("3b4260"), hex("22263c") }
local REGARD = { hex("ffd79a"), hex("ff9f4a") }

-- Almanac -----------------------------------------------------------------------------
-- spine on the left, page block showing on the right and bottom, an emblem in each corner,
-- the clasp's strap running in from the page edge to a riftglass bead
draw({
  "SsLLLLLLLLLLLP.",
  "SsLDDDDDDDDDMPp",
  "SsLDTTbMhGhDMPp",
  "SsLDTbTMGWGDMPp",
  "SsLDbbMMhGhDMPp",
  "SsLDMMMMMMMDMPp",
  "SsLDMMMrMMMDMPp",
  "SsLDMMrWRkkkkkk",
  "SsLDMMMRMMMDMPp",
  "SsLDMMMMMMMDMPp",
  "SsLDIMIMMNnDMPp",
  "SsLDMjMMNnmDDPp",
  "SsLDIMIMMnmDDPp",
  "SsLDDDDDDDDDDPp",
  "SsDDDDDDDDDDDPp",
  ".SPPPPPPPPPPPpq",
}, {
  S = LEATHER[5], s = LEATHER[4], L = LEATHER[1], M = LEATHER[2], D = LEATHER[3],
  P = BONE[1], p = BONE[2], q = BONE[3], k = BONE[2],
  T = BONE[1], b = BONE[3],
  G = GOLD[2], W = hex("ffffff"), h = GOLD[3],
  I = ICE[2], j = ICE[1],
  N = STONE[1], n = STONE[2], m = STONE[3],
  R = RIFT[2], r = RIFT[3],
}, "Almanac")

-- Equinox Almanac ---------------------------------------------------------------------
-- the Almanac in front, the void book behind it up and to the right, one thread strap
-- binding both, and the Riftflow's crystal set where the strap crosses the front cover
draw({
  "......VVVVVVVVVVVv.",
  "......VBBBBBBBBBbPp",
  "......VBBqBBBBBBbPp",
  "SsLLLLLLLLLLBBqBbPp",
  "SsLTbMMhGhMPBBBqbPp",
  "SsLbTMMGWGMPBBBBbPp",
  "SsLMMMMMMMMPBBBBbPp",
  "SsLMMMMRMMMPBBBBbPp",
  "SsQQQQRWRQQQQQQQQQQ",
  "SsqqqqrRrqqqqqqqqqq",
  "SsLMMMMrMMMPBBBBbPp",
  "SsLMMMMMMMMPBBBBbPp",
  "SsLIjMMMNnMPbbbbbPp",
  "SsLjIMMMnmDPkkkkkPp",
  "SsDDDDDDDDDP..pppp.",
  ".SPPPPPPPPPp.......",
}, {
  S = LEATHER[5], s = LEATHER[4], L = LEATHER[1], M = LEATHER[2], D = LEATHER[3],
  P = BONE[1], p = BONE[2],
  T = BONE[1], b = BONE[3],
  G = GOLD[2], W = hex("ffffff"), h = GOLD[3],
  I = ICE[2], j = ICE[1],
  N = STONE[1], n = STONE[2], m = STONE[3],
  V = VOID[1], v = VOID[2], B = VOID[2], k = VOID[4],
  Q = THREAD[2], q = THREAD[3],
  R = RIFT[2], r = RIFT[3],
}, "EquinoxAlmanac")

-- T5 outlines in the frame's near-black, T6 in prime's; everything below T5 keeps #241733
local BASE_OUTLINE = OUTLINE

-- Solstice -----------------------------------------------------------------------------
-- an open codex inside a loom frame: two posts and a crossbar, a warp thread down the
-- gutter, the sun on the left page and a moon crescent on the right, a leaf bookmark
OUTLINE = FRAME[2]
draw({
  "...FFFFFFFFFFFFFFFF...",
  "...Ff.q.q.QQ.q.q.fF...",
  "...Ff.q.q.QQ.q.q.fF...",
  "..wwwwWwqwvvwqwWwwww..",
  ".wWWWWWWwwvvwwWWWWWWw.",
  ".wWWWWWWWWvvWWWWWWWWw.",
  ".wWWWhWWWWvvWWWKKKWWw.",
  ".wWhggghWWvvWWKmKKKWw.",
  ".wWgGGGgWWvvWKmmKKKKw.",
  ".whgGWGghWvvWKmKKKKKw.",
  ".wWgGGGgWWvvWKmmKKKKw.",
  ".wWhggghWWvvWWKmKKKWw.",
  ".wWWWhWWWWvvWWWKKKWWw.",
  ".wWWWWWWWWvvWWWWWWWWw.",
  ".wvvvvvvvwvvwvvvvvvvw.",
  ".CwwwwwwwwvvwwwwwwwwC.",
  ".CccccccccccccccccccC.",
  "..CCCCCCCCCCCCCCCCCC..",
  "..........eE..........",
  "..........EEe.........",
  "...........Ee.........",
}, {
  F = FRAME[1], f = SILK[5],
  Q = THREAD[2], q = SILK[3],
  W = SILK[1], w = SILK[2], v = SILK[3], c = SILK[4], C = SILK[5],
  G = GOLD[1], g = GOLD[2], h = GOLD[3],
  K = VOID[4], m = SILK[2],
  E = LEAF[2], e = LEAF[3],
}, "Solstice")

-- Cosmogony ----------------------------------------------------------------------------
-- the Regard's eye risen over an open tome whose pages are night sky; the four loose pages
-- stand off its corners in their glyph colours
OUTLINE = hex("0d0f1c")
draw({
  ".223......3333......223.",
  ".2t3....33222233....2g3.",
  ".2t3...3221oo1223...2g3.",
  ".334..321oOOOOo123..334.",
  "......32oOO77OOo23......",
  "......32oOO77OOo23......",
  ".223..321oOOOOo123..223.",
  ".2i3...3221oo1223...2n3.",
  ".2i3....33222233....2n3.",
  ".334......3333......334.",
  "........................",
  ".....3333......3333.....",
  "....377773....377773....",
  "...3777177733777717773..",
  "..377177777337777777173.",
  "..377777717337177777773.",
  "..371777777337777177773.",
  "..377777177337777777173.",
  "..344444444334444444443.",
  "..355555555555555555553.",
  "...66666666666666666666.",
}, {
  ["1"] = PRIME[1], ["2"] = PRIME[2], ["3"] = PRIME[3], ["4"] = PRIME[4], ["5"] = PRIME[5],
  ["6"] = PRIME[6], ["7"] = PRIME[7],
  o = REGARD[1], O = REGARD[2],
  t = TEAL[1], g = GOLD[2], i = ICE[2], n = STONE[1],
}, "Cosmogony")

-- page glyphs: 4 frames of 9x9 art cells (18x18 px), stacked ----------------------------
local GLYPHS = {
  { -- tide: a bubble with its glint
    "..ttt..",
    ".tTWTt.",
    "tTWTTTt",
    "tTTTTTt",
    "tTTTTdt",
    ".tTTdt.",
    "..ttt..",
  },
  { -- sun: a disc and its rays
    "g..G..g",
    "..ggg..",
    ".gGWGg.",
    "GgWWWgG",
    ".gGWGg.",
    "..ggg..",
    "g..G..g",
  },
  { -- snow: a flake
    "I..I..I",
    ".I.I.I.",
    "..III..",
    "IIIWIII",
    "..III..",
    ".I.I.I.",
    "I..I..I",
  },
  { -- scree: stones falling
    ".NNn...",
    "NNnnm..",
    ".nmm...",
    "....Nn.",
    "...NNnm",
    "..Nnnmm",
    "...mm..",
  },
}
local GPAL = {
  t = TEAL[2], T = TEAL[1], d = TEAL[3], W = hex("ffffff"),
  g = GOLD[2], G = GOLD[1], I = ICE[2], N = STONE[1], n = STONE[2], m = STONE[3],
}
local sheet = newGrid(9, 36)
for k, rows in ipairs(GLYPHS) do
  local g = newGrid(7, 7)
  stamp(g, rows, GPAL, 0, 0)
  outline(g)
  for _, c in ipairs(cells(g)) do put(sheet, c[1] + 1, c[2] + 1 + (k - 1) * 9, c[3]) end
end
saveBox(sheet, 9, 36, PROJ .. "AlmanacGlyphs.png")

-- the loose page of the Cosmogony's ring: prime paper, a folded corner --------------------
local page = newGrid(10, 13)
stamp(page, {
  "22222234",
  "23333344",
  "23333334",
  "23333334",
  "23333334",
  "23333334",
  "23333334",
  "23333334",
  "23333334",
  "23333334",
  "44444445",
}, { ["2"] = PRIME[2], ["3"] = PRIME[3], ["4"] = PRIME[4], ["5"] = PRIME[5] }, 1, 1)
outline(page)
saveBox(page, 10, 13, PROJ .. "AlmanacPage.png")
