-- The Scarab Sultan, its sun, its map icon. Run from the repo root:
--
--   aseprite -b --script-param root=<repo> --script Art/scarab/sultan.lua
--
-- Side view, facing right, 64x56 art cells per frame (128x112 px), four frames of the
-- hind wings beating; the elytra stay shut, which is how real flower scarabs fly. The
-- shell is a dome lit by its own normal with a lapis band following its outline - the
-- jewelled-scarab amulet, grown huge. The crown on the pronotum carries the jewel that
-- flashes before the beetling fan (code: NPC.Center + (27, -11) px = art (45, 22)).

dofile(app.params.root .. "/Art/scarab/slib.lua")

local FW, FH = 64, 56

-- the elytron: dome centre and radii, clipped flat at the belly
local EX, EY, ERX, ERY = 26, 32, 20, 15
local function inElytron(x, y) return y <= 38 end
-- the pronotum: lower than the shell and darker, so the profile steps down to the head
local PX, PY, PRX, PRY = 45, 33.5, 9, 9.5

local GOLD5 = { GOLDR[1], GOLDR[2], GOLDR[3], GOLDR[4], GOLDR[5] }
local GOLDD = { GOLDR[2], GOLDR[3], GOLDR[4], GOLDR[5], GOLDR[6] }

local function isGold(c)
  for i = 1, 6 do if c == GOLDR[i] then return i end end
  return nil
end

