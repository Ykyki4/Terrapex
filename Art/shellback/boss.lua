-- The Old Shellback: a hermit crab wearing a sunken ship's hull. Run from the repo root:
--
--   aseprite -b --script-param root=<repo> --script Art/shellback/boss.lua
--
-- 2x2, 60x44 art cells per frame (120x88 px), facing right. Twelve frames stacked:
--   0-3 walking, 4-7 walking with the mast snapped (phase two), 8/9 in the shell,
--   10/11 stunned. The code in OldShellback.cs reads exactly this order.
--
-- The hull is a region (an upturned keel arc over a gunwale that rises to a pointed
-- bow at the back), lit by its own geometry: strakes run parallel to the keel, each
-- with a lit upper edge and a dark seam, and the right side - away from the light -
-- drops a step. The crab, the claw, the cannon and the barnacles are hand maps.

dofile(app.params.root .. "/Art/shellback/sea.lua")

local W, H = 60, 44
local CX = 23

local function hullTop(x)
  local t = (x - CX) / 21.5
  if math.abs(t) >= 1 then return 99 end
  return 33 - 23 * math.sqrt(1 - t * t)
end
local function hullBot(x)
  if x >= 14 then return 33 end
  return 33 - (14 - x)
end
local JAG = { [0] = 0, -2, 1, -1, 2, -3, 0, 1 }
local function hullRight(y) return 40 + JAG[y % 8] end

local function wood(k, x)
  -- step darker on the side away from the light
  local far = x >= 31
  if k == "L" then return far and WOODP.B or WOODP.L end
  if k == "B" then return (x >= 37) and WOODP.D or WOODP.B end
  return WOODP[k]
end

local function drawHull(g, dy, broken)
  for x = 3, 44 do
    local top, bot = hullTop(x), hullBot(x)
    for y = math.floor(top), bot do
      if y + 0.5 >= top and x <= hullRight(y) then
        local d = y + 0.5 - top
        local col
        if y == bot then col = WOODP.S
        elseif y == bot - 1 then col = wood("L", x)            -- the gunwale rail
        elseif d < 1.2 then col = WOODP.D                       -- the keel
        else
          local k = math.floor(d - 1.2) % 4
          col = (k == 0) and wood("L", x) or ((k == 3) and WOODP.D or wood("B", x))
        end
        if x == hullRight(y) then col = WOODP.S end             -- broken plank ends
        put(g, x, y + dy, col)
      end
    end
  end
  -- a stove-in plank showing the dark inside
  for y = 21, 23 do for x = 25, 28 do if get(g, x, y + dy) then put(g, x, y + dy, WOODP.K) end end end
  put(g, 25, 21 + dy, WOODP.D); put(g, 28, 23 + dy, WOODP.D)
  -- barnacle clusters
  local B = { W = BARN.W, G = BARN.G, g = BARN.g, d = BARN.d }
  overpaint(g, { "WG.", "Gdg", ".gg" }, B, 15, 17 + dy)
  overpaint(g, { ".WG", "WGd", "Ggg" }, B, 30, 14 + dy)
  overpaint(g, { "WGW", "gdg" }, B, 9, 27 + dy)
  overpaint(g, { "WG", "gd" }, B, 34, 25 + dy)
  overpaint(g, { "WGW.", "GdGd", ".gg." }, B, 19, 28 + dy)
  overpaint(g, { "WG", "gd" }, B, 23, 13 + dy)
  -- a little coral, the one warm accent on the wood
  overpaint(g, { "L.L", "BLB", ".D." }, CORAL, 37, 18 + dy)
  -- kelp hanging off the rail
  for _, k in ipairs({ { 7, 2 }, { 16, 3 }, { 29, 2 } }) do
    for i = 1, k[2] do put(g, k[1] + (i % 2), hullBot(k[1]) + i + dy, i == 1 and KELP.L or KELP.B) end
  end
  -- the figurehead on the bow: a carved gilt face looking back
  stamp(g, { ".KK", "KBL", "BLB", ".BK", ".K." }, GOLDP, 1, 18 + dy)
  if broken then
    -- the phase-two cracks, dark splits across the strakes
    overpaint(g, { "K....", ".K...", ".KK..", "...K.", "...KK", "....K" }, { K = WOODP.K }, 11, 19 + dy)
    overpaint(g, { "..K", ".K.", "KK.", "K..", ".K." }, { K = WOODP.K }, 31, 20 + dy)
  end
end

