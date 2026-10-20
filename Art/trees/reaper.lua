-- The reaper's crafting tree, "Harvest Moon": four scythes and the moon glyphs the reach
-- circle draws over them. Run from the repo root:
--
--   aseprite -b --script-param root=<repo> --script Art/trees/reaper.lua
--
-- Built on Art/reaper/lib.lua - the 2x2 art grid, the outline, hafts lit across their
-- width, crescent regions - and drawn the way the class's own scythes are: a long thin
-- haft from the bottom-left, a broad blade off the neck that sweeps right and DROPS below
-- it, the concave edge facing back at the wielder. One character of a map is one 2x2 block.
--
-- Each node carries the one below it, so each blade is the last one plus something that
-- changes its silhouette, never only its colour:
--   Harvest Moon   a fat amber moon-crescent, cratered, the four ingredients as gems at the neck
--   Eclipse        that crescent gone dark with a gold corona rim, a second (rift) blade inside
--                  it, and a black disc ringed in gold at the neck
--   Long Night     a longer night-blue blade full of stars, the Death Sickle's hook on its back
--                  and a pale moon over an ember at the neck
--   Event Horizon  the longest: prime white, matter streaking down its spine toward a black
--                  hole set in the neck, a tilted accretion ring across it

dofile(app.params.root .. "/Art/reaper/lib.lua")

local DIR = "Content/Items/Weapons/Trees/Reaper/"

local function finish(g, path)
  outline(g)
  save(g, path)
end

-- a deterministic per-cell hash, so craters and stars land in the same place every run
local function hash(x, y, seed)
  local v = math.sin(x * 12.9898 + y * 78.233 + seed * 37.719) * 43758.5453
  return v - math.floor(v)
end

-------------------------------------------------------------------------------
-- 1. Harvest Moon, 22x22 -> 44 px
do
  local g = newGrid(22, 22)
  haft(g, 1.3, 20.8, 8.4, 4.8, 1.05, WOOD[1], WOOD[2], WOOD[3], { 0.34, 0.7 })
  local A, B = { x = 13.4, y = 10.0, r = 8.8 }, { x = 10.2, y = 14.6, r = 7.2 }
  local MOON = { hex("fff3d6"), hex("f0c07a"), hex("e09a4a"), hex("b0603a"), hex("fff0c8") }
  crescent(g, A, B, function(x, y) return x > 8.2 and y < 17.5 end, MOON)
  -- craters: clumps in the body, never on the edge or the spine
  eachBladeCell(g, A, B, -160, function(x, y, s, fromSpine, fromEdge)
    if fromSpine > 1.2 and fromEdge > 1.2 and hash(x, y, 1) < 0.22 then
      put(g, x, y, hex("c07a40"))
      if get(g, x + 1, y) == MOON[3] then put(g, x + 1, y, hex("f0c07a")) end
    end
  end)
  -- the four it was forged from, a gem each in the collar: tide, sun, frost, fire
  stamp(g, {
    ".bb.",
    "bts.",
    "bfh.",
    ".b..",
  }, { b = hex("8a6b45"), t = hex("78dcd2"), s = hex("ffc85a"), f = hex("a0e6ff"), h = hex("ff9040") }, 5, 6)
  finish(g, DIR .. "HarvestMoon.png")
end

-------------------------------------------------------------------------------
-- 2. Eclipse, 25x25 -> 50 px
do
  local g = newGrid(25, 25)
  haft(g, 1.3, 23.8, 9.4, 5.2, 1.1, hex("9fb4bb"), hex("61818c"), hex("3d5560"), { 0.3, 0.55, 0.8 })
  -- the outer blade: the moon gone dark, its back lit by the corona
  local A, B = { x = 15.4, y = 11.0, r = 9.8 }, { x = 12.6, y = 15.6, r = 8.9 }
  crescent(g, A, B, function(x, y) return x > 9.4 and y < 20.5 end,
    { hex("fff1a8"), hex("f5c04a"), hex("22343e"), hex("0f1c26"), hex("ffd79a") })
  -- the inner blade: the Keeper's Harvest, rift-pink, riding inside the hollow two cells clear
  local A2, B2 = { x = 14.6, y = 15.6, r = 6.4 }, { x = 12.6, y = 17.8, r = 5.4 }
  crescent(g, A2, B2, function(x, y) return x > 11.4 and y < 20.0 end,
    { hex("fff2fb"), hex("ff9ad9"), hex("d94ecb"), hex("8a2f9c"), hex("fff2fb") })
  -- the disc at the neck, ringed in gold, the diamond bead on its rim
  stamp(g, {
    "..ggg..",
    ".gkkkW.",
    "gkkKkkg",
    "gkKkkkg",
    "gkkkkkg",
    ".gkkkg.",
    "..ggg..",
  }, { g = hex("f5c04a"), k = hex("0f1c26"), K = hex("22343e"), W = hex("ffffff") }, 6, 1)
  finish(g, DIR .. "Eclipse.png")
