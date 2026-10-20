-- The Sultan's shots, the player's shots from its drops, and the two desert mobs.
--
--   aseprite -b --script-param root=<repo> --script Art/scarab/shots_mobs.lua
--
-- Sizes here are load-bearing - the code frames these sheets by count, so every
-- sheet is written at a fixed size with sheet()/fixed(), never cropped by save().

dofile(app.params.root .. "/Art/scarab/slib.lua")

local P = "Content/Projectiles/Scarab/"

-- the sun shard: a hot spike pointing right (the code rotates it to its velocity)
do
  local g = newGrid(8, 6)
  stamp(g, {
    "..YO....",
    ".YWYOo..",
    "YWWWYYOo",
    ".YWYOo..",
    "..YO....",
  }, { W = SUNR[1], Y = SUNR[2], O = SUNR[3], o = SUNR[4] }, 0, 0)
  outline(g)
  fixed(P .. "SunShard.png", 9, 7, (function()
    local h = newGrid(9, 7)
    for _, c in ipairs(cells(g)) do put(h, c[1] + 1, c[2] + 1, c[3]) end
    return h
  end)())
end

-- the sand pillar: two 36x192 frames of a churning column, lit left, dark right,
-- with diagonal bands of grit that climb between frames
sheet(P .. "SandPillar.png", 2, 18, 96, function(f)
  local g = newGrid(18, 96)
  for y = 1, 95 do
    -- slightly waisted, flaring at the foot where it breaks out of the floor
    local half = 7 + math.floor(math.max(0, (y - 82) / 4))
    local cx = 9
    for x = cx - half, cx + half - 1 do
      local u = (x - (cx - half)) / (2 * half)
      local band = ((y + f * 5 + math.floor(u * 8)) // 4) % 3
      local col
      if u < 0.18 then col = SANDR[1]
      elseif u > 0.8 then col = SANDR[4]
      elseif band == 0 then col = SANDR[3]
      else col = SANDR[2] end
      if u > 0.9 then col = SANDR[5] end
      put(g, x, y, col)
    end
  end
  -- the top is ragged: spray, not a lid
  for _, p in ipairs(f == 1 and { { 4, 0 }, { 9, 0 }, { 13, 1 } } or { { 6, 0 }, { 11, 0 } }) do put(g, p[1], p[2], SANDR[2]) end
  outline(g)
  return g
end)

-- the hostile beetling: a gilded scarab in flight, wings up / wings down, facing right
local function beetle(f, shell, inlay, wingTone)
  local g = newGrid(13, 11)
  if f == 1 then
    stamp(g, { "..wW.", ".wwW.", "wwW..", }, { w = wingTone[2], W = wingTone[1] }, 2, 0)
  else
    stamp(g, { "....", "....", "....", "....", "....", "....", "....", ".wwW", "wwW." }, { w = wingTone[2], W = wingTone[1] }, 1, 1)
  end
  stamp(g, {
    "..aabbb...",
    ".abbbbbcd.",
    "abIIIIbccdh",
    "abbbbbbccEh",
    ".cccccccdd.",
    "..l.l..l...",
  }, { a = shell[1], b = shell[2], c = shell[3], d = shell[4], I = inlay, E = SUNR[4], h = shell[3], l = BRONZE[3] }, 1, 3)
  outline(g)
  return g
end
sheet(P .. "Beetling.png", 2, 13, 11, function(f)
  return beetle(f, { GOLDR[1], GOLDR[2], GOLDR[3], GOLDR[4] }, LAPIS[2], WINGR)
end)
-- the player's beetling: lapis-shelled with a gold band, so it never reads as the boss's
sheet(P .. "BeetlingMinion.png", 2, 13, 11, function(f)
  return beetle(f, { LAPIS[1], LAPIS[2], LAPIS[3], LAPIS[4] }, GOLDR[2], { TURQ[1], TURQ[2] })
end)

-- the scarab arrow: pointing up, as Terraria's arrows are; a gold scarab-head point,
-- a sandwood shaft, lapis fletching
do
  local g = newGrid(5, 16)
  stamp(g, {
    "..Y..",
    ".YGg.",
    ".GGg.",
    "..g..",
    "..s..",
    "..s..",
    "..S..",
    "..s..",
    "..s..",
    "..S..",
    "..s..",
    ".LsL.",
    "LLsLl",
    "L.s.l",
    "..s..",
  }, { Y = GOLDR[1], G = GOLDR[2], g = GOLDR[4], s = SANDR[3], S = SANDR[4], L = LAPIS[2], l = LAPIS[3] }, 0, 0)
  outline(g)
  local h = newGrid(7, 17)
  for _, c in ipairs(cells(g)) do put(h, c[1] + 1, c[2] + 1, c[3]) end
  fixed(P .. "ScarabArrow.png", 7, 17, h)
end

-- the scepter bolt: a small sun in a ring of sand
do
  local g = newGrid(9, 9)
  dome(g, 4.5, 4.5, 3, 3, { SUNR[1], SUNR[2], SUNR[3], SUNR[4], SUNR[5] })
  for _, p in ipairs({ { 0, 4 }, { 8, 4 }, { 4, 0 }, { 4, 8 } }) do put(g, p[1], p[2], SANDR[2]) end
  outline(g)
  local h = newGrid(11, 11)
  for _, c in ipairs(cells(g)) do put(h, c[1] + 1, c[2] + 1, c[3]) end
  fixed(P .. "ScepterBolt.png", 11, 11, h)
end

-- the sun pebble: the Sultan's sun at a tenth the size, a swirl of sand round a hot core
do
  local g = newGrid(10, 10)
  dome(g, 5, 5, 4.2, 4.2, { SUNR[1], SUNR[2], SUNR[3], SUNR[4], SUNR[5] })
  for _, p in ipairs({ { 3, 2 }, { 4, 2 }, { 6, 4 }, { 6, 5 }, { 3, 7 }, { 4, 7 } }) do put(g, p[1], p[2], SANDR[2]) end
  outline(g)
  local h = newGrid(12, 12)
  for _, c in ipairs(cells(g)) do put(h, c[1] + 1, c[2] + 1, c[3]) end
  fixed(P .. "SunPebble.png", 12, 12, h)
end

-------------------------------------------------------------------------------
-- the Sand Roller: a dull bronze dung beetle, head down, facing the ball it pushes
-- (the ball itself is SunBall's third frame, drawn by the code in front of it).
-- 24x16 art = 48x32 px, five frames: four walking, one rearing.
local RSH = { hex("f0c880"), hex("c08446"), hex("8a5236"), hex("54303a") }
local ROLLER = {
  "......aaaab...........",
  "....aaAAaabbb.........",
  "...aAAaaaabbbcs.......",
  "..aAaaaaabbbbcsppp....",
  "..aaaaaabbbbbcspPpp...",
  ".abbbbbbbbbbccsppppqh.",
  ".bbbbbbbbbbcccsppqqhhh",
  ".bbbbbbbbbccccsqqqhEhT",
  ".cccccccccccccsqqqhhhT",
  "..dddddddddddd..qq.hT.",
}
local function roller(f)
  local g = newGrid(24, 16)
  local rear = f == 5
  -- legs first, so the shell overlaps the hips: hip, knee (out and up), foot
  local legs = {
    { { 5, 11 }, { 2, 12 }, { 3, 15 } }, { { 10, 11 }, { 9, 13 }, { 11, 15 } }, { { 16, 11 }, { 19, 12 }, { 18, 15 } },
  }
  local swing = ({ { -1, 1, -1 }, { 0, 0, 0 }, { 1, -1, 1 }, { 0, 0, 0 }, { 0, 0, 0 } })[f]
  for i, l in ipairs(legs) do
    local s = swing[i]
    local knee, foot = { l[2][1] + s, l[2][2] }, { l[3][1] + s * 2, l[3][2] }
    if rear and i == 3 then knee, foot = { 21, 9 }, { 23, 6 } end -- the front leg braced up on the ball
    limb(g, l[1][1], l[1][2], knee[1], knee[2], 0.75, BRONZE[2], BRONZE[3], BRONZE[4])
    limb(g, knee[1], knee[2], foot[1], foot[2], 0.75, BRONZE[2], BRONZE[3], BRONZE[4])
    put(g, foot[1], foot[2], BRONZE[4])
  end
  stamp(g, ROLLER, { a = RSH[1], A = GOLDR[1], b = RSH[2], c = RSH[3], s = RSH[4], p = RSH[1], P = GOLDR[1],
    q = RSH[3], h = RSH[3], E = SUNR[4], T = GOLDR[3], d = BRONZE[3] }, 1, rear and 1 or 2)
  outline(g)
  return g
end
sheet("Content/NPCs/Desert/SandRoller.png", 5, 24, 16, roller)

-- the Dust Devil: a funnel of grit, wide at the top, with a dead scarab husk turning
-- in its eye. 18x26 art = 36x52 px, four frames of the bands climbing.
local function devil(f)
  local g = newGrid(18, 26)
  for y = 1, 24 do
    local half = math.floor(2 + (24 - y) / 24 * 6.5 + 0.5)
    local sway = math.floor(math.sin((y + f * 2) * 0.45) * 1.2 + 0.5)
    local cx = 9 + sway
    for x = cx - half, cx + half - 1 do
      local u = (x - (cx - half)) / (2 * half)
      local band = ((y * 2 + math.floor(u * 6) - f * 3) // 5) % 3
      local col = band == 0 and SANDR[3] or SANDR[2]
      if u < 0.15 then col = SANDR[1] elseif u > 0.82 then col = SANDR[4] end
      -- the gaps in the wind: some cells left open so it reads as moving air, not a cone
      if band == 2 and u > 0.3 and u < 0.7 and y % 4 == f % 4 then col = SANDR[1] end
      put(g, x, y, col)
    end
  end
  -- the husk in the eye: a dried scarab, dark, with one dull glint
  local hx = 7 + ({ 0, 1, 1, 0 })[f]
  stamp(g, { ".bb.", "bBBb", "bBEb", ".bb." }, { b = BRONZE[4], B = BRONZE[3], E = GOLDR[3] }, hx, 9)
  outline(g)
  return g
end
sheet("Content/NPCs/Desert/DustDevil.png", 4, 18, 26, devil)

print("shots_mobs: done")