local function drawMast(g, dy, broken)
  if broken then
    haft(g, 22.5, 14.5 + dy, 19.5, 8.5 + dy, 1.0, WOODP.L, WOODP.B, WOODP.D)
    clear(g, 19, 8 + dy); clear(g, 21, 8 + dy)
    put(g, 20, 7 + dy, WOODP.L)
    return
  end
  haft(g, 22.5, 14.5 + dy, 15.5, 1.5 + dy, 1.0, WOODP.L, WOODP.B, WOODP.D)
  -- a yard across it and a torn scrap of sail hanging off it
  line(g, 13, 5 + dy, 21, 3 + dy, WOODP.D)
  stamp(g, {
    "LLLLLB...",
    ".LLBBBB..",
    ".LBBBBD..",
    "..LBBD.B.",
    "..BB.D.D.",
    "..B......",
    "..D......",
  }, CANVAS, 12, 5 + dy)
end

local function drawCannon(g, dy)
  stamp(g, {
    "KL....",
    "LBL...",
    ".DBL..",
    "..DBLw",
    "...wWw",
  }, { K = IRONS.K, L = IRONS.L, B = IRONS.B, D = IRONS.D, w = WOODP.D, W = WOODP.B }, 7, 10 + dy)
  put(g, 9, 12 + dy, RUSTS.B)
end

local function drawEye(g, x, y)
  stamp(g, { "yyy", "yKY", "yyy" }, { y = EYEY, K = PUPIL, Y = hex("ffffff") }, x - 1, y - 1)
end

-- the big claw, open: the upper finger curls down, the lower one up, and the
-- three-row gap between them stays clear of the outline so the pincer reads
local CLAW = {
  "..LLLL.........",
  ".LBBBBLLL......",
  "LBBBBBBBBLLL...",
  "LBBBBBBBBBBBLL.",
  "BBBBBBBSSwSSwBD",
  "BBBBBBS........",
  "BBBBBS.........",
  "BBBBBS.........",
  "SBBBBBS........",
  "SBBBBBBSwSSD...",
  ".SSBBBBBBBBSD..",
  "..DDSSSSSD.....",
}
local SMALLCLAW = {
  ".LBBL..",
  "LBBBBSD",
  "BBS....",
  "SBBBSD.",
  ".DD....",
}
local CLAWPAL = { L = CRAB.L, B = CRAB.B, S = CRAB.S, D = CRAB.D, w = BONE.L }

local function drawCrab(g, dy, f, stunned)
  -- the body coming out of the broken stern: a carapace lit by its own normal
  local cx, cy, rx, ry = 43, 27 + dy, 8, 8
  for y = cy - ry, cy + ry do
    for x = 35, cx + rx do
      local nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
      if nx * nx + ny * ny <= 1 then
        local n = nx + ny
        local col = n < -0.75 and CRAB.L or (n < 0.35 and CRAB.B or (n < 0.95 and CRAB.S or CRAB.D))
        put(g, x, y, col)
      end
    end
  end
  -- pale speckles on the shell, and the mandibles
  put(g, 40, 23 + dy, BONE.L); put(g, 38, 26 + dy, BONE.L); put(g, 42, 21 + dy, CRAB.L)
  stamp(g, { "Kw", "wK", "Kw" }, { K = CRAB.K, w = BONE.B }, 50, 26 + dy)

  -- eye stalks, set back so the claw has the front to itself
  local sway = (f % 2 == 0) and 0 or 1
  if stunned then
    -- stalks flopped forward, eyes half shut
    line(g, 41, 20 + dy, 45, 17 + dy, CRAB.S)
    line(g, 44, 21 + dy, 49, 19 + dy, CRAB.S)
    stamp(g, { "yyy", "DDD" }, { y = EYEY, D = CRAB.D }, 45, 15 + dy)
    stamp(g, { "yyy", "DDD" }, { y = EYEY, D = CRAB.D }, 49, 17 + dy)
  else
    line(g, 40, 20 + dy, 40, 13 + dy, CRAB.S)
    line(g, 43, 20 + dy, 44 + sway, 12 + dy, CRAB.S)
    drawEye(g, 40, 12 + dy)
    drawEye(g, 44 + sway, 11 + dy)
  end

  -- the claws: the big one raised in front, open; the small one low
  if stunned then
    stamp(g, CLAW, CLAWPAL, 44, 32)
    stamp(g, SMALLCLAW, CLAWPAL, 38, 37)
  else
    local lift = (f % 2 == 1) and -1 or 0
    haft(g, 46.5, 27.5 + dy, 48.5, 22.5 + dy + lift, 1.3, CRAB.L, CRAB.B, CRAB.S)
    stamp(g, CLAW, CLAWPAL, 45, 13 + dy + lift)
    stamp(g, SMALLCLAW, CLAWPAL, 46, 31 + dy)
  end
end