end

-------------------------------------------------------------------------------
-- 3. Long Night, 28x28 -> 56 px
do
  local g = newGrid(28, 28)
  haft(g, 1.3, 26.8, 10.6, 5.8, 1.15, hex("93b8b6"), hex("3d6068"), hex("22343e"), { 0.25, 0.5, 0.75 })
  local A, B = { x = 17.6, y = 12.4, r = 10.9 }, { x = 14.4, y = 17.4, r = 9.8 }
  crescent(g, A, B, function(x, y) return x > 10.8 and y < 22.5 end,
    { hex("c2e0da"), hex("93b8b6"), hex("3d6068"), hex("22343e"), hex("eef9f6") })
  -- stars in the night of the blade
  eachBladeCell(g, A, B, -160, function(x, y, s, fromSpine, fromEdge)
    if fromSpine > 1.1 and fromEdge > 1.1 then
      local h = hash(x, y, 3)
      if h < 0.1 then put(g, x, y, hex("ffffff"))
      elseif h < 0.16 then put(g, x, y, hex("93b8b6")) end
    end
  end)
  -- the Death Sickle's hook, grown off the back of the blade
  stamp(g, {
    "..ab",
    ".ab.",
    "ab..",
  }, { a = hex("eef9f6"), b = hex("c2e0da") }, 20, 21)
  -- a pale moon over the neck and the Mourning Scythe's ember hung under it
  -- a pale crescent moon set in the blade by the neck, ringed so it is not a sticker
  stamp(g, {
    ".ooo.",
    "oMMfo",
    "oMfko",
    "oMMfo",
    ".ooo.",
  }, { o = hex("0a1620"), M = hex("eef9f6"), f = hex("c2e0da"), k = hex("22343e") }, 12, 2)
  -- the Mourning Scythe's ember, bound to the neck
  stamp(g, { "kk", "ek", ".e" }, { k = hex("3d6068"), e = hex("ff9640") }, 9, 6)
  finish(g, DIR .. "LongNight.png")
end

-------------------------------------------------------------------------------
-- 4. Event Horizon, 31x31 -> 62 px. T6: the prime ramp and its harder outline
do
  local saved = OUTLINE
  OUTLINE = hex("0d0f1c")
  local g = newGrid(31, 31)
  haft(g, 1.4, 29.6, 11.6, 6.6, 1.25, hex("eef1f8"), hex("9aa2bc"), hex("3b4260"), { 0.22, 0.44, 0.66, 0.88 })
  local A, B = { x = 19.8, y = 13.6, r = 12.2 }, { x = 16.0, y = 19.6, r = 11.0 }
  crescent(g, A, B, function(x, y) return x > 12.6 and y < 25.0 end,
    { hex("ffffff"), hex("eef1f8"), hex("c8cee0"), hex("9aa2bc"), hex("ffffff") })
  -- matter being drawn down the spine into the hole: dark streaks with the pillars'
  -- colours caught in them, broken into dashes so it reads as falling, not as a stripe
  local PIL = { hex("ff9a3a"), hex("3ae0c0"), hex("ff6ad8"), hex("6ab0ff") }
  eachBladeCell(g, A, B, -160, function(x, y, s, fromSpine, fromEdge)
    if fromEdge >= 1.0 and fromSpine >= 1.0 and fromSpine < 2.6 then
      local k = math.floor(s / 12)
      if k % 3 == 1 and fromSpine < 2.0 then put(g, x, y, PIL[(math.floor(s / 36) % 4) + 1])
      else put(g, x, y, hex("666e8c")) end
    end
  end)
  -- the black hole in the neck, built from its geometry: core, photon ring, and the
  -- accretion ring tilted across it - its near half crosses the core, its far half hides
  local cx, cy, tilt = 10.6, 6.2, math.rad(-30)
  for y = -2, 14 do
    for x = 1, 20 do
      local dx, dy = x + 0.5 - cx, y + 0.5 - cy
      local d = math.sqrt(dx * dx + dy * dy)
      local u = dx * math.cos(tilt) + dy * math.sin(tilt)
      local v = -dx * math.sin(tilt) + dy * math.cos(tilt)
      local e = math.sqrt(u * u + (v / 0.42) ^ 2)
      local inRing = e >= 5.0 and e <= 7.0
      if inRing and (v > 0 or d > 4.4) then
        put(g, x, y, (u > 0) and hex("ffd79a") or hex("ff9f4a"))
        if e > 6.2 then put(g, x, y, (u > 0) and hex("eef1f8") or hex("9aa2bc")) end
      elseif d <= 3.4 then put(g, x, y, (d > 2.6 and dx < -0.5 and dy < -0.5) and hex("22263c") or hex("0d0f1c"))
      elseif d <= 4.4 then put(g, x, y, hex("ffffff")) end
    end
  end
  finish(g, DIR .. "EventHorizon.png")
  OUTLINE = saved
