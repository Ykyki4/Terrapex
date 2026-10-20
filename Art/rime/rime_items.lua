-- The Rime's drops and the summon. Run from the repo root:
--
--   aseprite -b --script-param root=<repo> --script Art/rime/rime_items.lua
--
-- Every item is its own archetype, never one shape recoloured: a maul with a block
-- of glacier ice for a head, a recurve bow with horn tips and an ice-crusted back,
-- a birch staff with a snowball in an ice claw, a bell of ice on a red strap, and
-- a scythe whose blade is a jagged glacier shard. Swung and held weapons are drawn
-- as held facing right: grip bottom-left, tip top-right.

dofile(app.params.root .. "/Art/rime/rlib.lua")

local WOODR = { hex("c79a6a"), hex("8a5a40"), hex("54323a") } -- dark old wood

-- a box set across the end of a haft: u runs along the haft, w across it. The
-- faces are hard: the top-left end lit, the striking face light, the body mid,
-- the face toward the haft dark, the bottom-right end darkest.
local function crossBox(g, cx, cy, du, dw, hu, hw, ramp)
  for y = math.floor(cy - 10), math.ceil(cy + 10) do
    for x = math.floor(cx - 10), math.ceil(cx + 10) do
      local px, py = x + 0.5 - cx, y + 0.5 - cy
      local u = px * du[1] + py * du[2]
      local w = px * dw[1] + py * dw[2]
      if math.abs(u) <= hu and math.abs(w) <= hw then
        local col
        if w <= -hw + 1.2 then col = ramp[1]
        elseif w >= hw - 1.2 then col = ramp[5]
        elseif u >= hu - 1.2 then col = ramp[2]
        elseif u <= -hu + 1.2 then col = ramp[4]
        else col = ramp[3] end
        put(g, x, y, col)
      end
    end
  end
end

-- Glacierbreaker: a long dark haft, a red wrap at the grip and at the neck, and a
-- block of glacier ice set across the top like a maul's head, snow on its top end
do
  local g = newGrid(26, 26)
  haft(g, 2.0, 24.5, 15.0, 11.5, 1.15, WOODR[1], WOODR[2], WOODR[3], { 0.45, 0.62 })
  -- grip wrap
  for _, c in ipairs(cells(g)) do
    local t = (c[1] - 2) / 13
    if t > 0.08 and t < 0.3 then put(g, c[1], c[2], ((c[1] + c[2]) % 2 == 0) and RED[2] or RED[1]) end
  end
  local s = math.sqrt(0.5)
  crossBox(g, 18.2, 8.4, { s, -s }, { s, s }, 3.4, 7.2, { ICE[1], ICE[2], ICE[3], ICE[4], ICE[5] })
  -- fracture streaks inside the block
  stamp(g, { "I..", ".I.", "..i" }, CH, 15, 6)
  stamp(g, { ".I", "I.", "i." }, CH, 20, 9)
  -- snow on the lit end
  stamp(g, { ".WS", "WSs", "Ss." }, CH, 12, 2)
  -- the red wrap binding head to haft
  stamp(g, { ".rR", "rRq", "Rq." }, CH, 13, 10)
  outline(g)
  save(g, "Content/Items/Weapons/Rime/Glacierbreaker.png")
end

-- Hoarfrost Bow: a recurve of dark wood bulging to the right, ice grown along its
-- back, horn nocks at both tips, a red grip, a pale string straight down the left
do
  local g = newGrid(15, 27)
  for r = 1, 25 do
    local t = (r - 1) / 24
    local cx = 3.2 + 6.4 * math.sin(math.pi * t) - 1.4 * math.sin(2 * math.pi * t) * 0.0
    -- the recurve: the tips flick back outward
    if t < 0.12 then cx = cx + (0.12 - t) * 14 end
    if t > 0.88 then cx = cx + (t - 0.88) * 14 end
    local half = 0.9 + 0.7 * math.sin(math.pi * t)
    for x = math.floor(cx - half), math.floor(cx + half) do
      local col = (x < cx - half * 0.3) and WOODR[1] or ((x > cx + half * 0.3) and WOODR[3] or WOODR[2])
      put(g, x, r, col)
    end
    -- ice on the back (the right side) of both limbs
    if (t > 0.18 and t < 0.4) or (t > 0.6 and t < 0.82) then
      local ex = math.floor(cx + half) + 1
      put(g, ex, r, (r % 3 == 0) and ICE[1] or ICE[3])
      if (t > 0.24 and t < 0.34) or (t > 0.66 and t < 0.76) then put(g, ex + 1, r, ICE[4]) end
    end
  end
  -- grip
  for r = 11, 15 do for x = 7, 10 do if get(g, x, r) then put(g, x, r, (r % 2 == 0) and RED[2] or RED[1]) end end end
  -- horn nocks
  stamp(g, { "Hh", ".g" }, CH, 4, 0)
  stamp(g, { ".g", "Hh" }, CH, 4, 25)
  -- the string
  for r = 2, 24 do put(g, 2, r, SNOW[2]) end
  outline(g)
  save(g, "Content/Items/Weapons/Rime/HoarfrostBow.png")
