-- The melee tree of CRAFTING_TREES.md, "Confluence": four swords and the one wave texture
-- they share. Run from the repo root:
--
--   aseprite -b --script-param root=<repo> --script Art/trees/melee.lua
--
-- Every blade is laid on the 45-degree diagonal in a half-size art buffer: u runs from the
-- guard to the tip, w across it (negative = the upper-left, lit side). The silhouette comes
-- from a hand-authored half-width table per sword, the colour from a rule that lights the
-- blade across its own width, and the fittings (guards, hooks, gems, rings) are stamped
-- from char maps. Outlined in the buffer, blitted 2x2 - the scythes' grid.
--
--   Confluence       the four ingredients laminated: a gold spine and a tide-steel edge that
--                    swap sides down the blade (the braid), a bone core, a glacier tip, the
--                    khopesh hook off the guard and a barnacle crust on the other quillon
--   True Confluence  the same braid re-forged in void and thread: wider, the Cleaver's rift
--                    shards up the spine, a cleft tip, the four currents kept as four gems
--   Worldseam        pale silk, split down its length by a dark seam that thread stitches
--                    shut, a long Warp crossbar, a curved single edge
--   Meridian         prime white, the longest blade, a dark core of Nothing, and the meridian
--                    ring behind the guard with five warm studs - one per blade it calls up

dofile(app.params.root .. "/Art/reaper/lib.lua")

local DIR = "Content/Items/Weapons/Trees/Melee/"
local PDIR = "Content/Projectiles/Trees/Melee/"
local R2 = math.sqrt(2)

-- ramps ----------------------------------------------------------------------------------
local TIDE = { hex("d8fff7"), hex("6fd8d8"), hex("2f9fb0"), hex("1d5a7a") }
local SUN = { hex("fff1a8"), hex("f5c04a"), hex("c7802a"), hex("7a3e1e") }
local FROST = { hex("eaffff"), hex("a3e9f8"), hex("58c3e6"), hex("2c87c0") }
local BONE = { hex("fff0c8"), hex("e8cf94"), hex("c0a066"), hex("8a6b45"), hex("5a4030") }
local LEATHER = { hex("f0c07a"), hex("d18a4a"), hex("9a5140"), hex("5e2f45") }
local BARN = { hex("c9bdd1"), hex("93849f"), hex("635478") }
local RIFTP = { hex("ffffff"), hex("ff9ad9"), hex("d94ecb"), hex("8a2f9c") }
local THREAD = { hex("d8fff7"), hex("7ff2e0"), hex("35c9b8"), hex("1c7d78") }
local VOID = { hex("9fb4bb"), hex("61818c"), hex("3d5560"), hex("22343e"), hex("0f1c26") }
local SILK = { hex("eef9f6"), hex("c2e0da"), hex("93b8b6"), hex("648d90"), hex("3d6068") }
local FRAME = { hex("22343e"), hex("0a1620") }
local PRIME = { hex("ffffff"), hex("eef1f8"), hex("c8cee0"), hex("9aa2bc"), hex("666e8c"), hex("3b4260"), hex("22263c") }
local PRIME_OUT = hex("0d0f1c")
local REGARD = { hex("ffd79a"), hex("ff9f4a") }

-- the diagonal frame ---------------------------------------------------------------------
-- u along the blade (guard -> tip), w across (negative toward the upper-left)
local function uw(x, y, gx, gy)
  local px, py = x + 0.5 - gx, y + 0.5 - gy
  return (px - py) / R2, (px + py) / R2
end

-- fill every cell whose (u, w) falls inside the profile; fn returns a colour or nil
local function blade(g, gx, gy, len, hwAt, fn)
  for y = 0, g.h - 1 do
    for x = 0, g.w - 1 do
      local u, w = uw(x, y, gx, gy)
      if u >= 0 and u <= len then
        local t = u / len
        local hw, wc = hwAt(t)
        wc = wc or 0
        if hw > 0 and math.abs(w - wc) <= hw then
          local col = fn(t, u, w - wc, hw, w)
          if col then put(g, x, y, col) end
        end
      end
    end
  end