-- legs: attach under the body, a knee out and up, a pointed tip on the sand.
-- s is the side (1 forward, -1 back); the walk swaps which legs are lifted.
local LEGS = { { 12, -1 }, { 18, -1 }, { 24, -1 }, { 36, 1 }, { 40, 1 }, { 44, 1 } }
local function drawLegs(g, dy, f, mode)
  for i, l in ipairs(LEGS) do
    local ax, s = l[1], l[2]
    local lifted = mode == "walk" and ((i + f) % 2 == 0)
    local ay = 33 + dy
    local kx, ky, tx, ty
    if mode == "stun" then
      kx, ky, tx, ty = ax + s * 4, 38, ax + s * 7, 43
    elseif mode == "hide" then
      kx, ky, tx, ty = ax + s * 1, 42, ax + s * 2, 43
    else
      kx, ky = ax + s * 3, 36 + dy - (lifted and 1 or 0)
      tx, ty = ax + s * 5 + (lifted and s or 0), lifted and 42 or 43
    end
    if mode ~= "hide" then
      line(g, ax, ay, kx, ky, CRAB.B)
      line(g, ax + 1, ay, kx + 1, ky, CRAB.S)
    end
    line(g, kx, ky, tx, ty, CRAB.S)
    put(g, kx, ky, CRAB.L)
    put(g, tx, ty, CRAB.D)
  end
end

local function frame(kind, f, broken)
  local g = newGrid(W, H)
  if kind == "walk" then
    local dy = (f == 2 or f == 4) and -1 or 0
    drawLegs(g, dy, f, "walk")
    drawMast(g, dy, broken)
    drawHull(g, dy, broken)
    drawCannon(g, dy)
    drawCrab(g, dy, f, false)
  elseif kind == "hide" then
    -- all the way into the wreck: the hull sits on the sand, two eyes in the dark stern
    local dy = 9
    drawLegs(g, 0, 1, "hide")
    drawMast(g, dy, broken)
    drawHull(g, dy, broken)
    drawCannon(g, dy)
    for y = 20, 32 do
      local r = hullRight(y)
      for x = r - 3, r - 1 do if get(g, x, y + dy) then put(g, x, y + dy, WOODP.K) end end
    end
    put(g, 37, 27 + dy, EYEY); put(g, 39, 27 + dy, EYEY)
  else
    -- stunned: the hull slumped, legs splayed flat, claws on the sand, eyes crossed
    local dy = 3
    drawLegs(g, dy, 1, "stun")
    drawMast(g, dy, broken)
    drawHull(g, dy, true)
    drawCannon(g, dy)
    drawCrab(g, dy, 1, true)
  end
  outline(g)
  return g
end

local FRAMES = {}
for f = 1, 4 do FRAMES[#FRAMES + 1] = { "walk", f, false } end
for f = 1, 4 do FRAMES[#FRAMES + 1] = { "walk", f, true } end
FRAMES[#FRAMES + 1] = { "hide", 1, false }
FRAMES[#FRAMES + 1] = { "hide", 1, true }
FRAMES[#FRAMES + 1] = { "stun", 1, false }
FRAMES[#FRAMES + 1] = { "stun", 1, true }

sheet("Content/NPCs/Bosses/OldShellback.png", W, H, #FRAMES, function(i)
  local fr = FRAMES[i]
  return frame(fr[1], fr[2], fr[3])
end)

-- the map icon: the crab's face out of the stern, eyes and the big claw, 30x30
do
  local g = newGrid(15, 15)
  -- a corner of hull behind
  for y = 4, 13 do for x = 0, 5 - math.floor((13 - y) / 3) do
    put(g, x, y, (y % 3 == 0) and WOODP.L or ((y % 3 == 2) and WOODP.D or WOODP.B))
  end end
  put(g, 1, 6, BARN.W); put(g, 2, 6, BARN.G); put(g, 1, 7, BARN.g)
  for y = 6, 13 do for x = 3, 11 do
    local nx, ny = (x + 0.5 - 7) / 4.5, (y + 0.5 - 10) / 4
    if nx * nx + ny * ny <= 1 then
      local n = nx + ny
      put(g, x, y, n < -0.75 and CRAB.L or (n < 0.35 and CRAB.B or (n < 0.95 and CRAB.S or CRAB.D)))
    end
  end end
  line(g, 6, 6, 6, 3, CRAB.S); line(g, 9, 6, 9, 2, CRAB.S)
  drawEye(g, 6, 2); drawEye(g, 10, 1)
  stamp(g, { ".LLL.", "LBBBL", "BBSwS", "SBww.", ".DS.." }, CLAWPAL, 10, 7)
  outline(g)
  icon("Content/NPCs/Bosses/OldShellback_Head_Boss.png", 15, 15, g)
end

print("shellback boss: done")