end

-- Snowcap Staff: a birch staff (pale, with dark bark marks), a three-pronged claw
-- of ice at the top and a snowball held in it, a red ribbon tied under the claw
do
  local g = newGrid(25, 25)
  haft(g, 1.8, 23.5, 15.5, 9.8, 1.05, BIRCH[1], BIRCH[2], BIRCH[3])
  -- birch marks: short dark dashes across the bark, placed by hand
  for _, m in ipairs({ { 5, 20 }, { 8, 17 }, { 11, 14 }, { 13, 12 } }) do put(g, m[1], m[2], BIRCH[4]) end
  -- the claw
  stamp(g, {
    "...I.....",
    "..Ic.....",
    ".Ic....I.",
    ".ic...Ic.",
    ".iC..Ic..",
    "..cC.cC..",
    "..IccCC..",
    "...iCB...",
  }, CH, 13, 3)
  -- the snowball, held in the claw
  ellipse(g, 19.5, 6.5, 3.4, 3.4, { SNOW[1], SNOW[2], SNOW[3], SNOW[4], SNOW[5] })
  put(g, 20, 7, SNOW[3])
  stamp(g, { "IiC" }, CH, 14, 8) -- the claw's near prong in front of the ball
  -- ribbon
  stamp(g, { "Rr..", ".rRq", "..q.", "...r" }, CH, 13, 10)
  outline(g)
  save(g, "Content/Items/Weapons/Rime/SnowcapStaff.png")
end

-- Glacier Bell: a bell of ice on a red strap loop, a horn clapper under its mouth
do
  local g = newGrid(16, 18)
  stamp(g, {
    "....qRRq....",
    "...r....r...",
    "...R....R...",
    "....rRRr....",
    "....IiiC....",
    "...IiccCB...",
    "...IiccCB...",
    "..IicIccCB..",
    "..IicIccCB..",
    "..IiccIcCB..",
    ".IiiccIccCB.",
    ".IiccccccCB.",
    "IiicccccccCB",
    "WSSssssddDDx",
    ".kkkkkkkkkk.",
    ".....hg.....",
    ".....Gg.....",
  }, CH, 2, 0)
  outline(g)
  save(g, "Content/Items/Weapons/Rime/GlacierBell.png")
end

-- Frostfell Scythe: a pale birch snath, a red wrap and a fur tassel at the neck,
-- and a blade that is a shard of glacier: the crescent cut into facets with hard
-- breaks and a jagged, broken edge
do
  local g = newGrid(26, 26)
  haft(g, 1.4, 24.6, 10.2, 5.2, 1.1, BIRCH[1], BIRCH[2], BIRCH[3])
  for _, m in ipairs({ { 4, 20 }, { 6, 16 }, { 8, 12 } }) do put(g, m[1], m[2], BIRCH[4]) end
  local A, B = { x = 16.0, y = 10.6, r = 9.8 }, { x = 12.6, y = 15.6, r = 9.0 }
  crescent(g, A, B, function(x, y) return x > 10.0 and y < 19.5 end,
    { ICE[1], ICE[2], ICE[3], ICE[4], ICE[1] })
  -- facets: the blade broken into planes by its angle round A, alternating light and
  -- mid, with a dark plane next to the edge - ice, not steel
  eachBladeCell(g, A, B, -170, function(x, y, s, fromSpine, fromEdge)
    local facet = math.floor(s / 22)
    if fromEdge >= 0.9 and fromSpine >= 0.9 then
      local col = facet % 2 == 0 and ICE[2] or ICE[4]
      if s % 22 < 2.2 then col = ICE[1] end          -- the lit break between planes
      if fromEdge < 1.9 and facet % 2 == 0 then col = ICE[3] end
      put(g, x, y, col)
    end
    -- the broken edge: a bite out of every other plane
    if facet % 2 == 1 and fromEdge < 1.6 and s < 150 then clear(g, x, y) end
  end)
  -- wrap and tassel at the neck
  stamp(g, { "rR.", "qrR", ".f.", ".fu", ".uU", "..U" }, CH, 9, 4)
  outline(g)
  save(g, "Content/Items/Weapons/Rime/FrostfellScythe.png")