end

-- a disc / annulus of cells, centre in cell coordinates
local function ring(g, cx, cy, r0, r1, fn)
  for y = 0, g.h - 1 do
    for x = 0, g.w - 1 do
      local dx, dy = x + 0.5 - cx, y + 0.5 - cy
      local d = math.sqrt(dx * dx + dy * dy)
      if d >= r0 and d <= r1 then
        local col = fn(dx, dy, d)
        if col then put(g, x, y, col) end
      end
    end
  end
end

-- light across the width: lit edge (upper-left), body, shade, dark edge
local function across(ramp, w, hw)
  local s = w / hw
  if s < -0.55 then return ramp[1] end
  if s < 0.05 then return ramp[2] end
  if s < 0.6 then return ramp[3] end
  return ramp[4]
end

local function finish(g, name, outlineCol)
  if outlineCol then
    local saved = OUTLINE
    OUTLINE = outlineCol
    outline(g)
    OUTLINE = saved
  else
    outline(g)
  end
  save(g, name)
end

-------------------------------------------------------------------------------------------
-- 1. Confluence (T2) - 30x30 art, 60x60 px
do
  local g = newGrid(30, 30)
  local gx, gy, len = 9.5, 20.5, 24.5
  -- grip: leather wrap, lit across its width, two grain ticks; a bone pommel
  haft(g, 3, 27, 9, 21, 1.05, LEATHER[2], LEATHER[3], LEATHER[4], { 0.3, 0.62 })
  stamp(g, {
    ".bB",
    "bBb",
    "Bbd",
  }, { B = BONE[1], b = BONE[2], d = BONE[4] }, 1, 26)

  local function hw(t)
    if t < 0.72 then return 2.9 + 0.55 * math.sin(t / 0.72 * math.pi) end
    return 2.9 * (1 - t) / 0.28 + 0.2
  end
  blade(g, gx, gy, len, hw, function(t, u, w, h)
    if t > 0.72 then
      -- the glacier tip, a hard break from the steel below it
      if w < -h * 0.3 then return FROST[1] end
      if w < h * 0.35 then return FROST[2] end
      return FROST[3]
    end
    -- the braid: the gold spine and the tide edge cross over the bone core every ~7 cells
    local b = 1.25 * math.sin(u * 0.62)
    local d = w - b
    if math.abs(d) < 0.55 then
      return (math.floor(u * 0.9) % 2 == 0) and BONE[2] or BONE[3]
    end
    local sideSun = (d < 0) == (math.cos(u * 0.62) > -0.2)
    if sideSun then
      if w < -h * 0.6 then return SUN[1] end
      return d < -1.6 and SUN[2] or SUN[3]
    else
      if w > h * 0.7 then return TIDE[1] end
      return d > 1.6 and TIDE[2] or TIDE[3]
    end
  end)
  -- the guard: the khopesh hook curls up off the upper quillon, barnacles crust the lower
  stamp(g, {
    "..GG.....",
    ".G..g....",
    ".G...gg..",
    "..g...Sg.",
    "......gSg",
  }, { G = SUN[1], g = SUN[2], S = SUN[3] }, 2, 14)
  stamp(g, {
    ".nB..",
    "nBnb.",
    ".bnBn",
    "..nb.",
  }, { B = BARN[1], b = BARN[2], n = BARN[3] }, 9, 21)
  stamp(g, { "t" }, { t = TIDE[2] }, 10, 23)
  finish(g, DIR .. "Confluence.png")
end

