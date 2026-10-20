-- Everything the Scarab Sultan drops, its summon, the mob materials, the buff icon.
--
--   aseprite -b --script-param root=<repo> --script Art/scarab/items.lua
--
-- One idea per item, each its own silhouette: a hooked khopesh, a vertical recurve bow
-- whose limbs are wing-cases, a was-sceptre with a forked foot and a beast's head, a
-- staff crowned with a round lapis scarab, a scythe with a sun-disc in its neck, a
-- winged heart-scarab amulet. Swung and held weapons are drawn as held facing right:
-- grip bottom-left, tip top-right.

dofile(app.params.root .. "/Art/scarab/slib.lua")

local I = "Content/Items/"

local function pad(g, w, h)
  local o = newGrid(w, h)
  for _, c in ipairs(cells(g)) do put(o, c[1] + 1, c[2] + 1, c[3]) end
  return o
end

-- the khopesh: a lapis-wrapped grip, a short bronze shank, and the big hooked blade
-- whose OUTER curve is the edge - the shape that says Egypt before any colour does
do
  local g = newGrid(22, 22)
  local A, B = { x = 14.2, y = 9.6, r = 7.8 }, { x = 11.4, y = 13.2, r = 6.6 }
  crescent(g, A, B, function(x, y) return x > 8.5 and y < 16.5 end,
    { GOLDR[1], GOLDR[2], GOLDR[3], GOLDR[4], GOLDR[5] })
  haft(g, 6.4, 15.8, 9.4, 6.4, 1.0, GOLDR[2], GOLDR[3], GOLDR[4])    -- the shank, running up into the hook
  haft(g, 1.2, 20.8, 6.6, 15.6, 1.05, LAPIS[2], LAPIS[3], LAPIS[4])  -- the grip
  stamp(g, { "y.", "gy" }, { y = GOLDR[2], g = GOLDR[4] }, 3, 18)     -- a gold band on the grip
  stamp(g, { ".GG", "GGg", "gg." }, { G = GOLDR[2], g = GOLDR[4] }, 5, 14) -- the guard
  stamp(g, { "Gg", "gg" }, { G = GOLDR[2], g = GOLDR[5] }, 0, 20)      -- the pommel
  put(g, 6, 15, TURQ[2])
  outline(g)
  fixed(I .. "Weapons/Scarab/SultansKhopesh.png", 22, 22, g)
end

-- the bow: upright, string on the left, the limbs two gold wing-cases with a lapis seam,
-- flicking forward at the tips; a scarab jewel in the grip
do
  local g = newGrid(12, 24)
  for y = 1, 22 do
    local t = (y - 0.5) / 22
    local cx = 2.4 + 6.2 * math.sin(math.pi * t)
    if y <= 2 or y >= 21 then cx = cx + 1 end           -- the recurve
    local w = (y >= 9 and y <= 14) and 1.6 or (1.2 + 0.6 * math.sin(math.pi * t))
    for x = math.floor(cx - w + 0.5), math.floor(cx + w - 0.5) do
      local u = (x + 0.5 - (cx - w)) / (2 * w)
      local col = u < 0.34 and GOLDR[4] or (u > 0.66 and (y < 12 and GOLDR[1] or GOLDR[3]) or GOLDR[2])
      if y >= 10 and y <= 13 then col = (u < 0.5) and LAPIS[3] or LAPIS[2] end
      if y % 5 == 2 and u > 0.34 and u < 0.66 then col = LAPIS[2] end
      put(g, x, y, col)
    end
  end
  stamp(g, { "J", "j" }, { J = TURQ[1], j = TURQ[2] }, 9, 11)
  for y = 2, 21 do put(g, 1, y, SANDR[1]) end          -- the string
  outline(g)
  fixed(I .. "Weapons/Scarab/SunwingBow.png", 12, 24, g)
end

-- the was-sceptre: a straight gold-and-lapis staff, the forked foot at the bottom-left,
-- an angled beast's head at the top-right with a sun for an eye
do
  local g = newGrid(22, 22)
  haft(g, 3.0, 19.0, 15.0, 7.0, 0.8, GOLDR[2], GOLDR[3], GOLDR[4], { 0.25, 0.5, 0.75 })
  for _, t in ipairs({ 0.3, 0.55, 0.8 }) do
    local x, y = 3.0 + 12 * t, 19.0 - 12 * t
    put(g, math.floor(x), math.floor(y), LAPIS[2])
  end
  stamp(g, { "G..", "gG.", ".gG" }, { G = GOLDR[2], g = GOLDR[4] }, 0, 19)   -- one tine of the fork
  stamp(g, { "..G", ".Gg", "Gg." }, { G = GOLDR[2], g = GOLDR[4] }, 3, 19)   -- the other
  stamp(g, {
    "..ee....",
    "..eLe...",
    ".GLLGG..",
    "GGGOGGGG",
    "gGGGGggGg",
    ".ggggg..g",
    "..gg.....",
  }, { e = GOLDR[3], L = LAPIS[2], G = GOLDR[2], g = GOLDR[4], O = SUNR[4] }, 13, 1)
  outline(g)
  fixed(I .. "Weapons/Scarab/SunScepter.png", 22, 22, g)
