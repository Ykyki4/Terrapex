-- The Rime: the boss sheet, its map icon and the ice block it carries. Run:
--
--   aseprite -b --script-param root=<repo> --script Art/rime/rime_boss.lua
--
-- A hunched frost giant drawn on the Calamity grid: 64x64 art cells per frame,
-- blitted 2x2 into 128x128. Seven frames stacked vertically:
--   0-3 walk, 4 arms overhead (the block raised), 5 slam (crouched, fists down),
--   6 roar (head up, arms flung out).
-- The block is NOT in the sheet - the code draws RimeBoulder behind the body at the
-- hand anchor of the frame, so it can leave the hands when it is thrown.
--
-- The big plain masses (the back, the belly, the arms) are lit ellipses and
-- capsules against a top-left light, quantised hard; the parts that say what the
-- thing is - the horned head with its icicle beard, the glacier ridge on its back,
-- the red wrap - are hand-authored maps.

dofile(app.params.root .. "/Art/rime/rlib.lua")

local TORSO = { SNOW[1], SNOW[2], SNOW[3], SNOW[4], SNOW[5] }
local FIST_FRONT = { ICE[1], ICE[2], ICE[3], ICE[4], ICE[5] }
-- the body's bands sit low: most of a lit giant is in its own shadow
BANDS = { 0.62, 0.30, -0.05, -0.45 }
local FIST_BACK = { ICE[3], ICE[4], ICE[5], ICE[6], ICE[6] }

local HEAD = {
  "hh......................",
  "Hhhg.....sSSSSs.........",
  ".HHhhg.ssSWWWWWSSs......",
  "..HHhhgsSWWWWWWWSSs.....",
  "...HHhhgSWWWWWSSSSSs....",
  "....hHhgsSSSSSSSSSSSs...",
  ".....ghgdsSSSSSSWWWWWS..",
  "......gddssSSWWWWWWWWWS.",
  "......dddsssSSSSSSSSSssd",
  "......dDDdkkkkkkkddDdDd.",
  "......dDDdkkEYYEkkdssSd.",
  "......ddDDdkkeekkdsSSSs.",
  ".......dDDDddkkddsSSWSSs",
  "........DDdddddddsSSSSsd",
  "........dDdIiiSSSWSSSdd.",
  ".........dIiiiiIiiiIiic.",
  ".........IiicCiiicCiiCC.",
  "..........icCBiicCBicCB.",
  "..........iCB.icCB.iCB..",
  "..........CB..iCB..cB...",
  "...........B..cB...cB...",
  "..............CB...C....",
  "..............B....B....",
  "...............B........",
}

-- the roar: the moustache parts on a dark open mouth and the eye burns hotter
local ROAR = {}
for i, row in ipairs(HEAD) do ROAR[i] = row end
ROAR[11] = "......dDDdkYYYYYYkdssSd."
ROAR[15] = "........dDdIkkkkkkkSdd.."
ROAR[16] = ".........dIkkqqqqkkIiic."

-- a glacier shard jutting from the back: a tapered prism, lit face left, dark
-- face right, hard break down the middle
local function shard(g, bx, by, h, w, lean)
  for j = 0, h - 1 do
    local t = j / h
    local half = w * (1 - t) * 0.5
    local cx = bx + lean * j
    for x = math.floor(cx - half), math.floor(cx + half) do
      local col
      if j == h - 1 then col = ICE[1]
      elseif x < cx - 0.5 then col = (x <= cx - half + 0.5) and ICE[1] or ICE[2]
      elseif x < cx + 0.5 then col = ICE[3]
      else col = (x >= cx + half - 0.5) and ICE[5] or ICE[4] end
      put(g, x, by - j, col)
    end
  end
end

