-- Scarecrow set: the reaper class's first armour (pre-boss). Shipping art.
--
--   aseprite -b --script-param root=<repo>/Content/Items/Armor --script Art/reaper/scarecrow.lua
--
-- Built exactly the way the Ronin set was (Art/concepts/ronin/ronin.lua): half-size
-- char maps for the hat, the shirt and the shoulder, ExampleMod's arm and leg masks
-- taken cell for cell and painted by rule, all blitted 2x2. The reaper sets are
-- the class's 2x2 armour; the rift tiers below stay 1:1.
--
-- A scarecrow, so the read is straw: a wide-brimmed straw hat with a red band and
-- straw sticking out under it at the back, a burlap shirt with a red patch and a
-- rope belt, washed-blue trousers patched at the knee, straw at every cuff.

local root = app.params.root
local exRoot = root .. "/../../../Art/reference/examplemod/"
local pc = app.pixelColor

-- Every ramp hue-shifts, the way Calamity's do: red lacquer runs from peach
-- through scarlet into crimson-magenta, gold browns out in shadow, and the
-- black lacquer is cool, with a blue sheen where the light catches it.
local P = {
  O = "1e140f", -- outline: a warm near-black, straw and sacking are not stone
  Y = "fff0a0", S = "e8c860", s = "c09a40", z = "7a5a28",              -- straw
  a = "d8aa78", b = "a8784c", c = "74503a", d = "4a3024",              -- burlap
  P = "d8483a", p = "9a2a2a",                                           -- red patch, red band
  ["1"] = "9aaad0", ["2"] = "6a7aa8", ["3"] = "4a5680", ["4"] = "2e3456", -- washed denim
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
  ".........zS.........",
  "........zSSs........",
  ".......zSSSSs.......",
  "......zSSSSSSs......",
  "......SPPPPPPs......",
  "...YYSSSSSSSSSss....",
  "....ssssssssssszz...",
  ".....YS.............",
  "....Y.SY............",
  ".....S..............",
}

---------------------------------------------------------------- torso
-- Do: gold shoulder straps, two red lames laced in black, a black obi with a
-- gold knot, two red kusazuri hanging below it.
local TORSO = {
  "ab...bc",
  "abbbbbc",
  "abPPbbc",
  "abppbcc",
  "abbbbcc",
  "SsSsSsS",
  "bbc.bbc",
  "Y.S.Y.S",
}
local TORSO_X, TORSO_Y = 7, 13

-- sode: a shoulder guard the width of the upper arm it sits on (the vanilla
-- arm hangs at cols 4-8); any wider and it reads as a backpack
local SODE = {
  ".Y.",
  "YSs",
}
local SODE_X, SODE_Y = 5, 12

---------------------------------------------------------------- icons
-- the do, with the sode hung one lame lower on either side and split from it
-- by a single outline line, so it reads as one piece but not as one brick
local DO_ICON = {
  "...ab.....bc...",
  ".aabbbbbbbbbcc.",
  "aabbbbPPbbbbccd",
  "SY.abbppbbbc.YS",
  "Y..abbbbbbcc..Y",
  "...SsSsSsSsS...",
  "...bbcc.bbcc...",
  "...Y.S...Y.S...",
}

-- greaves: two shins side by side, one outline line between them, toes out
local SUNEATE_ICON = {
  "1222.12334",
  "1223.12334",
  "1PP3.1PP34",
  "1223.12334",
  "1223.12334",
  "YSSs.YSSs.",
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
  save(img, "ScarecrowHat_Head")
  icon(g, "ScarecrowHat")
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
    if d >= maxd - 1.2 and #cells > 3 then
      ch = corner and "Y" or (lit and "S" or (shade and "z" or "s"))
    else
      ch = corner and "a" or (lit and "a" or (shade and "d" or (x % 3 == 0 and "c" or "b")))
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
  save(img, "ScarecrowShirt_Body")

  local gi = {}
  stampMap(gi, DO_ICON, 0, 0)
  outline(gi)
  icon(gi, "ScarecrowShirt")
end

-- legs: each leg is a 4-connected run of fill cells between ExampleMod's own
-- outline. Down each leg: black hakama, red knee, red suneate with a gold
-- spine, black sandal. The smaller leg is the far one, one step darker.
local DARKER = { ["1"] = "2", ["2"] = "3", ["3"] = "4", P = "p", Y = "S", S = "s", a = "b", b = "c", c = "d" }

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
      local mid = not first and not last
      local ch
      if y == bottom then ch = first and "Y" or "S"                     -- straw out of the cuff
      elseif rel <= 1 then ch = first and "1" or (last and "4" or "2") -- thigh
      elseif rel == 2 then ch = first and "1" or (last and "4" or "P") -- the patched knee
      else ch = first and "2" or (last and "4" or "3") end             -- shin
      if rel == 2 and not mid then ch = first and "1" or "4" end
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
  save(img, "ScarecrowTrousers_Legs")

  local gi = {}
  stampMap(gi, SUNEATE_ICON, 0, 0)
  outline(gi)
  icon(gi, "ScarecrowTrousers")
end

head()
body()
legs()
print("scarecrow: done")