-------------------------------------------------------------------------------------------
-- 2. True Confluence (T4) - 34x34 art, 68x68 px
do
  local g = newGrid(34, 34)
  local gx, gy, len = 10.5, 23.5, 28.5
  haft(g, 3, 31, 10, 24, 1.1, VOID[2], VOID[3], VOID[4], { 0.25, 0.55, 0.8 })
  stamp(g, {
    ".tT",
    "tTt",
    "Ttd",
  }, { T = THREAD[1], t = THREAD[3], d = THREAD[4] }, 1, 30)

  local function hw(t)
    if t < 0.78 then return 3.3 + 0.9 * math.sin(t / 0.78 * math.pi * 0.85) end
    return 3.6 * (1 - t) / 0.22 + 0.4
  end
  blade(g, gx, gy, len, hw, function(t, u, w, h)
    -- a cleft tip: the Cleaver's split shard
    if t > 0.84 and math.abs(w) < 0.75 then return nil end
    if w > h - 0.95 then return THREAD[2] end -- the edge is thread, the one lit thing
    -- the braid kept as a single strand of thread wound down the fuller
    local b = 1.4 * math.sin(u * 0.55)
    if t > 0.06 and t < 0.8 and math.abs(w - b) < 0.5 then
      return (math.cos(u * 0.55) > 0) and THREAD[1] or THREAD[3]
    end
    return across(VOID, w, h)
  end)
  -- rift shards up the spine, the Cleaver's crystals
  for _, s in ipairs({ { 14, 15 }, { 18, 11 }, { 22, 7 } }) do
    stamp(g, {
      "P.",
      "pP",
      ".p",
    }, { P = RIFTP[2], p = RIFTP[3] }, s[1] - 3, s[2] - 2)
  end
  -- a swept guard, both wings rising toward the tip
  stamp(g, {
    "aA.......",
    ".aA......",
    "..aV.....",
    "...VV....",
    "....VVv..",
    ".....vVa.",
    "......vAa",
  }, { A = VOID[1], a = VOID[2], V = VOID[3], v = VOID[4] }, 3, 17)
  stamp(g, {
    "aV.....",
    "VVv....",
    ".vVa...",
    "..vVaA.",
    "....aaA",
  }, { A = VOID[1], a = VOID[2], V = VOID[3], v = VOID[4] }, 10, 24)
  -- the four currents as four gems down the ricasso: tide, sun, frost, bone
  stamp(g, { "x" }, { x = TIDE[2] }, 12, 21)
  stamp(g, { "x" }, { x = SUN[2] }, 13, 20)
  stamp(g, { "x" }, { x = FROST[1] }, 14, 19)
  stamp(g, { "x" }, { x = BONE[1] }, 15, 18)
  stamp(g, { "R" }, { R = RIFTP[1] }, 10, 23)
  finish(g, DIR .. "TrueConfluence.png")
end

-------------------------------------------------------------------------------------------
-- 3. Worldseam (T5) - 36x36 art, 72x72 px. Pale garment, dark frame.
do
  local g = newGrid(36, 36)
  local gx, gy, len = 10.5, 25.5, 31
  haft(g, 3, 33, 10, 26, 1.0, FRAME[1], FRAME[2], FRAME[2], nil)
  -- the grip is wrapped in silk: three bands across it
  for _, p in ipairs({ { 5, 30 }, { 7, 28 } }) do
    stamp(g, { ".s", "s." }, { s = SILK[2] }, p[1], p[2])
  end
  stamp(g, {
    ".sS",
    "sSs",
    "Ssd",
  }, { S = SILK[1], s = SILK[3], d = SILK[5] }, 1, 32)

  -- a single curved edge: the centre line bows toward the spine near the tip
  local function hw(t)
    local wc = -3.4 * t * t
    if t < 0.82 then return 2.25 + 0.35 * t, wc end
    return 2.5 * (1 - t) / 0.18 + 0.3, wc
  end
  blade(g, gx, gy, len, hw, function(t, u, w, h)
    -- the seam: a dark cut down the middle of the blade, stitched across every few cells
    local inSeam = t > 0.08 and t < 0.84
    if inSeam and math.floor(u) % 4 == 0 and math.abs(w) < 1.3 then return THREAD[2] end
    if inSeam and math.abs(w + 0.1) < 0.55 then return FRAME[2] end
    if w > h - 0.8 then return SILK[1] end -- the cutting edge, bright
    local s = w / h
    if s < -0.6 then return SILK[2] end
    if s < 0 then return SILK[3] end
    if s < 0.55 then return SILK[2] end
    return SILK[4]
  end)
  -- the Warp crossbar: long, straight, silk-capped, across the blade
  stamp(g, {
    "SS........",
    "sFS.......",
    ".FFs......",
    "..FFs.....",
    "...FFF....",
    "....FFF...",
    ".....sFF..",
    "......sFF.",
    ".......sFs",
    "........SS",
  }, { S = SILK[1], s = SILK[3], F = FRAME[1] }, 4, 19)
  stamp(g, { "T" }, { T = THREAD[1] }, 10, 25)
  finish(g, DIR .. "Worldseam.png")