-- per-frame pose: body bob, leg feet, fists, head offset, which head
local POSES = {
  { bob = 0, bl = { 22, 58 }, fl = { 37, 58 }, bf = { 14, 54 }, be = { 17, 42 }, ff = { 52, 55 }, fe = { 48, 42 }, hx = 0, hy = 0 },
  { bob = -1, bl = { 24, 57 }, fl = { 35, 58 }, bf = { 17, 53 }, be = { 18, 41 }, ff = { 49, 56 }, fe = { 47, 43 }, hx = 0, hy = -1 },
  { bob = 0, bl = { 26, 58 }, fl = { 33, 58 }, bf = { 20, 54 }, be = { 19, 42 }, ff = { 46, 56 }, fe = { 46, 43 }, hx = 0, hy = 0 },
  { bob = -1, bl = { 24, 58 }, fl = { 35, 57 }, bf = { 17, 53 }, be = { 18, 41 }, ff = { 49, 54 }, fe = { 48, 42 }, hx = 0, hy = -1 },
  -- overhead: fists together above the head, where the block sits
  { bob = -2, bl = { 22, 58 }, fl = { 38, 58 }, bf = { 31, 7 }, be = { 22, 17 }, ff = { 40, 6 }, fe = { 47, 16 }, hx = -1, hy = 0 },
  -- slam: crouched, both fists driven into the floor ahead
  { bob = 4, bl = { 20, 58 }, fl = { 39, 58 }, bf = { 47, 58 }, be = { 36, 46 }, ff = { 56, 58 }, fe = { 50, 44 }, hx = 1, hy = 3 },
  -- roar: head thrown up, arms flung wide
  { bob = -1, bl = { 21, 58 }, fl = { 39, 58 }, bf = { 5, 21 }, be = { 14, 27 }, ff = { 57, 17 }, fe = { 51, 28 }, hx = 0, hy = -3, roar = true },
}

local function frame(f)
  local P = POSES[f]
  local g = newGrid(64, 64)
  local b = P.bob

  -- far side first: the back arm and back leg sit in shadow
  capsule(g, 24, 30 + b, P.be[1], P.be[2], 5, SNOW[4], SNOW[5], SNOW[6])
  capsule(g, P.be[1], P.be[2], P.bf[1], P.bf[2] - 2, 4.2, SNOW[4], SNOW[5], SNOW[6])
  capsule(g, P.be[1], P.be[2], P.bf[1], P.bf[2] - 2, 2.6, ICE[4], ICE[5], ICE[6])
  ellipse(g, P.bf[1], P.bf[2] - 1, 5, 4.4, FIST_BACK)
  capsule(g, 24, 45 + b, P.bl[1], P.bl[2] - 3, 5.2, SNOW[4], SNOW[5], SNOW[6])
  ellipse(g, P.bl[1] + 1, P.bl[2] - 1, 5.6, 2.6, { SNOW[4], SNOW[5], SNOW[6], SNOW[6] })

  -- the glacier ridge along the back, behind the hump's outline
  shard(g, 13, 24 + b, 9, 5, -0.35)
  shard(g, 19, 19 + b, 13, 6, -0.25)
  shard(g, 27, 17 + b, 10, 5, -0.1)

  -- the shoulders and the hips
  ellipse(g, 30, 44 + b, 13, 9, TORSO, BANDS)
  ellipse(g, 29, 30 + b, 19, 14, TORSO, BANDS)
  -- clumped snow: a few blots a step darker, fixed to the body so they ride with it
  for _, c in ipairs(cells(g)) do
    local x, y, col = c[1], c[2], c[3]
    if (col == SNOW[2] or col == SNOW[3]) and hash(x, y - b) > 0.9 then
      put(g, x, y, col == SNOW[2] and SNOW[3] or SNOW[4])
      if get(g, x + 1, y) == col then put(g, x + 1, y, col == SNOW[2] and SNOW[3] or SNOW[4]) end
    end
  end
  -- the overhang where the shoulders meet the hips
  for x = 17, 44 do
    local y = math.floor(43 + b + (x - 30) * (x - 30) * 0.012)
    if get(g, x, y) then put(g, x, y, SNOW[5]) end
    if get(g, x, y + 1) then put(g, x, y + 1, SNOW[4]) end
  end

  -- the near leg
  capsule(g, 37, 47 + b, P.fl[1], P.fl[2] - 3, 5.4, SNOW[2], SNOW[3], SNOW[4])
  ellipse(g, P.fl[1] + 1, P.fl[2] - 1, 5.8, 2.6, { SNOW[2], SNOW[3], SNOW[4], SNOW[5] })

  -- the red wrap round the neck, its tail streaming back over the shoulder
  local tail = (f % 2 == 0) and 1 or 0
  capsule(g, 36, 27 + b, 45, 31 + b, 2.6, RED[1], RED[2], RED[3])
  capsule(g, 36, 27 + b, 25, 22 + b - tail, 1.7, RED[1], RED[2], RED[3])
  put(g, 23, 23 + b - tail, RED[3])

  -- head
  stamp(g, P.roar and ROAR or HEAD, CH, 38 + P.hx, 17 + b + P.hy)
  -- the ibex horn: up off the crown and swept back, ridged
  stamp(g, {
    "..ghH.....",
    ".ghHH.....",
    "ghHg......",
    "Ghh.......",
    "GhH.......",
    ".GhH......",
    "..GhHh....",
    "...GgHhh..",
    ".....GghHh",
    ".......GhH",
  }, CH, 38 + P.hx + 1, 17 + b + P.hy - 8)

  -- the near arm, in front of everything: a snow upper arm, a forearm cased in
  -- glacier ice, a fist of ice
  local sx, sy = 39, 31 + b
  capsule(g, sx, sy, P.fe[1], P.fe[2], 6, SNOW[2], SNOW[3], SNOW[4])
  capsule(g, P.fe[1], P.fe[2], P.ff[1], P.ff[2] - 2, 5, SNOW[2], SNOW[3], SNOW[4])
  capsule(g, P.fe[1], P.fe[2], P.ff[1], P.ff[2] - 2, 3.4, ICE[2], ICE[3], ICE[4])
  ellipse(g, P.ff[1], P.ff[2] - 1, 5.6, 5, FIST_FRONT)
  -- knuckle cracks
  put(g, math.floor(P.ff[1]), math.floor(P.ff[2] - 1), ICE[1])

  outline(g)
  return g
