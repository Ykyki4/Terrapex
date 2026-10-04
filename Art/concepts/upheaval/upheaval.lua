-- Upheaval set: the Breach floor, thrown up and worn. Concept art, not shipped.
--
--   aseprite -b --script-param root=<this folder> --script upheaval.lua
--
-- Everything is authored at half resolution and blitted as 2x2 blocks, the way
-- Calamity draws its armour (measured: 100% of their equip sheets are 2x2).
--
-- Two materials, so the parts separate on the player: stone plate (lavender, the
-- Heave's own ramp) and a plum-rust undersuit. The front arm is mostly undersuit,
-- which is what keeps it readable over a stone chest.
--
-- Head, torso, pauldron and the three icons are hand-drawn maps. Arms and legs
-- are animation geometry: ExampleMod's masks are already on the 2x2 grid, so
-- their cells and their outline are taken exactly, and only the paint is ours.

local root = app.params.root
local exRoot = root .. "/../../reference/examplemod/"
local pc = app.pixelColor

local P = {
  O = "241733", -- outline
  D = "3b3050", d = "584a6e", m = "635478", l = "93849f", h = "c9bdd1", w = "f4efe6", -- stone
  L = "9a5140", M = "5e2f45", N = "3a1c33", -- undersuit
  R = "ff9ad9", r = "d94ecb", p = "8a2f9c", -- rift seam
  B = "e8cf94", -- bone stud
}
local C = {}
for k, hex in pairs(P) do
  C[k] = pc.rgba(tonumber(hex:sub(1, 2), 16), tonumber(hex:sub(3, 4), 16), tonumber(hex:sub(5, 6), 16), 255)
end

-- art-space grid: grid[y][x] = palette char
local function put(g, x, y, ch)
  g[y] = g[y] or {}
  g[y][x] = ch
end
local function get(g, x, y) return g[y] and g[y][x] end

local function stampMap(g, rows, ox, oy)
  for j, s in ipairs(rows) do
    for i = 1, #s do
      local ch = s:sub(i, i)
      if ch ~= "." then put(g, ox + i - 1, oy + j - 1, ch) end
    end
  end
end

-- hard 8-neighbour outline, in art space, so it lands on the 2x2 grid
local function outline(g)
  local add = {}
  for y, row in pairs(g) do
    for x, _ in pairs(row) do
      for dy = -1, 1 do
        for dx = -1, 1 do
          if not get(g, x + dx, y + dy) then add[#add + 1] = { x + dx, y + dy } end
        end
      end
    end
  end
  for _, a in ipairs(add) do put(g, a[1], a[2], "O") end
end

local function blit(img, g, px, py, clip)
  for y, row in pairs(g) do
    for x, ch in pairs(row) do
      for sy = 0, 1 do
        for sx = 0, 1 do
          local X, Y = px + x * 2 + sx, py + y * 2 + sy
          if not clip or (Y >= clip[1] and Y < clip[2]) then img:drawPixel(X, Y, C[ch]) end
        end
      end
    end
  end
end

local function bounds(g)
  local x0, y0, x1, y1 = 1e9, 1e9, -1e9, -1e9
  for y, row in pairs(g) do
    for x, _ in pairs(row) do
      x0, y0, x1, y1 = math.min(x0, x), math.min(y0, y), math.max(x1, x), math.max(y1, y)
    end
  end
  return x0, y0, x1, y1
end

local function save(img, name)
  local spr = Sprite(img.width, img.height, ColorMode.RGB)
  spr.cels[1].image = img
  spr:saveAs(root .. "/" .. name .. ".png")
end

local function icon(g, name)
  local x0, y0, x1, y1 = bounds(g)
  local img = Image((x1 - x0 + 1) * 2, (y1 - y0 + 1) * 2, ColorMode.RGB)
  blit(img, g, -x0 * 2, -y0 * 2)
  save(img, name)
end

local function loadRGB(path)
  local spr = app.open(path)
  if spr.colorMode ~= ColorMode.RGB then app.command.ChangePixelFormat { format = "rgb" } end
  local img = Image(spr.width, spr.height, ColorMode.RGB)
  img:drawSprite(spr, 1, Point(0, 0))
  return img
end

-- ExampleMod's cells read one-to-one: dark cells are its outline ("O"),
-- everything else is fill ("o") for us to paint.
local function readMask(src, px, py)
  local g = {}
  for ay = 0, 27 do
    for ax = 0, 19 do
      local v = src:getPixel(px + ax * 2, py + ay * 2)
      if pc.rgbaA(v) > 0 then
        local lum = (0.299 * pc.rgbaR(v) + 0.587 * pc.rgbaG(v) + 0.114 * pc.rgbaB(v)) / 255
        put(g, ax, ay, lum < 0.20 and "O" or "o")
      end
    end
  end
  return g
end
local function isFill(g, x, y)
  local c = get(g, x, y)
  return c ~= nil and c ~= "O"
end

---------------------------------------------------------------- head
-- A short stone great-helm. One slab has heaved up off the crown and leans back,
-- and the rift shows in the crack at its root. The face is one flat vertical
-- plate with a lit slit -- a front that leans or juts turns the whole helm into
-- a beak. Slot box: cols 5-15, rows 5-14.
local HEAD = {
  "....................",
  "....................",
  "....................",
  "......hh............",
  "......hlm...........",
  ".......lmRhhhh......",
  "......hllllllllh....",
  "......lmmmmmmmmd....",
  "......mmDDDDDDDD....",
  "......mDRRRRRRRw....",
  "......dmDDDDDDDd....",
  "......dmmd..Dmmd....",
  "......DddD...DD.....",
  ".......DD...........",
}

---------------------------------------------------------------- torso
-- A stone breastplate over the undersuit, split down the sternum by a rift
-- crack; plum belt with bone studs, two stone tassets.
local TORSO = {
  "hl...lh",
  "hlhhhld",
  "MlmmRmd",
  "MlmRmdd",
  "MmrpmdM",
  "MBMMMBM",
  "lmd.lmd",
  "Dd...Dd",
}
local TORSO_X, TORSO_Y = 7, 13

local PAULDRON = {
  ".hhh.",
  "hlllD",
  "lmRmD",
  ".DDD.",
}
local PAULDRON_X, PAULDRON_Y = 3, 12

---------------------------------------------------------------- icons
-- one piece: the pauldrons sit on the shoulders, the arms hang clear of the chest
local PLATE_ICON = {
  ".hhh.hl...lh.hhh.",
  "hlllDhlhhhldhlllD",
  "lmRmDMlmmRmdlmRmD",
  ".DDD.MlmRmdd.DDD.",
  ".LMM.MmrpmdM.LMM.",
  ".LMM.MBMMMBM.LMM.",
  ".LMN.lmd.lmd.LMN.",
  ".hll.Dd...Dd.hll.",
  ".lmd.........lmd.",
}

-- two boots, toes turned out
local GREAVES_ICON = {
  "..LMM...LMM..",
  "..LMN...LMN..",
  "..hhl...hhl..",
  "..lrd...lrd..",
  "..lmd...lmd..",
  "..lmd...lmd..",
  ".lmmd...lmmd.",
  "lmmmd...lmmmd",
  "DDDDD...DDDDD",
}

---------------------------------------------------------------- build
local function head()
  local g = {}
  stampMap(g, HEAD, 0, 0)
  outline(g)
  local img = Image(40, 1120, ColorMode.RGB)
  local high = { [7] = true, [8] = true, [9] = true, [14] = true, [15] = true, [16] = true }
  for f = 0, 19 do
    local top = f * 56
    blit(img, g, 0, top - (high[f] and 2 or 0), { top, top + 56 })
  end
  save(img, "UpheavalHelm_Head")
  icon(g, "UpheavalHelm")
end

-- an arm is undersuit from the shoulder and a stone bracer at the hand end
local function paintArm(g, pivotX, pivotY)
  local cells, maxd = {}, 0
  for y, row in pairs(g) do
    for x, c in pairs(row) do
      if c == "o" then
        local d = math.sqrt((x - pivotX) ^ 2 + (y - pivotY) ^ 2)
        cells[#cells + 1] = { x, y, d }
        maxd = math.max(maxd, d)
      end
    end
  end
  for _, c in ipairs(cells) do
    local x, y, d = c[1], c[2], c[3]
    local lit = not isFill(g, x, y - 1) or not isFill(g, x - 1, y)
    local shade = not isFill(g, x, y + 1) and not isFill(g, x + 1, y)
    local ch
    if d >= maxd - 2.2 and #cells > 3 then
      ch = lit and "l" or (shade and "d" or "m")
    else
      ch = lit and "L" or (shade and "N" or "M")
    end
    put(g, x, y, ch)
  end
end

local function body()
  local src = loadRGB(exRoot .. "ExampleBreastplate_Body.png")
  local img = Image(360, 224, ColorMode.RGB)
  for cy = 0, 3 do
    for cx = 0, 8 do
      local g
      if cx <= 1 and (cy == 0 or cy == 2) then
        g = {}; stampMap(g, TORSO, TORSO_X, TORSO_Y); outline(g)
      elseif cx == 0 and (cy == 1 or cy == 3) then
        g = {}; stampMap(g, PAULDRON, PAULDRON_X, PAULDRON_Y); outline(g)
      elseif not (cx == 1 and (cy == 1 or cy == 3)) then
        g = readMask(src, cx * 40, cy * 56)
        if cx == 8 then paintArm(g, 12.5, 12) else paintArm(g, 6.5, 13.5) end
      end
      if g then blit(img, g, cx * 40, cy * 56, { cy * 56, cy * 56 + 56 }) end
    end
  end
  save(img, "UpheavalPlate_Body")

  local gi = {}
  stampMap(gi, PLATE_ICON, 0, 0)
  outline(gi)
  icon(gi, "UpheavalPlate")
end

-- legs: each leg is a 4-connected run of fill cells between ExampleMod's own
-- outline. Down each leg: undersuit thigh, stone knee, stone greave, boot.
-- The smaller leg is the far one and takes the whole ramp one step darker.
local DARKER = { h = "l", l = "m", m = "d", d = "D", L = "M", M = "N", r = "p" }

local function legParts(g)
  local seen, parts = {}, {}
  for y, row in pairs(g) do
    for x, c in pairs(row) do
      local key = y * 100 + x
      if c == "o" and not seen[key] then
        local part, stack = {}, { { x, y } }
        seen[key] = true
        while #stack > 0 do
          local cell = table.remove(stack)
          part[#part + 1] = cell
          for _, dxy in ipairs({ { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } }) do
            local nx, ny = cell[1] + dxy[1], cell[2] + dxy[2]
            local k = ny * 100 + nx
            if get(g, nx, ny) == "o" and not seen[k] then
              seen[k] = true
              stack[#stack + 1] = { nx, ny }
            end
          end
        end
        parts[#parts + 1] = part
      end
    end
  end
  table.sort(parts, function(a, b) return #a > #b end)
  return parts
end

local function paintLegs(g)
  local parts = legParts(g)
  for i, part in ipairs(parts) do
    local top, bottom = 1e9, -1e9
    for _, c in ipairs(part) do top, bottom = math.min(top, c[2]), math.max(bottom, c[2]) end
    for _, c in ipairs(part) do
      local x, y = c[1], c[2]
      local rel = y - top
      local first = not isFill(g, x - 1, y)
      local last = not isFill(g, x + 1, y)
      local ch
      if y == bottom then ch = first and "d" or "D"                    -- boot
      elseif rel <= 1 then ch = first and "L" or (last and "N" or "M") -- thigh
      elseif rel == 2 then ch = first and "h" or (last and "d" or "l") -- knee
      else ch = first and "l" or (last and "d" or "m") end             -- greave
      if rel == 3 and not first and not last then ch = "r" end
      if i > 1 then ch = DARKER[ch] or ch end
      put(g, x, y, ch)
    end
  end
end

local function legs()
  local src = loadRGB(exRoot .. "ExampleLeggings_Legs.png")
  local img = Image(40, 1120, ColorMode.RGB)
  for f = 0, 19 do
    local g = readMask(src, 0, f * 56)
    paintLegs(g)
    blit(img, g, 0, f * 56, { f * 56, f * 56 + 56 })
  end
  save(img, "UpheavalGreaves_Legs")

  local gi = {}
  stampMap(gi, GREAVES_ICON, 0, 0)
  outline(gi)
  icon(gi, "UpheavalGreaves")
end

head()
body()
legs()
print("upheaval: done")