end

-------------------------------------------------------------------------------------------
-- 4. Meridian (T6) - 40x40 art, 80x80 px. Prime white, one warm tone.
do
  local g = newGrid(40, 40)
  local gx, gy, len = 12.5, 27.5, 35.5
  -- the meridian ring behind the guard: drawn first so the blade and grip overwrite it
  local rcx, rcy = 12.5, 27.5
  ring(g, rcx, rcy, 6.2, 7.6, function(dx, dy, d)
    local a = math.atan(dy, dx)
    local lit = (dx + dy) < 0
    return lit and PRIME[3] or PRIME[5]
  end)
  -- five warm studs on the ring, one per blade the sword calls up
  for k = 0, 4 do
    local a = math.rad(-45 + 72 * k + 36)
    local sx, sy = rcx + math.cos(a) * 6.9, rcy + math.sin(a) * 6.9
    put(g, math.floor(sx), math.floor(sy), REGARD[1])
  end
  haft(g, 4, 36, 12, 28, 1.05, PRIME[4], PRIME[5], PRIME[6], { 0.3, 0.6 })
  stamp(g, {
    ".wW.",
    "wWWp",
    "WWpd",
    ".pd.",
  }, { W = PRIME[1], w = PRIME[3], p = PRIME[4], d = PRIME[6] }, 1, 35)

  local function hw(t)
    if t < 0.12 then return 3.2 + t * 6 end
    if t < 0.7 then return 3.9 - (t - 0.12) * 1.6 end
    return 2.97 * (1 - t) / 0.3 + 0.25
  end
  blade(g, gx, gy, len, hw, function(t, u, w, h)
    -- Nothing's dark core, narrowing to a point well before the tip
    if t > 0.05 and math.abs(w) < 0.7 * (1 - t) + 0.15 and t < 0.82 then return PRIME[7] end
    local s = w / h
    if s < -0.62 then return PRIME[1] end
    if s < 0 then return PRIME[2] end
    if s < 0.6 then return PRIME[3] end
    return PRIME[4]
  end)
  -- a guard like an open lid: two wide shell plates and the regard between them
  stamp(g, {
    "wW.......",
    ".wWp.....",
    "..pWWp...",
    "...pW....",
  }, { W = PRIME[1], w = PRIME[3], p = PRIME[5] }, 5, 21)
  stamp(g, {
    "...pW....",
    "....pWWp.",
    ".....pWw.",
    "......pWw",
  }, { W = PRIME[2], w = PRIME[3], p = PRIME[5] }, 9, 25)
  stamp(g, {
    ".r.",
    "rRr",
    ".r.",
  }, { R = REGARD[1], r = REGARD[2] }, 11, 25)
  finish(g, DIR .. "Meridian.png", PRIME_OUT)
end

-------------------------------------------------------------------------------------------
-- The wave every node throws: a white crescent bulging toward +x, tinted per current at draw
-- time. No outline - it is drawn as light.
do
  local g = newGrid(14, 28)
  crescent(g, { x = 3, y = 14, r = 12.5 }, { x = -2.5, y = 14, r = 13.2 },
    function(px, py) return px > 1 end,
    { hex("ffffff"), hex("ffffff"), hex("d8d8e0"), hex("a8a8b8"), hex("8a8a9c") })
  save(g, PDIR .. "ConfluenceWave.png")
end