end

-- the beetling staff: a plain dark sandwood staff with a round lapis scarab set on top
-- (wings shut - the winged scarab belongs to the Carapace)
do
  local g = newGrid(20, 20)
  haft(g, 1.5, 18.5, 12.5, 7.5, 0.85, SANDR[3], SANDR[4], SANDR[5], { 0.35, 0.7 })
  stamp(g, { "GG", "Gg" }, { G = GOLDR[2], g = GOLDR[4] }, 11, 7)   -- the gold cup
  stamp(g, { "l.....l", ".......", "l.....l", ".......", "l.....l" }, { l = GOLDR[4] }, 11, 2) -- legs
  stamp(g, { ".GG.", "GyGg", ".gg." }, { G = GOLDR[2], g = GOLDR[4], y = GOLDR[1] }, 13, 0)   -- the head
  dome(g, 14.5, 6.5, 3.6, 4.2, { LAPIS[1], LAPIS[1], LAPIS[2], LAPIS[3], LAPIS[4] })
  for y = 3, 10 do if get(g, 14, y) then put(g, 14, y, GOLDR[3]) end end      -- the shell's seam
  outline(g)
  fixed(I .. "Weapons/Scarab/BeetlingStaff.png", 20, 20, g)
end

-- the Khepri Scythe: long dark haft, a broad gold blade out of the neck that sweeps
-- right and drops below it to a hooked point, concave edge toward the wielder; a sun
-- disc ringed in lapis set in the neck
do
  local g = newGrid(24, 24)
  haft(g, 1.3, 22.8, 9.6, 4.2, 1.1, BRONZE[2], BRONZE[3], BRONZE[4], { 0.3, 0.62 })
  for _, t in ipairs({ 0.12, 0.2 }) do put(g, math.floor(1.3 + 8.3 * t), math.floor(22.8 - 18.6 * t), LAPIS[2]) end
  local A, B = { x = 15.4, y = 10.2, r = 9.8 }, { x = 11.6, y = 14.0, r = 8.4 }
  crescent(g, A, B, function(x, y) return x > 9.6 and y < 19.5 end,
    { GOLDR[1], GOLDR[2], GOLDR[3], GOLDR[4], SUNR[1] })
  -- the rib, following the curve of the back
  eachBladeCell(g, A, B, -170, function(x, y, s, fromSpine, fromEdge)
    if fromSpine > 1.3 and fromSpine < 2.2 and fromEdge > 1 then put(g, x, y, LAPIS[3]) end
  end)
  dome(g, 9.5, 4.5, 2.4, 2.4, { SUNR[1], SUNR[2], SUNR[3], SUNR[4], SUNR[5] })
  outline(g)
  fixed(I .. "Weapons/Scarab/KhepriScythe.png", 24, 24, g)
end

-- the Sultan's Carapace: a heart-scarab amulet, top-down, wings spread in lapis and
-- turquoise feathers, a sun held over its head
do
  local g = newGrid(1, 1)
  stamp(g, {
    "......oOo......",
    ".....oWYo......",
    "......oo.......",
    "......hh.......",
    "LL...yGGg...LL.",
    "TLLl.GyGg.lLLT.",
    ".TLLLGGGgLLLT..",
    "..TTLGGGgLTT...",
    "...TTgGGgTT....",
    ".....gGGg......",
    "......gg.......",
  }, { o = SUNR[4], O = SUNR[3], W = SUNR[1], Y = SUNR[2], h = GOLDR[4], y = GOLDR[1], G = GOLDR[2],
       g = GOLDR[4], L = LAPIS[2], l = LAPIS[3], T = TURQ[2] }, 0, 0)
  outline(g)
  save(g, I .. "Accessories/SultansCarapace.png")
end