local function wing(g, f)
  -- the hind wing: a long membrane from under the front of the elytron, at an angle per frame
  local ang = ({ 104, 132, 166, 140 })[f]
  local len = ({ 28, 27, 27, 22 })[f]
  local rx, ry = 34, 27
  local a = math.rad(ang)
  local ux, uy = math.cos(a), -math.sin(a)       -- along the wing
  local vx, vy = -uy, ux                         -- across
  if vx * 1 + vy * 0 < 0 then vx, vy = -vx, -vy end
  local pts = {}
  -- leading edge straight, trailing edge bellied
  local prof = { { 0, -1.5, 1.5 }, { 0.3, -2, 6 }, { 0.62, -2.2, 8.5 }, { 0.88, -1.5, 6 }, { 1, 0, 1.5 } }
  for _, p in ipairs(prof) do pts[#pts + 1] = { rx + ux * len * p[1] + vx * p[2], ry + uy * len * p[1] + vy * p[2] } end
  for i = #prof, 1, -1 do
    local p = prof[i]
    pts[#pts + 1] = { rx + ux * len * p[1] + vx * p[3], ry + uy * len * p[1] + vy * p[3] }
  end
  poly(g, pts, function(x, y)
    local px, py = x + 0.5 - rx, y + 0.5 - ry
    local along, across = px * ux + py * uy, px * vx + py * vy
    local t = along / len
    if across < -1.0 then return WINGR[4] end                 -- the costal vein, dark and stiff
    if math.abs(across - (0.8 + 4.2 * t)) < 0.6 and t > 0.15 and t < 0.85 then return WINGR[3] end
    if math.abs(across - (2.5 + 1.2 * t)) < 0.6 and t > 0.4 and t < 0.8 then return WINGR[3] end
    if t > 0.8 then return WINGR[3] end
    return across < 2.5 and WINGR[1] or WINGR[2]
  end)
end

local function body(g)
  -- belly under the shell: bronze segments
  for y = 37, 40 do
    for x = 12, 42 do
      local l = 13 + (y - 37) * 2
      local r = 42 - (y - 37) * 1.5
      if x >= l and x <= r then
        put(g, x, y, (x % 4 == 0) and BRONZE[4] or BRONZE[3])
      end
    end
  end

  -- the elytron
  dome(g, EX, EY, ERX, ERY, GOLD5, inElytron)
  for _, c in ipairs(cells(g)) do
    local x, y = c[1], c[2]
    local r, nx, ny = erad(x, y, EX, EY, ERX, ERY)
    if r <= 1 and y <= 38 and isGold(c[3]) then
      local lit = (-0.55 * nx - 0.75 * ny) > 0.1
      local k = isGold(c[3])
      if r > 0.68 and r < 0.82 then
        -- the lapis band, following the shell's outline, with turquoise studs set in it
        local s = math.floor(math.deg(math.atan(ny, nx)) / 11)
        if r > 0.73 and r < 0.78 and s % 2 == 0 and ny < 0.25 then put(g, x, y, lit and TURQ[1] or TURQ[2])
        else put(g, x, y, lit and LAPIS[2] or LAPIS[3]) end
      elseif r > 0.82 and r < 0.89 then
        put(g, x, y, lit and GOLDR[2] or GOLDR[4])                -- a gold rim outside the band
      elseif ((r > 0.42 and r < 0.49) or (r > 0.2 and r < 0.27)) and ny < -0.15 and math.abs(nx) < 0.75 then
        put(g, x, y, GOLDR[math.min(6, k + 1)])                   -- the striae, arcs parallel to the back
      end
    end
  end
  for x = 6, 46 do
    if get(g, x, 38) and isGold(get(g, x, 38)) then put(g, x, 38, GOLDR[6]) end
  end

  -- the pronotum: a darker shield in front, its leading rim lapis
  dome(g, PX, PY, PRX, PRY, GOLDD, function(x, y) return y <= 38 end)
  for y = 23, 38 do
    for x = 36, 55 do
      local r, nx, ny = erad(x, y, PX, PY, PRX, PRY)
      if r <= 1 and y <= 38 and r > 0.76 and nx > 0.15 then put(g, x, y, (ny < 0) and LAPIS[2] or LAPIS[3]) end
    end
  end

  -- the head, low at the front, with the rake of the clypeus
  dome(g, 54.5, 35.5, 4.5, 4, { GOLDR[2], GOLDR[3], GOLDR[3], GOLDR[4], GOLDR[5] })
  stamp(g, {
    "..ooo",
    ".oooo",
    "oo.o.",
    "o..o.",
  }, { o = GOLDR[4] }, 56, 37)
  -- the eye: a big orange iris, a white glint
  stamp(g, { "eY", "Ee" }, { e = SUNR[4], E = SUNR[5], Y = SUNR[1] }, 54, 34)
  -- the antenna: a thin stalk and a fanned club
  stamp(g, {
    "...TTt",
    "..Tt..",
    ".b....",
    "b.....",
  }, { T = SUNR[3], t = SUNR[4], b = BRONZE[2] }, 57, 29)

  -- the crown, seated on the pronotum: a band with three points and the jewel
  stamp(g, {
    "y.....y.....y",
    "yy...yGy...yy",
    "GyG.yGGGy.GyG",
    "GGGGGGJGGGGGG",
    "gTgLgJjJgLgTg",
    "ggggggggggggg",
  }, { y = GOLDR[1], G = GOLDR[2], g = GOLDR[4], T = TURQ[2], L = LAPIS[2], J = SUNR[4], j = SUNR[5] }, 39, 19)
  put(g, 45, 22, SUNR[1]) -- the jewel's glint
end

local function legs(g)
  local lit, mid, dark = BRONZE[1], BRONZE[2], BRONZE[3]
  -- front leg: forward, then the broad toothed tibia hooks back round the front of the sun
  limb(g, 50, 38, 55, 44, 1.6, lit, mid, dark)
  limb(g, 55, 44, 47, 50, 1.5, lit, mid, dark)
  stamp(g, { "d.", ".d", "d." }, { d = BRONZE[4] }, 56, 46)
  -- middle leg: down onto the top of the sun
  limb(g, 38, 39, 42, 43, 1.5, lit, mid, dark)
  limb(g, 42, 43, 43, 46, 1.2, lit, mid, dark)
  -- hind leg: back, then hooking round the back of the sun
  limb(g, 19, 38, 14, 45, 1.6, lit, mid, dark)
  limb(g, 14, 45, 23, 51, 1.5, lit, mid, dark)
  stamp(g, { ".d", "d.", ".d" }, { d = BRONZE[4] }, 11, 45)
end

local function frame(f)
  local g = newGrid(FW, FH)
  wing(g, f)
  -- the body is drawn over a fresh grid so the wing stays behind it
  local b = newGrid(FW, FH)
  body(b)
  legs(b)
  for _, c in ipairs(cells(b)) do put(g, c[1], c[2], c[3]) end
  outline(g)
  return g
end

sheet("Content/NPCs/Bosses/ScarabSultan.png", 4, FW, FH, frame)

-- the map icon: the crowned head-on face of the scarab, 15x15 art = 30x30
do
  local g = newGrid(15, 15)
  stamp(g, {
    "..y...y...y....",
    "..yy.yGy.yy....",
    "..GGGGJGGGG....",
    "..gTgLjLgTg....",
    ".GGGGGGGGGGG...",
    "GGyyyGGGGGGGg..",
    "GyyGGGGGGGGgg..",
    "GGGGLLLLLGGgg..",
    "GeYGLTLTLGeYg..",
    "gEegLLLLLgEeg..",
    ".gggggggggggg..",
    "..o.o...o.o....",
  }, { y = GOLDR[1], G = GOLDR[2], g = GOLDR[4], T = TURQ[2], L = LAPIS[2], J = SUNR[4], j = SUNR[5],
       e = SUNR[4], E = SUNR[5], Y = SUNR[1], o = GOLDR[4] }, 1, 1)
  outline(g)
  fixed("Content/NPCs/Bosses/ScarabSultan_Head_Boss.png", 15, 15, g)
end

-- the sun: three 48x48 frames (24x24 art), one per kind of ball
do
  local function sunFrame(kind)
    local g = newGrid(24, 24)
    if kind == 1 then
      -- the sun ball: packed glowing sand, hot core showing through swirls
      dome(g, 12, 12, 10.5, 10.5, { SUNR[1], SUNR[2], SUNR[3], SUNR[4], SUNR[5] })
      -- sand swirls wrapped round it: two bands of tan following a spiral
      for _, c in ipairs(cells(g)) do
        local x, y = c[1], c[2]
        local dx, dy = x + 0.5 - 12, y + 0.5 - 12
        local r = math.sqrt(dx * dx + dy * dy)
        local a = math.atan(dy, dx)
        local s = (a / (2 * math.pi) * 3 + r / 4.2) % 1
        if r > 3.5 and s < 0.28 then
          local lit = (-0.55 * dx - 0.75 * dy) > 0
          put(g, x, y, lit and SANDR[2] or SANDR[4])
        end
      end
    elseif kind == 2 then
      -- the cracked sun: smaller, white-hot, split by dark cracks
      dome(g, 12, 12, 8, 8, { SUNR[1], SUNR[2], SUNR[3], SUNR[4], SUNR[5] })
      stamp(g, {
        "....c....",
        "....c..c.",
        ".c..cc.c.",
        "..cc..cc.",
        "....cc...",
        "...c..c..",
        "..c....cc",
        "..c......",
      }, { c = SUNR[6] }, 7, 7)
      for _, p in ipairs({ { 12, 2 }, { 3, 13 }, { 20, 9 }, { 15, 21 } }) do put(g, p[1], p[2], SUNR[3]) end -- loose flecks
    else
      -- the Sand Roller's ball: dull packed sand, no light of its own
      dome(g, 12, 12, 8.5, 8.5, { SANDR[1], SANDR[2], SANDR[3], SANDR[4], SANDR[5] })
      for _, p in ipairs({ { 9, 8 }, { 14, 11 }, { 10, 15 }, { 16, 16 }, { 7, 12 } }) do put(g, p[1], p[2], SANDR[5]) end
      for _, p in ipairs({ { 8, 7 }, { 12, 6 } }) do put(g, p[1], p[2], SANDR[1]) end
    end
    outline(g)
    return g
  end
  sheet("Content/Projectiles/Scarab/SunBall.png", 3, 24, 24, sunFrame)
end

print("sultan: done")