end

-- Permafrost Heart: a faceted heart of glacier ice with a coal of amber in it, a
-- crust of snow on its lobes
do
  local g = newGrid(1, 1)
  stamp(g, {
    ".WSs..SWs..",
    "WSIic.IicC.",
    "IiicciicCCB",
    "IicccicccCB",
    "IicEYEcccCB",
    ".iceYEecCB.",
    "..icEecCB..",
    "...icCCB...",
    "....cCB....",
    ".....B.....",
  }, CH, 1, 1)
  outline(g)
  save(g, "Content/Items/Accessories/PermafrostHeart.png")
end

-- the mask: the Rime's head in profile, stamped into the twenty head frames on the
-- player's head box (cols 5-15 / rows 5-14), the eye over the player's eye line and
-- the icicle beard hanging under the chin
do
  local MASK = {
    "Hh.........",
    ".hg........",
    "..hg.sSSs..",
    "...gsWWWSs.",
    "..dsWWWWSSs",
    ".dssSSSWWWS",
    ".ddkkkkdSSs",
    ".dDkYEkdsSs",
    ".ddkkekddSS",
    "..dDddddsSs",
    "..dIiiSWSIi",
    "..IiciicIic",
    "...cCicCicC",
    "...B.CB.cB.",
    ".....B..B..",
  }
  local g = newGrid(20, 28)
  stamp(g, MASK, CH, 5, 3)
  outline(g)
  local sheetImg = Image(40, 1120, ColorMode.RGB)
  local high = { [7] = true, [8] = true, [9] = true, [14] = true, [15] = true, [16] = true }
  for f = 0, 19 do
    local top = f * 56 - (high[f] and 2 or 0)
    for _, c in ipairs(cells(g)) do
      for sy = 0, 1 do
        for sx = 0, 1 do
          local py = top + c[2] * 2 + sy
          if py >= f * 56 and py < f * 56 + 56 then sheetImg:drawPixel(c[1] * 2 + sx, py, c[3]) end
        end
      end
    end
  end
  saveImage(sheetImg, "Content/Items/Armor/RimeMask_Head.png")
  save(g, "Content/Items/Armor/RimeMask.png")
end

-- the treasure bag: a hide sack, fur side out, tied with red cord, snow on its
-- shoulders and an icicle hanging off the knot
do
  local g = newGrid(1, 1)
  stamp(g, {
    "....RrR.......",
    ".....rq.......",
    "....WSSs......",
    "..WSFFFfSs....",
    ".SFFFFffffu...",
    "SFFFFfffffuU..",
    "FFFFfffffuuU..",
    "FFFfffiffuuU..",
    "FFffffIffuuU..",
    ".Fffffcfuuu...",
    ".ffffffuuuU...",
    "..uuuuuuUU....",
  }, CH, 1, 1)
  outline(g)
  save(g, "Content/Items/Consumables/RimeBag.png")
end

-- Frozen Horn: an old ram's horn made into a trumpet - a mouthpiece at the
-- bottom-left, curling up and out to a wide bell at the right, ice crusted on its
-- back and a red carrying strap
do
  local g = newGrid(1, 1)
  stamp(g, {
    "...........HHh.",
    ".........HHkkkg",
    "........HhkkkkG",
    ".......HhhkkkgG",
    "......HhhggGG..",
    ".....HhhggG....",
    "....IiHgG......",
    "...IicHg.......",
    "..HhcCg........",
    ".Hhg...........",
    "Gg.rRr.........",
    "....qr.........",
  }, CH, 1, 1)
  outline(g)
  save(g, "Content/Items/Consumables/FrozenHorn.png")
end

-- Frost Pelt: a folded white hide, the leather showing on the turned-back corner
do
  local g = newGrid(1, 1)
  stamp(g, {
    "..WWSS.....",
    ".WWSSSs....",
    "WWSSSsssF..",
    "WSSSsssFff.",
    "SSSssdFfffu",
    ".Sssdduffu.",
    "..sddDuuU..",
    "...dDDx....",
  }, CH, 1, 1)
  outline(g)
  save(g, "Content/Items/Materials/FrostPelt.png")
end

-- Rime Crystal: three prisms of rime off one root, the middle one tall
do
  local g = newGrid(1, 1)
  stamp(g, {
    "....I....",
    "...IiC...",
    "...IcC...",
    ".I.IcCB..",
    "IiCIicB.I",
    "IcCIicBIC",
    ".iCicCBcB",
    "..icCCcB.",
    "..SWSsdd.",
  }, CH, 1, 1)
  outline(g)
  save(g, "Content/Items/Materials/RimeCrystal.png")
end

print("rime items: done")