-- the Sun Idol: a gold scarab on a sandstone plinth, holding the sun up over itself
do
  local g = newGrid(1, 1)
  stamp(g, {
    "....oOOo....",
    "...oOWWOo...",
    "...OWYYWO...",
    "...oOYYOo...",
    "....oOOo....",
    "..l..gg..l..",
    "..lgGGGGgl..",
    ".lgGyyGGGgl.",
    "..gGyGGGGg..",
    "..gLTLLTLg..",
    "...gggggg...",
    ".aSSSSSSSSs.",
    ".SSbSSSSbSs.",
    "assssssssssd",
  }, { o = SUNR[4], O = SUNR[3], W = SUNR[1], Y = SUNR[2], l = BRONZE[2], g = GOLDR[4], G = GOLDR[2],
       y = GOLDR[1], L = LAPIS[2], T = TURQ[2], a = STONE2[1], S = STONE2[2], s = STONE2[3], b = STONE2[4],
       d = STONE2[4] }, 0, 0)
  outline(g)
  save(g, I .. "Consumables/SunIdol.png")
end

-- the treasure bag: a lapis cloth sack, a gold cord, a gold scarab clasp
do
  local g = newGrid(1, 1)
  stamp(g, {
    "....L..L.....",
    "...LlL.lL....",
    "....yGGg.....",
    "....LlLl.....",
    "..LLLLLLLl...",
    ".LLLyGGgLll..",
    "LLLLGGJGLlll.",
    "LLLLgGGgLlll.",
    "LLLLLggLLlll.",
    "LLLLLLLLllll.",
    ".lLLLLLLlll..",
    "..llllllll...",
  }, { L = LAPIS[2], l = LAPIS[3], y = GOLDR[1], G = GOLDR[2], g = GOLDR[4], J = SUNR[4] }, 0, 0)
  put(g, 1, 6, LAPIS[1]); put(g, 1, 7, LAPIS[1]); put(g, 2, 5, LAPIS[1])
  outline(g)
  save(g, I .. "Consumables/ScarabSultanBag.png")
end

-- the materials: a curved gold shell plate with a lapis seam; a heap of glowing sand
do
  local g = newGrid(1, 1)
  stamp(g, {
    "...yyGG...",
    ".yyGGGGgg.",
    "yGGLLLLGgg",
    "GGLGGGGLgg",
    "GGGGGGGggg",
    ".gggggggg.",
    "..hhhhhh..",
  }, { y = GOLDR[1], G = GOLDR[2], g = GOLDR[4], L = LAPIS[2], h = GOLDR[5] }, 0, 0)
  outline(g)
  save(g, I .. "Materials/GildedChitin.png")
end
do
  local g = newGrid(1, 1)
  stamp(g, {
    "....W.....",
    "...YsY....",
    "..YSsSs.Y.",
    ".YSSOSSs..",
    "YSSSSOSSs.",
    "ssSSSSSsss",
  }, { W = SUNR[1], Y = SUNR[2], S = SANDR[2], s = SANDR[3], O = SUNR[3] }, 0, 0)
  outline(g)
  save(g, I .. "Materials/Sunsand.png")
end

-- the mask: the Sultan's crowned shell as a helm on the player's head box (cols 5-15,
-- rows 5-14), the crown above it, a dark visor band with the scarab's orange eye-gems
-- on the eye line (rows 10-11)
do
  local MASK = {
    "..y..y..y..",
    "..yGyGyGy..",
    "..GGGJGGG..",
    ".gLgjjgLgg.",
    ".GGyyGGGGg.",
    "GGyGGGGGGgg",
    "GyGGGGGGGgg",
    "LLLLLLLLLll",
    "GTddddddTgg",
    "GEddddddEgg",
    "GGGGGGGGGgg",
    "gGGgGGgGGg.",
    "g.g.g.g.g..",
  }
  local pal = { y = GOLDR[1], G = GOLDR[2], g = GOLDR[4], L = LAPIS[2], l = LAPIS[3], J = SUNR[4], j = SUNR[5],
                T = TURQ[2], d = GOLDR[6], E = SUNR[4] }
  local g = newGrid(20, 28)
  stamp(g, MASK, pal, 5, 2)
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
  saveImage(sheetImg, I .. "Armor/ScarabSultanMask_Head.png")
  save(g, I .. "Armor/ScarabSultanMask.png")
end

-- the buff icon: the lapis beetling, top-down, on its own
do
  local g = newGrid(16, 16)
  stamp(g, {
    "......gg......",
    ".....gGGg.....",
    "..l..gGGg..l..",
    "...lLLLLLLl...",
    "..lLlLLLlLLl..",
    ".l.LlLLLLlL.l.",
    "...LLLyLLLl...",
    "..lLLLyLLLLl..",
    "...LLLyLLLl...",
    ".l..LLyLLl..l.",
    "....lLLLl.....",
    "......l.......",
  }, { g = GOLDR[4], G = GOLDR[2], L = LAPIS[2], l = LAPIS[3], y = GOLDR[2] }, 1, 2)
  outline(g)
  fixed("Content/Buffs/BeetlingBuff.png", 16, 16, g)
end

print("items: done")