end

sheet("Content/NPCs/Bosses/RimeColossus.png", 7, 64, 64, frame)

-- the map icon: the head alone, 15x15 art cells
do
  local g = newGrid(15, 15)
  stamp(g, {
    "...sSSSS.......",
    "..sSWWWWSs.....",
    ".hhSWWWSSSs....",
    "hHghSSSSSssd...",
    "hg.gdsSSSWWSd..",
    "hHghddkkkSssd..",
    ".hgdDkEYEkdSSd.",
    "..ddDkkekkdsSs.",
    "...DdIiSWSSIi..",
    "...dIiiIiiIiic.",
    "....icCicCicC..",
    "....CB.cB.cB...",
    "....B..C..B....",
  }, CH, 0, 1)
  outline(g)
  local img = Image(30, 30, ColorMode.RGB)
  blit(img, g, 0, 0, 15, 15)
  saveImage(img, "Content/NPCs/Bosses/RimeColossus_Head_Boss.png")
end

-- the block it carries and throws: a cube of glacier ice in three-quarter view -
-- a lit top with a cap of snow, a mid front, a dark side, hard breaks between the
-- faces and two white fracture streaks inside
do
  local g = newGrid(22, 20)
  -- top face (a parallelogram), front face, right face
  for y = 0, 19 do
    for x = 0, 21 do
      local col
      if y <= 5 and x >= 5 - y and x <= 20 - y then col = ICE[2]
      elseif y > 5 and x >= 0 and x <= 15 then col = ICE[3]
      elseif y > 5 - (x - 15) and x > 15 and x <= 20 and y <= 19 - (x - 15) then col = ICE[5] end
      if col then put(g, x, y, col) end
    end
  end
  -- edges: the lit rim along the top's front edge and the front's left edge
  for x = 0, 15 do put(g, x, 6, ICE[1]) end
  for y = 6, 19 do put(g, 0, y, ICE[2]) end
  for y = 7, 19 do put(g, 15, y, ICE[4]) end
  for y = 13, 19 do for x = 1, 14 do if get(g, x, y) == ICE[3] then put(g, x, y, ICE[4]) end end end
  -- fracture streaks
  stamp(g, { "I..", ".I.", ".iI", "..i", "...I" }, CH, 3, 8)
  stamp(g, { "..I", ".I.", "Ii.", "i.." }, CH, 10, 10)
  -- snow on the top
  stamp(g, { "....SWWWSs....", "..SWWWWSSss...", ".sSSSs..sd...." }, CH, 4, 0)
  outline(g)
  save(g, "Content/Projectiles/Rime/RimeBoulder.png")
end

print("rime boss: done")