end

-------------------------------------------------------------------------------
-- The moon glyphs: four 16x16 art frames stacked into a 32x128 sheet, read by
-- MoonReachLayer by frame. Saved on a fixed canvas - save() crops to the bounding
-- box, and a sheet's frames have to sit on their pitch.
local function saveFixed(g, path, w, h)
  local img = Image(w * 2, h * 2, ColorMode.RGB)
  for _, c in ipairs(cells(g)) do
    if c[1] >= 0 and c[1] < w and c[2] >= 0 and c[2] < h then
      for sy = 0, 1 do
        for sx = 0, 1 do img:drawPixel(c[1] * 2 + sx, c[2] * 2 + sy, c[3]) end
      end
    end
  end
  local spr = Sprite(img.width, img.height, ColorMode.RGB)
  spr.cels[1].image = img
  spr:saveAs(root .. "/" .. path)
end

do
  local g = newGrid(16, 64)
  -- 0: the harvest moon, full, cratered
  stamp(g, {
    "................",
    "................",
    ".....aaaaaa.....",
    "....abbbbbbc....",
    "...abbbbcbbbc...",
    "..abbccbbbbbcd..",
    "..abbccbbbbbcd..",
    "..abbbbbbbccdd..",
    "..abbbbbbbccdd..",
    "..abbbccbbbcdd..",
    "...bbbccbbbcd...",
    "....bccccccd....",
    ".....dddddd.....",
    "................",
    "................",
    "................",
  }, { a = hex("fff3d6"), b = hex("f0c07a"), c = hex("d18a4a"), d = hex("9a5140") }, 0, 0)
  -- 1: the eclipse, a black disc in a gold ring with the diamond bead
  stamp(g, {
    "................",
    "................",
    ".....gggggW.....",
    "....gkkkkkkW....",
    "...gkkkkkkkkg...",
    "..gkkKkkkkkkkg..",
    "..gkKkkkkkkkkg..",
    "..gkkkkkkkkkkg..",
    "..gkkkkkkkkkkg..",
    "..gkkkkkkkkkkg..",
    "...gkkkkkkkkg...",
    "....gkkkkkkg....",
    ".....gggggg.....",
    "................",
    "................",
    "................",
  }, { g = hex("f5c04a"), k = hex("0f1c26"), K = hex("22343e"), W = hex("ffffff") }, 0, 16)
  -- 2: the night moon, a pale crescent with two stars
  stamp(g, {
    "................",
    "............w...",
    ".....aaaa..www..",
    "....abbb....w...",
    "...abbc.........",
    "..abbc..........",
    "..abc...........",
    "..abc.......w...",
    "..abc...........",
    "..abbc..........",
    "...abbc.........",
    "....abbbc.......",
    ".....cccc.......",
    "................",
    "................",
    "................",
  }, { a = hex("eef9f6"), b = hex("c2e0da"), c = hex("648d90"), w = hex("ffffff") }, 0, 32)
  -- 3: the event horizon, a black core in a photon ring (the ring is drawn live round it)
  stamp(g, {
    "................",
    "................",
    "................",
    "......wwww......",
    ".....wkkkkw.....",
    "....wkkkkkkw....",
    "...wkkkkkkkkw...",
    "...wkkkkkkkkr...",
    "...rkkkkkkkkw...",
    "...wkkkkkkkkw...",
    "....wkkkkkkw....",
    ".....wkkkkw.....",
    "......wwww......",
    "................",
    "................",
    "................",
  }, { w = hex("eef1f8"), k = hex("0d0f1c"), r = hex("ffd79a") }, 0, 48)
  -- each glyph gets the hard outline, frame by frame, without bleeding across frames
  outline(g)
  saveFixed(g, "Content/Projectiles/Trees/Reaper/MoonGlyphs.png", 16, 64)
end

print("trees/reaper: done")
