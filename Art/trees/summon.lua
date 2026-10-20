-- The summon tree of CRAFTING_TREES.md, "Menagerie": four horns, the beast sheet all four
-- share, the hatchling's spit, and four buff icons. Run from the repo root:
--
--   aseprite -b --script-param root=<repo> --script Art/trees/summon.lua
--
-- Every horn is one curve: a quadratic Bezier from the mouthpiece (left) to the bell (right),
-- its half-width swelling along it. Cells are lit across the horn's own width - lit edge on
-- the upper-left normal, body, shade, dark edge - never by distance to the silhouette, and
-- banded by its fittings. A carrying strap sags under the horn and the ingredients hang off
-- it as trinkets, so each node visibly carries what went into it:
--
--   Menagerie Horn  ivory horn, brass fittings; a crab claw, a beetling, a glacier bell and a
--                   cracked egg on a leather strap
--   Wild Court      the horn in void, banded in thread; the Warden Plate set on it as a crest,
--                   the four trinkets on a thread strap, three soul beads at the bell
--   Grove Conclave  pale silk horn with a dark frame; a jungle vine winding it (the Pygmy
--                   Staff), the Sailcloth hung from the bell as a pennant
--   The Ark         prime white, the longest; warm studs on the bell rim (the Keeper's eye),
--                   a stardust star in the mouth, the four trinkets cast as medallions
--
-- The beast sheet is 16 frames of 32x32 stacked vertically, facing LEFT:
--   0-3 crab (0-2 walk, 3 airborne / gripping), 4-7 beetling (4-5 flap, 6-7 dive),
--   8-11 rime sprite (float), 12-15 hatchling (12-13 bob, 14-15 spit)

dofile(app.params.root .. "/Art/reaper/lib.lua")

local IDIR = "Content/Items/Weapons/Trees/Summon/"
local PDIR = "Content/Projectiles/Trees/Summon/"
local BDIR = "Content/Buffs/Trees/"

-- ramps ----------------------------------------------------------------------------------
local BONE = { hex("fff0c8"), hex("e8cf94"), hex("c0a066"), hex("8a6b45"), hex("5a4030") }
local LEATHER = { hex("f0c07a"), hex("d18a4a"), hex("9a5140"), hex("5e2f45") }
local FROST = { hex("eaffff"), hex("a3e9f8"), hex("58c3e6"), hex("2c87c0") }
local THREAD = { hex("d8fff7"), hex("7ff2e0"), hex("35c9b8"), hex("1c7d78") }
local VOID = { hex("9fb4bb"), hex("61818c"), hex("3d5560"), hex("22343e"), hex("0f1c26") }
local SILK = { hex("eef9f6"), hex("c2e0da"), hex("93b8b6"), hex("648d90"), hex("3d6068") }
local FRAME = { hex("22343e"), hex("0a1620") }
local PRIME = { hex("ffffff"), hex("eef1f8"), hex("c8cee0"), hex("9aa2bc"), hex("666e8c"), hex("3b4260"), hex("22263c") }
local PRIME_OUT = hex("0d0f1c")
local REGARD = { hex("ffd79a"), hex("ff9f4a") }
local LEAF = { hex("c9f08a"), hex("7fc24a"), hex("3f8a3a"), hex("23523a") }
local STAR = { hex("ffffff"), hex("bfe8ff"), hex("5fa8ff"), hex("2a4fb0") }
local SOULS = { hex("6fb8ff"), hex("7cff8a"), hex("ffb04a") } -- might, sight, fright
local EYE = hex("1c2a52")

-------------------------------------------------------------------------------------------
-- fixed-size saves: buff icons and sheets must not be cropped to their bounding box
local function saveFixed(grids, fw, fh, path)
  local img = Image(fw * 2, fh * 2 * #grids, ColorMode.RGB)
  for k, g in ipairs(grids) do
    for _, c in ipairs(cells(g)) do
      local x, y = c[1], c[2]
      if x >= 0 and x < fw and y >= 0 and y < fh then
        for sy = 0, 1 do
          for sx = 0, 1 do img:drawPixel(x * 2 + sx, ((k - 1) * fh + y) * 2 + sy, c[3]) end
        end
      end
    end
  end
  local spr = Sprite(img.width, img.height, ColorMode.RGB)
  spr.cels[1].image = img
  spr:saveAs(root .. "/" .. path)
end

local function withOutline(g, col)
  local saved = OUTLINE
  OUTLINE = col or saved
  outline(g)
  OUTLINE = saved
end

-- a dome lit by its own normal: four hard tones, upper-left lit
local function blob(g, cx, cy, rx, ry, ramp, clip)
  for y = math.floor(cy - ry) - 1, math.ceil(cy + ry) + 1 do
    for x = math.floor(cx - rx) - 1, math.ceil(cx + rx) + 1 do
      local dx, dy = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
      local d = dx * dx + dy * dy
      if d <= 1 and (not clip or clip(x, y)) then
        local l = -(dx * 0.6 + dy * 0.8)
        local col
        if l > 0.5 and d < 0.55 then col = ramp[1]
        elseif l > -0.05 then col = ramp[2]
        elseif l > -0.55 then col = ramp[3]
        else col = ramp[4] end
        put(g, x, y, col)
      end
    end
  end
end

-------------------------------------------------------------------------------------------
-- the horn
local function bez(P0, P1, P2, t)
  local a, b, c = (1 - t) ^ 2, 2 * t * (1 - t), t * t
  return a * P0[1] + b * P1[1] + c * P2[1], a * P0[2] + b * P1[2] + c * P2[2]
end

-- spec: P0 P1 P2, hw0 hw1, ramp (4), bands {{t0,t1,ramp}}, mouth (dark), rim (lit)
-- returns at(t, s): the cell at curve position t, s half-widths off the centre (+ = lit side)
local function horn(g, spec)
  local N = 90
  local pts = {}
  for i = 0, N do
    local t = i / N
    local x, y = bez(spec.P0, spec.P1, spec.P2, t)
    local x2, y2 = bez(spec.P0, spec.P1, spec.P2, math.min(1, t + 0.01))
    local x1, y1 = bez(spec.P0, spec.P1, spec.P2, math.max(0, t - 0.01))
    local tx, ty = x2 - x1, y2 - y1
    local l = math.sqrt(tx * tx + ty * ty)
    local nx, ny = -ty / l, tx / l
    if nx + ny > 0 then nx, ny = -nx, -ny end
    pts[i] = { x = x, y = y, t = t, nx = nx, ny = ny, hw = spec.hw0 + (spec.hw1 - spec.hw0) * t ^ 1.7 }
  end
  for y = -2, g.h + 2 do
    for x = -2, g.w + 2 do
      local px, py = x + 0.5, y + 0.5
      local best, bd = nil, 1e9
      for i = 0, N do
        local p = pts[i]
        local d = (px - p.x) ^ 2 + (py - p.y) ^ 2
        if d < bd then bd, best = d, p end
      end
      if math.sqrt(bd) <= best.hw then
        local side = ((px - best.x) * best.nx + (py - best.y) * best.ny) / best.hw
        local ramp = spec.ramp
        for _, b in ipairs(spec.bands or {}) do
          if best.t >= b[1] and best.t <= b[2] then ramp = b[3] end
        end
        local col
        if best.t >= 0.985 and math.abs(side) < 0.62 then
          col = spec.mouth                                  -- the bell's open throat
        elseif best.t >= 0.95 then
          col = side > -0.2 and (spec.rim or ramp[1]) or ramp[2]  -- the bell's lip
        elseif side > 0.5 then col = ramp[1]
        elseif side > -0.1 then col = ramp[2]
        elseif side > -0.6 then col = ramp[3]
        else col = ramp[4] end
        put(g, x, y, col)
      end
    end
  end
  return function(t, s)
    local i = math.floor(t * N + 0.5)
    local p = pts[i]
    return math.floor(p.x + p.nx * s * p.hw), math.floor(p.y + p.ny * s * p.hw)
  end
end

-- a sagging strap between two cells, one cell thick, every column filled
local function strap(g, ax, ay, bx, by, sag, col)
  local ys = {}
  for x = ax, bx do
    local t = (x - ax) / math.max(1, bx - ax)
    local y = math.floor(ay + (by - ay) * t + math.sin(t * math.pi) * sag + 0.5)
    ys[x] = y
  end
  for x = ax, bx do
    put(g, x, ys[x], col)
    -- close vertical steps so the strap never breaks into stairs with gaps
    if x > ax and math.abs(ys[x] - ys[x - 1]) > 1 then
      local s = ys[x] > ys[x - 1] and 1 or -1
      for y = ys[x - 1] + s, ys[x] - s, s do put(g, x, y, col) end
    end
  end
  return ys
end

-- the four trinkets: each a char map hung by its top-centre
local TRINKET = {
  claw = { { "R.r", "R.r", "RRr", ".rd" }, { R = hex("f2603f"), r = hex("c8283a"), d = hex("861a3e") } },
  beetle = { { ".k.", "GgG", "gGs", ".s." }, { k = hex("3a3654"), G = hex("f5c04a"), g = hex("fff1a8"), s = hex("c7802a") } },
  bell = { { ".F.", "FfS", "FfS", "fSS", ".k." }, { F = FROST[1], f = FROST[2], S = FROST[3], k = FROST[4] } },
  egg = { { ".B.", "BbD", "bxd", ".d." }, { B = BONE[1], b = BONE[2], D = BONE[3], d = BONE[4], x = hex("d94ecb") } },
}
-- four hang columns spread over the strap, inset from both ends
local function spread(ax, bx)
  local out = {}
  for i, f in ipairs({ 0.14, 0.39, 0.64, 0.89 }) do out[i] = math.floor(ax + (bx - ax) * f + 0.5) end
  return out
end

local function hang(g, name, x, y, pal)
  local t = TRINKET[name]
  stamp(g, t[1], pal or t[2], x - 1, y)
end

-------------------------------------------------------------------------------------------
-- 1. Menagerie Horn (T2) - 24x22 art
do
  local g = newGrid(24, 22)
  local at = horn(g, {
    P0 = { 2.5, 7.5 }, P1 = { 11, 13 }, P2 = { 21.5, 3 }, hw0 = 1.15, hw1 = 4.2,
    ramp = { BONE[1], BONE[2], BONE[3], BONE[4] },
    bands = { { 0, 0.09, { GOLD[1], GOLD[2], GOLD[3], GOLD[4] } }, { 0.46, 0.52, { LEATHER[1], LEATHER[2], LEATHER[3], LEATHER[4] } },
              { 0.86, 0.9, { GOLD[1], GOLD[2], GOLD[3], GOLD[4] } } },
    mouth = BONE[5], rim = GOLD[1],
  })
  local ax, ay = at(0.06, -1)
  local bx, by = at(0.88, -1)
  local ys = strap(g, ax, ay + 1, bx, by + 1, 6, LEATHER[3])
  local xs = spread(ax, bx)
  local names = { "claw", "beetle", "bell", "egg" }
  for i, x in ipairs(xs) do
    if ys[x] then hang(g, names[i], x, ys[x] + 1) end
  end
  withOutline(g)
  save(g, IDIR .. "MenagerieHorn.png")
end

-- 2. Wild Court (T4) - 26x24 art
do
  local g = newGrid(26, 24)
  local at = horn(g, {
    P0 = { 2.5, 9.5 }, P1 = { 12, 15 }, P2 = { 23.5, 4 }, hw0 = 1.25, hw1 = 4.6,
    ramp = { VOID[1], VOID[2], VOID[3], VOID[4] },
    bands = { { 0, 0.08, THREAD }, { 0.3, 0.34, THREAD }, { 0.6, 0.64, THREAD }, { 0.87, 0.91, THREAD } },
    mouth = VOID[5], rim = THREAD[1],
  })
  -- the Warden Plate as a crest on the horn's back: a dense-stone shield, rift seam down it
  local cx, cy = at(0.47, 1.2)
  stamp(g, {
    ".aAAa.",
    "aAbbBa",
    "aAbrBa",
    ".aBrb.",
    "..ab..",
  }, { A = hex("baacc4"), a = hex("847696"), b = hex("584a6e"), B = hex("3b3050"), r = hex("ff2fb0") }, cx - 3, cy - 4)
  local ax, ay = at(0.06, -1)
  local bx, by = at(0.88, -1)
  local ys = strap(g, ax, ay + 1, bx, by + 1, 6, THREAD[3])
  local xs = spread(ax, bx)
  local names = { "claw", "beetle", "bell", "egg" }
  for i, x in ipairs(xs) do
    if ys[x] then hang(g, names[i], x, ys[x] + 1) end
  end
  -- three soul beads where the bell meets the strap
  local sx, sy = at(0.97, -1.05)
  put(g, sx + 1, sy + 1, SOULS[1]); put(g, sx + 2, sy + 2, SOULS[2]); put(g, sx + 1, sy + 3, SOULS[3])
  withOutline(g)
  save(g, IDIR .. "WildCourt.png")
end

-- 3. Grove Conclave (T5) - 28x26 art
do
  local g = newGrid(28, 26)
  local at = horn(g, {
    P0 = { 2.5, 10.5 }, P1 = { 13, 16 }, P2 = { 25, 4.5 }, hw0 = 1.3, hw1 = 5.0,
    ramp = { SILK[1], SILK[2], SILK[3], SILK[4] },
    bands = { { 0, 0.08, { SILK[3], SILK[4], SILK[5], FRAME[1] } }, { 0.88, 0.92, { SILK[3], SILK[4], SILK[5], FRAME[1] } } },
    mouth = FRAME[2], rim = SILK[1],
  })
  -- the jungle vine wound round it: a dark stem crossing the horn every so often, with leaves
  for i = 0, 40 do
    local t = 0.1 + i / 40 * 0.75
    local s = math.sin(t * 34) * 1.05
    if math.abs(s) < 1.0 then
      local x, y = at(t, s)
      put(g, x, y, s > 0 and LEAF[3] or LEAF[4])
    end
  end
  for _, t in ipairs({ 0.22, 0.48, 0.72 }) do
    local x, y = at(t, 1.2)
    stamp(g, { ".lL", "lL." }, { l = LEAF[2], L = LEAF[1] }, x - 1, y - 2)
  end
  -- the Sailcloth pennant, hung from under the bell: pale cloth, a dark frame stripe
  local px, py = at(0.93, -1)
  stamp(g, {
    "ss",
    "sSs",
    "sSSs",
    "sfSs",
    "sfs",
    "sf",
    "s",
  }, { s = SILK[2], S = SILK[1], f = SILK[4] }, px, py + 1)
  local ax, ay = at(0.06, -1)
  local ys = strap(g, ax, ay + 1, px - 2, py + 1, 6, SILK[4])
  local xs = spread(ax, px - 2)
  local names = { "claw", "beetle", "bell", "egg" }
  for i, x in ipairs(xs) do
    if ys[x] then hang(g, names[i], x, ys[x] + 1) end
  end
  withOutline(g, FRAME[2])
  save(g, IDIR .. "GroveConclave.png")
end

-- 4. The Ark (T6) - 30x28 art
do
  local g = newGrid(30, 28)
  local P = { PRIME[2], PRIME[3], PRIME[4], PRIME[5] }
  local at = horn(g, {
    P0 = { 2.5, 11.5 }, P1 = { 14, 18 }, P2 = { 27, 4.5 }, hw0 = 1.35, hw1 = 5.6,
    ramp = P,
    bands = { { 0, 0.07, { PRIME[4], PRIME[5], PRIME[6], PRIME[7] } }, { 0.4, 0.43, { PRIME[4], PRIME[5], PRIME[6], PRIME[7] } },
              { 0.7, 0.73, { PRIME[4], PRIME[5], PRIME[6], PRIME[7] } } },
    mouth = PRIME[7], rim = PRIME[1],
  })
  -- warm studs round the bell lip: the Keeper's regard, a few pixels only
  for _, s in ipairs({ 0.85, 0.15, -0.55 }) do
    local x, y = at(0.965, s)
    put(g, x, y, REGARD[1])
  end
  local ex, ey = at(0.56, 0.1)
  put(g, ex, ey, REGARD[2]); put(g, ex + 1, ey, REGARD[1])
  -- a stardust star caught in the throat
  local mx, my = at(1.0, 0)
  stamp(g, { ".w.", "wWw", ".w." }, { w = STAR[2], W = STAR[1] }, mx - 1, my - 1)
  -- the four trinkets cast as prime medallions: each keeps its own colour at the heart
  local ax, ay = at(0.05, -1)
  local bx, by = at(0.86, -1)
  local ys = strap(g, ax, ay + 1, bx, by + 1, 7, PRIME[5])
  local xs = spread(ax, bx)
  local hearts = { hex("f2603f"), hex("f5c04a"), FROST[2], hex("d94ecb") }
  for i, x in ipairs(xs) do
    if ys[x] then
      local y = ys[x] + 1
      stamp(g, { ".p.", "pHq", "qqr", ".r." }, { p = PRIME[2], q = PRIME[4], r = PRIME[5], H = hearts[i] }, x - 1, y)
    end
  end
  withOutline(g, PRIME_OUT)
  save(g, IDIR .. "TheArk.png")
end

-------------------------------------------------------------------------------------------
-- the beast sheet: 16x16 art per frame, facing left
local CRAB = { hex("ffb27a"), hex("f2603f"), hex("c8283a"), hex("861a3e") }
local GILD = { hex("fff1a8"), hex("f5c04a"), hex("c7802a"), hex("7a3e1e") }
local CHITIN = { hex("8fa3c4"), hex("5a5c7a"), hex("3a3654"), hex("241f36") }
local WING = { hex("eaf4ff"), hex("b9c6ee"), hex("8a92d0"), hex("5d5c9e") }
local STONEV = { hex("baacc4"), hex("847696"), hex("584a6e"), hex("3b3050") }
local frames = {}

local function crab(f)
  local g = newGrid(16, 16)
  -- legs first so the body sits over their roots; f 3 = airborne, legs spread
  for i = 0, 2 do
    local bx = 7 + i * 2
    if f == 3 then
      put(g, bx, 12, CRAB[4]); put(g, bx + (i - 1), 13, CRAB[4])
    else
      local o = ((i + f) % 2 == 0) and -1 or 0
      put(g, bx, 12, CRAB[4]); put(g, bx + o, 13, CRAB[4]); put(g, bx + o, 14, CRAB[3])
    end
  end
  local bob = (f == 1) and -1 or 0
  blob(g, 9.5, 9.5 + bob, 4.6, 3.0, CRAB)
  -- a ridge of lighter shell bumps on the back
  put(g, 8, 7 + bob, CRAB[1]); put(g, 11, 7 + bob, CRAB[1])
  -- eye stalks and eyes, leaning forward (left)
  for _, ex in ipairs({ 6, 8 }) do
    put(g, ex, 6 + bob, CRAB[3]); put(g, ex - 1, 5 + bob, BONE[1])
  end
  -- the big claw out front; raised and open while airborne / gripping
  local cy = (f == 3) and 5.5 or 8.5 + bob
  put(g, 5, math.floor(cy) + 1, CRAB[3]); put(g, 4, math.floor(cy) + 1, CRAB[3])
  blob(g, 2.8, cy, 2.0, 1.6, CRAB)
  clear(g, 1, math.floor(cy)); clear(g, 0, math.floor(cy))
  withOutline(g)
  return g
end

local function beetle(f)
  local g = newGrid(16, 16)
  local dive = f >= 2
  if not dive then
    for i = 0, 2 do
      local o = ((i + f) % 2 == 0) and 0 or 1
      put(g, 6 + i * 3, 12, CHITIN[3]); put(g, 6 + i * 3 - o, 13, CHITIN[4])
    end
    blob(g, 9.5, 9.0, 4.4, 3.0, GILD)
    blob(g, 4.2, 9.5, 1.8, 1.6, CHITIN)
    put(g, 3, 7, GILD[2]); put(g, 2, 6, GILD[1])           -- horn
    put(g, 3, 9, EYE)
    -- glass wings: up on 0, swept back on 1
    if f == 0 then blob(g, 11.5, 4.6, 3.3, 1.2, WING)
    else blob(g, 13.2, 6.6, 2.7, 1.1, WING) end
    put(g, 9, 9, GILD[4]); put(g, 10, 10, GILD[4])           -- elytra seam
  else
    -- folded and stretched along the dive
    blob(g, 9.0, 8.5, 5.2, 2.6, GILD)
    blob(g, 3.4, 8.5, 1.7, 1.5, CHITIN)
    put(g, 2, 7, GILD[1]); put(g, 1, 6 + (f - 2), GILD[2])
    put(g, 3, 8, EYE)
    for x = 7, 12 do put(g, x, 6, WING[f == 2 and 2 or 3]) end
    put(g, 13, 7, WING[3])
  end
  withOutline(g)
  return g
end

local function sprite(f)
  local g = newGrid(16, 16)
  local bob = (f == 1 or f == 2) and -1 or 0
  -- a tail of wisps trailing back and down, swaying
  local sway = ({ 0, 1, 1, 0 })[f + 1]
  put(g, 11, 11 + bob, FROST[3]); put(g, 12 + sway, 12 + bob, FROST[3]); put(g, 12 + sway, 13 + bob, FROST[4])
  put(g, 11 + sway, 14 + bob, FROST[4])
  blob(g, 8.0, 9.0 + bob, 3.8, 4.0, FROST)
  -- a crown of three ice spikes
  local spikes = { { 6, 4 }, { 8, 3 }, { 10, 4 } }
  for k, s in ipairs(spikes) do
    local x, y = s[1], s[2] + bob
    put(g, x, y + 1, FROST[2]); put(g, x, y, (k - 1 == f % 3) and hex("ffffff") or FROST[1])
    if k == 2 then put(g, x, y - 1, FROST[1]) end
  end
  put(g, 5, 9 + bob, EYE); put(g, 7, 9 + bob, EYE)
  withOutline(g)
  return g
end

local function hatchling(f)
  local g = newGrid(16, 16)
  local spit = f >= 2
  local bob = (f == 1) and -1 or 0
  local hx = spit and 7.0 or 8.0
  -- the creature: a stone-violet head with nubs, one big rift eye facing left
  blob(g, hx, 7.5 + bob, 3.4, 3.0, STONEV)
  put(g, math.floor(hx) + 1, 4 + bob, STONEV[3]); put(g, math.floor(hx) + 3, 5 + bob, STONEV[3])
  local ex = math.floor(hx) - 2
  put(g, ex, 7 + bob, hex("fff2fb")); put(g, ex + 1, 7 + bob, hex("d94ecb")); put(g, ex, 6 + bob, hex("ff9ad9"))
  if spit then
    put(g, ex - 1, 9, hex("ff9ad9")); put(g, ex, 9, hex("fff2fb"))
    if f == 3 then put(g, ex - 2, 9, hex("d94ecb")) end
  end
  -- the half eggshell it sits in, the broken rim zigzagging
  blob(g, 8.5, 11.5, 4.6, 3.2, { BONE[1], BONE[2], BONE[3], BONE[4] }, function(x, y) return y >= 10 end)
  for x = 4, 13 do
    if x % 2 == 0 then put(g, x, 9, BONE[2]) end
  end
  put(g, 10, 12, hex("d94ecb"))                               -- a rift crack in the shell
  withOutline(g)
  return g
end

for f = 0, 3 do frames[#frames + 1] = crab(f) end
for f = 0, 3 do frames[#frames + 1] = beetle(f) end
for f = 0, 3 do frames[#frames + 1] = sprite(f) end
for f = 0, 3 do frames[#frames + 1] = hatchling(f) end
saveFixed(frames, 16, 16, PDIR .. "MenagerieBeast.png")

-- the hatchling's spit, and the Ark's volley bolt drawn from it: a rift glob, 6x6 art
do
  local g = newGrid(6, 6)
  blob(g, 3, 3, 2.2, 2.2, { hex("ffffff"), hex("ff9ad9"), hex("d94ecb"), hex("8a2f9c") })
  withOutline(g)
  saveFixed({ g }, 6, 6, PDIR .. "MenagerieSpit.png")
end

-------------------------------------------------------------------------------------------
-- buff icons, 16x16 art (32x32)
do
  -- Menagerie Horn: a small ivory horn with one hanging claw
  local g = newGrid(16, 16)
  local at = horn(g, { P0 = { 2.5, 6.5 }, P1 = { 7, 11 }, P2 = { 13.5, 3 }, hw0 = 1.0, hw1 = 2.9,
    ramp = { BONE[1], BONE[2], BONE[3], BONE[4] }, bands = { { 0, 0.12, { GOLD[1], GOLD[2], GOLD[3], GOLD[4] } } },
    mouth = BONE[5], rim = GOLD[1] })
  local x, y = at(0.5, -1)
  put(g, x, y + 1, LEATHER[3]); put(g, x, y + 2, LEATHER[3])
  hang(g, "claw", x, y + 3)
  withOutline(g)
  saveFixed({ g }, 16, 16, BDIR .. "MenagerieHornBuff.png")
end

do
  -- Wild Court: a void crown with four thread-set gems, one per beast
  local g = newGrid(16, 16)
  stamp(g, {
    "a....a....a",
    "aA..aAa..Aa",
    "aAA.aAa.AAa",
    "aAAAAAAAAAa",
    "bBBBBBBBBBb",
    "bcBdBeBfBBb",
    "bBBBBBBBBBb",
  }, { A = VOID[1], a = VOID[2], B = VOID[3], b = VOID[4],
       c = hex("f2603f"), d = hex("f5c04a"), e = FROST[2], f = hex("d94ecb") }, 2, 4)
  for x = 3, 12 do put(g, x, 11, THREAD[3]) end
  withOutline(g)
  saveFixed({ g }, 16, 16, BDIR .. "WildCourtBuff.png")
end

do
  -- Grove Conclave: a ring of four leaves round a silk core, turning
  local g = newGrid(16, 16)
  blob(g, 8, 8, 2.2, 2.2, { SILK[1], SILK[2], SILK[3], SILK[4] })
  stamp(g, { ".L.", "LlL", "lll", ".l." }, { L = LEAF[1], l = LEAF[2] }, 7, 1)
  stamp(g, { ".l.", "lll", "LlL", ".L." }, { L = LEAF[2], l = LEAF[3] }, 7, 11)
  stamp(g, { ".Ll.", "Llll", ".ll." }, { L = LEAF[1], l = LEAF[2] }, 1, 7)
  stamp(g, { ".ll.", "llll", ".lL." }, { L = LEAF[2], l = LEAF[3] }, 11, 7)
  withOutline(g, FRAME[2])
  saveFixed({ g }, 16, 16, BDIR .. "GroveConclaveBuff.png")
end

do
  -- The Ark: a prime hull with a warm-eyed prow and a star over the deck
  local g = newGrid(16, 16)
  stamp(g, {
    "..........w...",
    ".........wWw..",
    "..........w...",
    "....m...m.....",
    "....m...m.....",
    "AAAAAAAAAAAAAA",
    ".BBBBBBBBBBBr.",
    "..CCCCCCCCCC..",
    "...DDDDDDDD...",
  }, { w = STAR[2], W = STAR[1], m = PRIME[5], A = PRIME[2], B = PRIME[3], C = PRIME[4], D = PRIME[5], r = REGARD[2] }, 1, 3)
  withOutline(g, PRIME_OUT)
  saveFixed({ g }, 16, 16, BDIR .. "ArkBuff.png")
end
