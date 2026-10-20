-- The Rime's projectiles, its two snow mobs and the minion's buff icon. Run:
--
--   aseprite -b --script-param root=<repo> --script Art/rime/rime_fx.lua
--
-- Orientation contracts the code relies on:
--   RimeShard, RimeArrow    point UP (the code adds +90 deg to the velocity angle)
--   RimeIcicle              hangs point DOWN, drawn unrotated
--   RimeShockwave           stands on its bottom edge (drawn from the bottom)
--   RimeWall                one 30x32 tile, repeated vertically - no top/bottom outline
--   RimeSprite              four frames facing right
--   DriftHare, FrostWisp    four frames each; the hare faces LEFT (vanilla's NPC flip)

dofile(app.params.root .. "/Art/rime/rlib.lua")

-- a prism of ice standing on (bx, by), h tall, w wide, leaning by `lean` per row
local function spike(g, bx, by, h, w, lean)
  for j = 0, h - 1 do
    local t = j / h
    local half = w * (1 - t) * 0.5
    local cx = bx + lean * j
    for x = math.floor(cx - half + 0.5), math.floor(cx + half + 0.5) do
      local col
      if j == h - 1 then col = ICE[1]
      elseif x < cx - 0.5 then col = (x <= cx - half + 0.5) and ICE[1] or ICE[2]
      elseif x < cx + 0.5 then col = ICE[3]
      else col = (x >= cx + half - 0.5) and ICE[5] or ICE[4] end
      put(g, x, by - j, col)
    end
  end
end

-- RimeShockwave: three spikes erupting from a heap of snow
do
  local g = newGrid(18, 24)
  spike(g, 4, 21, 12, 5, -0.12)
  spike(g, 13, 21, 15, 5, 0.1)
  spike(g, 8.5, 21, 21, 7, 0)
  stamp(g, { "..WSSSSSSSSs...", ".WSSsssssssddD.", "WSssdddddddDDDx" }, CH, 1, 20)
  outline(g)
  local img = Image(36, 48, ColorMode.RGB)
  blit(img, g, 0, 0, 18, 24)
  saveImage(img, "Content/Projectiles/Rime/RimeShockwave.png")
end

-- RimeShard: a sliver pointing up
do
  local g = newGrid(1, 1)
  stamp(g, { ".I.", "Iic", "Iic", "icC", "icC", "cCB", ".B." }, CH, 1, 1)
  outline(g)
  save(g, "Content/Projectiles/Rime/RimeShard.png")
end

-- RimeIcicle: a long icicle with a cap of snow, hanging point down
do
  local g = newGrid(1, 1)
  stamp(g, {
    ".WWSSSSsd.",
    "WSSSsssddD",
    ".IiicccCB.",
    ".IiicccCB.",
    ".IiiccCCB.",
    "..IicccB..",
    "..IicIcB..",
    "..IiccCB..",
    "..IicCCB..",
    "...IicB...",
    "...IicB...",
    "...IIcB...",
    "...IicB...",
    "...icCB...",
    "....icB...",
    "....icB...",
    "....iC....",
    "....iC....",
    "....IC....",
    "....I.....",
  }, CH, 1, 1)
  outline(g)
  save(g, "Content/Projectiles/Rime/RimeIcicle.png")
end

-- RimeWall: one block of the wall. Tiled, so it is outlined only down its sides,
-- and the seam at its foot is the joint between blocks
do
  local img = Image(30, 32, ColorMode.RGB)
  local g = newGrid(15, 16)
  for y = 0, 15 do
    for x = 0, 14 do
      local col
      if x == 0 or x == 14 then col = OUTLINE
      elseif x == 1 then col = ICE[1]
      elseif x <= 3 then col = ICE[2]
      elseif x >= 12 then col = ICE[5]
      elseif x >= 10 then col = ICE[4]
      else col = ICE[3] end
      if y == 15 and x > 0 and x < 14 then col = ICE[5] end
      if y == 0 and x > 0 and x < 14 then col = ICE[2] end
      put(g, x, y, col)
    end
  end
  -- fracture streaks and a bubble or two
  stamp(g, { "I...", ".I..", ".iI.", "...i" }, CH, 4, 3)
  stamp(g, { "..I", ".I.", "i.." }, CH, 7, 9)
  put(g, 9, 5, ICE[1])
  put(g, 5, 12, ICE[2])
  blit(img, g, 0, 0, 15, 16)
  saveImage(img, "Content/Projectiles/Rime/RimeWall.png")
end

-- FrostMote: a six-pointed mote
do
  local g = newGrid(1, 1)
  stamp(g, { "i...i", ".iIi.", ".IWI.", ".iIi.", "i...i" }, CH, 1, 1)
  outline(g)
  save(g, "Content/Projectiles/Rime/FrostMote.png")
end

-- RimeArrow: an icicle bolt, point up, with a stub of shaft and pale fletching
do
  local g = newGrid(1, 1)
  stamp(g, {
    ".I.",
    ".I.",
    "Iic",
    "Iic",
    "icC",
    "icC",
    "cCB",
    ".h.",
    ".h.",
    ".g.",
    "SgS",
    "SgS",
    "s.s",
  }, CH, 1, 1)
  outline(g)
  save(g, "Content/Projectiles/Rime/RimeArrow.png")
end

-- RollingSnowball: a ball of packed snow, its clumps showing the roll
do
  local g = newGrid(12, 12)
  ellipse(g, 6, 6, 5.5, 5.5, { SNOW[1], SNOW[2], SNOW[3], SNOW[4], SNOW[5] })
  for _, p in ipairs({ { 4, 7 }, { 7, 3 }, { 8, 8 }, { 3, 4 } }) do put(g, p[1], p[2], SNOW[4]) end
  put(g, 5, 7, SNOW[3])
  outline(g)
  local img = Image(26, 26, ColorMode.RGB)
  for _, c in ipairs(cells(g)) do
    for sy = 0, 1 do for sx = 0, 1 do img:drawPixel((c[1] + 1) * 2 + sx, (c[2] + 1) * 2 + sy, c[3]) end end
  end
  saveImage(img, "Content/Projectiles/Rime/RollingSnowball.png")
end

-- RimeSprite: a little rime-spirit - a snow head with a horn and an amber eye, and
-- a tail of ice-wisp that flickers between four frames. Faces right.
local SPRITE_HEAD = {
  ".hg.......",
  "..hgsSS...",
  "..sSWWWS..",
  ".sSWWWWSs.",
  ".dsSSkkSs.",
  ".dssSkYSS.",
  ".ddssSSsd.",
  "..dIiiSIi.",
  "...icCic..",
}
local TAILS = {
  { "..ic....", ".ic.....", "ic......" },
  { "..ic....", "..ic....", ".ic.....", "..c....." },
  { "..ic....", "...ic...", "....c..." },
  { "..ic....", ".ic.....", ".c......", "c......." },
}
do
  local img = Image(26, 26 * 4, ColorMode.RGB)
  for f = 1, 4 do
    local g = newGrid(13, 13)
    stamp(g, TAILS[f], CH, 1, 9)
    stamp(g, SPRITE_HEAD, CH, 2, (f == 2 or f == 3) and 0 or 1)
    outline(g)
    blit(img, g, 0, (f - 1) * 26, 13, 13)
  end
  saveImage(img, "Content/Projectiles/Rime/RimeSprite.png")

  local g = newGrid(16, 16)
  stamp(g, SPRITE_HEAD, CH, 3, 2)
  stamp(g, TAILS[1], CH, 2, 11)
  outline(g)
  local icon = Image(32, 32, ColorMode.RGB)
  blit(icon, g, 0, 0, 16, 16)
  saveImage(icon, "Content/Buffs/RimeSpriteBuff.png")
end

-- DriftHare: a white hare, black ear tips, an amber-ringed eye; faces left.
-- 0 sitting, 1 falling (legs hanging), 2 leaping (stretched out), 3 the mound it
-- travels as under the snow - with the black ear tips poking out, so the player
-- knows what the moving heap is
local HARE = {
  {
    "................",
    ".....k..k.......",
    ".....Wd.Wd......",
    ".....Wd.Wd......",
    ".....WsdWd......",
    "....WSSSSs......",
    "...WSSSSSSs.....",
    "..WSkESSSSSs....",
    "..SSSSSSSSSSs...",
    "...rSSSSSSSSsd..",
    "....SSSSSSSSsdW.",
    "....sSSSSSsssdW.",
    ".....SsssssddD..",
    "....WS..sd.WSd..",
    "...WSs..d..Wsd..",
    "................",
  },
  {
    ".....k..........",
    ".....Wd..k......",
    ".....Wd..Wd.....",
    ".....WsdWd......",
    "....WSSSSs......",
    "...WSSSSSSs.....",
    "..WSkESSSSSs....",
    "..SSSSSSSSSSs...",
    "...rSSSSSSSSsdW.",
    "....SSSSSSSSsdW.",
    "....sSSSSSsssd..",
    ".....SsssssddD..",
    ".....Ss...sdd...",
    "....WS....sd....",
    "....W.....Wd....",
    "................",
  },
  {
    "................",
    "..........k.k...",
    ".........Wd.Wd..",
    "...Ws....WsdWd..",
    "..WSs...WSSSs...",
    "..WSSs.WSSSSSs..",
    "...SSSSSSSkESSs.",
    "....sSSSSSSSSSS.",
    ".WdssSSSSSSSSr..",
    "WsdddsSSSSSSs...",
    ".d...dssssSs....",
    "......dd..Ss....",
    "...........SW...",
    "............W...",
    "................",
    "................",
  },
  {
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "................",
    "......k..k......",
    "......d..d......",
    ".....WSWWSs.....",
    "...WWSSSSSSss...",
    "..WSSSssssssdd..",
    ".WSSssssdddddDD.",
    "WSSsssddddDDDDxx",
    "................",
  },
}
do
  local pal = {}
  for k, v in pairs(CH) do pal[k] = v end
  pal.k = hex("1e1a2c")
  pal.r = RED[1] -- a nose, the one warm cell
  local img = Image(32, 32 * 4, ColorMode.RGB)
  for f = 1, 4 do
    local g = newGrid(16, 16)
    stamp(g, HARE[f], pal, 0, 0)
    -- the leap is authored facing right, where its stretch was easier to draw: mirror it
    if f == 3 then
      local m = newGrid(16, 16)
      for _, c in ipairs(cells(g)) do put(m, 15 - c[1], c[2], c[3]) end
      g = m
    end
    outline(g)
    blit(img, g, 0, (f - 1) * 32, 16, 16)
  end
  saveImage(img, "Content/NPCs/Snow/DriftHare.png")
end

-- FrostWisp: a flame of cold - a teardrop of white-hot ice, two dark eyes, its
-- tips flickering differently in each of four frames
do
  local TIPS = {
    { { 6, 0 }, { 4, 2 }, { 9, 1 } },
    { { 7, 0 }, { 5, 1 }, { 9, 2 } },
    { { 6, 1 }, { 3, 2 }, { 8, 0 } },
    { { 7, 1 }, { 4, 1 }, { 10, 2 } },
  }
  local img = Image(28, 32 * 4, ColorMode.RGB)
  for f = 1, 4 do
    local g = newGrid(14, 16)
    ellipse(g, 7, 10, 4.6, 4.8, { ICE[1], ICE[2], ICE[3], ICE[4], ICE[5] }, { 0.5, 0.0, -0.4, -0.75 })
    -- the hot core
    for y = 8, 11 do for x = 5, 8 do if get(g, x, y) then put(g, x, y, (x + y) % 3 == 0 and ICE[1] or SNOW[1]) end end end
    -- the flame rising to the flickering tips
    for _, t in ipairs(TIPS[f]) do
      local tx, ty = t[1], t[2]
      for y = ty, 6 do
        local x = math.floor(tx + (7 - tx) * (y - ty) / (6 - ty + 0.01) + 0.5)
        put(g, x, y, y <= ty + 1 and ICE[1] or ICE[2])
        if y > ty + 2 then put(g, x + 1, y, ICE[3]) end
      end
    end
    stamp(g, { "k..k" }, { k = hex("1e2a48") }, 5, 10)
    outline(g)
    blit(img, g, 0, (f - 1) * 32, 14, 16)
  end
  saveImage(img, "Content/NPCs/Snow/FrostWisp.png")
end

print("rime fx: done")
