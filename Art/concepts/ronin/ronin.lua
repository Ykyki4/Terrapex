-- Ronin set: red-lacquered samurai armour. Concept art, not shipped.
--
--   aseprite -b --script-param root=<this folder> --script ronin.lua
--
-- Authored at half resolution and blitted as 2x2 blocks, the way Calamity draws
-- its armour. Three materials, each with one job:
--   red lacquer  - every plate: kabuto neck-guard, cuirass lames, sode, shins
--   black        - lacing between lames, the kabuto bowl, sleeves, hakama
--   gold         - only the crescent, the brim and a few trims
-- The lames are the read: a red band, a laced black band, a red band. That is
-- what says "samurai" at 20 pixels, so it is kept on every plate.
--
-- Head, torso, sode and the three icons are hand-drawn maps. Arms and legs are
-- animation geometry: ExampleMod's masks are already on the 2x2 grid, so their
-- cells and outline are taken exactly and only the paint is ours.

local root = app.params.root
local exRoot = root .. "/../../reference/examplemod/"
local pc = app.pixelColor

-- Every ramp hue-shifts, the way Calamity's do: red lacquer runs from peach
-- through scarlet into crimson-magenta, gold browns out in shadow, and the
-- black lacquer is cool, with a blue sheen where the light catches it.
local P = {
  O = "12080f", -- outline
  ["1"] = "ffb27a", ["2"] = "f2603f", ["3"] = "c8283a", ["4"] = "861a3e", ["5"] = "4a0f2e", -- red
  Y = "fff1a8", G = "f5c04a", g = "c7802a", b = "7a3e1e", -- gold
  w = "8fa3c4", j = "5a5c7a", k = "3a3654", K = "241f36", -- black lacquer
}
local C = {}
for key, hex in pairs(P) do
  C[key] = pc.rgba(tonumber(hex:sub(1, 2), 16), tonumber(hex:sub(3, 4), 16), tonumber(hex:sub(5, 6), 16), 255)
end

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
-- Kabuto: black bowl, gold crescent maedate, gold brim, and a red two-lame
-- shikoro flaring down the back. Built around the vanilla head, measured from
-- Player_0_0: skin at cols 6-14 / rows 6-13, the eye at col 10 rows 10-11, the
-- face at cols 11-14. The bowl and brim stop above the eye and the shikoro
-- stays behind col 9 and ends at row 11, clear of the sode, so the face shows -- a cheek guard here once put a
-- black hole where the eye should be.
local HEAD = {
  "....................",
  "....................",
  "....................",
  ".........Y....G.....",
  "..........G..g......",
  "........wjkGgK......",
  ".......jwjkkkkK.....",
  "......jkkkkkkkK.....",
  "......YYGGGGGGggb...",
  ".....1223...........",
  ".....4555...........",
  "....12334...........",
}

---------------------------------------------------------------- torso
-- Do: gold shoulder straps, two red lames laced in black, a black obi with a
-- gold knot, two red kusazuri hanging below it.
local TORSO = {
  "Gg...gb",
  "1222334",
  "5K5K5K5",
  "1223344",
  "5K5K5K5",
  "jkkYKKK",
  "123.234",
  "455.455",
}
local TORSO_X, TORSO_Y = 7, 13

-- sode: a shoulder guard the width of the upper arm it sits on (the vanilla
-- arm hangs at cols 4-8); any wider and it reads as a backpack
local SODE = {
  "1223",
  "5K5K",
  "2334",
}
local SODE_X, SODE_Y = 4, 13

---------------------------------------------------------------- icons
-- the do, with the sode hung one lame lower on either side and split from it
-- by a single outline line, so it reads as one piece but not as one brick
local DO_ICON = {
  "......Gg...gb......",
  ".YGGg.1222334.GGgb.",
  "12234.5k5k5K5.22344",
  "5k5k5.1223344.5k5K5",
  "22334.5k5k5K5.23344",
  "45555.jkkYKKK.45555",
  ".jkK..123.234..kKK.",
  ".123..455.455..234.",
  ".455...........455.",
}

-- greaves: two shins side by side, one outline line between them, toes out
local SUNEATE_ICON = {
  "..wjk.jkK..",
  "..jkK.kKK..",
  "..123.234..",
  "..2G4.3g4..",
  "..2G4.3g4..",
  "..334.344..",
  ".jkkK.kkKK.",
  "kKKKK.KKKKK",
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
  save(img, "RoninKabuto_Head")
  icon(g, "RoninKabuto")
end

-- an arm is a black sleeve from the shoulder and a red kote plate at the hand
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
    local corner = not isFill(g, x, y - 1) and not isFill(g, x - 1, y)
    if d >= maxd - 2.2 and #cells > 3 then
      ch = corner and "1" or (lit and "2" or (shade and "4" or "3"))
    else
      ch = corner and "w" or (lit and "j" or (shade and "K" or "k"))
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
        g = {}; stampMap(g, SODE, SODE_X, SODE_Y); outline(g)
      elseif not (cx == 1 and (cy == 1 or cy == 3)) then
        g = readMask(src, cx * 40, cy * 56)
        if cx == 8 then paintArm(g, 12.5, 12) else paintArm(g, 6.5, 13.5) end
      end
      if g then blit(img, g, cx * 40, cy * 56, { cy * 56, cy * 56 + 56 }) end
    end
  end
  save(img, "RoninDo_Body")

  local gi = {}
  stampMap(gi, DO_ICON, 0, 0)
  outline(gi)
  icon(gi, "RoninDo")
end

-- legs: each leg is a 4-connected run of fill cells between ExampleMod's own
-- outline. Down each leg: black hakama, red knee, red suneate with a gold
-- spine, black sandal. The smaller leg is the far one, one step darker.
local DARKER = { w = "j", j = "k", k = "K", ["1"] = "2", ["2"] = "3", ["3"] = "4", ["4"] = "5", Y = "G", G = "g", g = "b" }

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
      local mid = not first and not last
      if y == bottom then ch = first and "k" or "K"                    -- sandal
      elseif rel <= 1 then ch = first and "j" or (last and "K" or "k") -- hakama
      elseif rel == 2 then ch = first and "1" or (last and "4" or "2") -- knee
      else ch = first and "2" or (last and "4" or "3") end             -- suneate
      if rel >= 3 and y < bottom and mid and not isFill(g, x - 2, y) then ch = "G" end
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
  save(img, "RoninSuneate_Legs")

  local gi = {}
  stampMap(gi, SUNEATE_ICON, 0, 0)
  outline(gi)
  icon(gi, "RoninSuneate")
end

head()
body()
legs()
print("ronin: done")
