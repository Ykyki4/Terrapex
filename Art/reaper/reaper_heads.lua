-- The reaper heads of the four multi-head sets (T3-T6). Run from the repo root:
--
--   aseprite -b --script-param root=<repo> --script Art/reaper/reaper_heads.lua
--
-- These sets are drawn 1:1, so these heads are too: one character is one pixel, and
-- the map is the 22-wide head box (x 10-31) from y 6 down. CLAUDE.md: within one
-- set, never mix the grids.
--
-- The reaper's head is a hood with the face in shadow and lights for eyes - the one
-- silhouette in each set that is not a helmet. Each tier then makes it its own:
-- riftsteel plates with a rift crest, a tall stitched point, a draped silk veil
-- under a dark frame, a smooth white hood with a single warm eye.

local root = app.params.root
local pc = app.pixelColor

local function hex(h)
  return pc.rgba(tonumber(h:sub(1, 2), 16), tonumber(h:sub(3, 4), 16), tonumber(h:sub(5, 6), 16), 255)
end

local HIGH = { [7] = true, [8] = true, [9] = true, [14] = true, [15] = true, [16] = true }
local X0, Y0 = 10, 6

local function build(map, pal, outlineHex)
  local cells = {}
  for j, s in ipairs(map) do
    for i = 1, #s do
      local ch = s:sub(i, i)
      if pal[ch] then cells[(j - 1) * 100 + (i - 1)] = pal[ch] end
    end
  end
  -- 8-neighbour outline in the set's own darkest tone
  local out = hex(outlineHex)
  local add = {}
  for key, _ in pairs(cells) do
    local y, x = math.floor(key / 100), key % 100
    for dy = -1, 1 do
      for dx = -1, 1 do
        local k = (y + dy) * 100 + (x + dx)
        if x + dx >= 0 and y + dy >= 0 and not cells[k] then add[k] = true end
      end
    end
  end
  for k, _ in pairs(add) do cells[k] = out end
  return cells
end

local function save(cells, name)
  local sheet = Image(40, 1120, ColorMode.RGB)
  for f = 0, 19 do
    local dy = f * 56 - (HIGH[f] and 2 or 0)
    for key, col in pairs(cells) do
      local y, x = math.floor(key / 100), key % 100
      local py = Y0 - 1 + y + dy
      if py >= f * 56 and py < f * 56 + 56 then sheet:drawPixel(X0 - 1 + x, py, col) end
    end
  end
  local spr = Sprite(40, 1120, ColorMode.RGB)
  spr.cels[1].image = sheet
  spr:saveAs(root .. "/Content/Items/Armor/" .. name .. "_Head.png")

  local x0, y0, x1, y1 = 999, 999, -1, -1
  for key, _ in pairs(cells) do
    local y, x = math.floor(key / 100), key % 100
    x0, y0, x1, y1 = math.min(x0, x), math.min(y0, y), math.max(x1, x), math.max(y1, y)
  end
  local icon = Image(x1 - x0 + 1, y1 - y0 + 1, ColorMode.RGB)
  for key, col in pairs(cells) do
    local y, x = math.floor(key / 100), key % 100
    icon:drawPixel(x - x0, y - y0, col)
  end
  local ispr = Sprite(icon.width, icon.height, ColorMode.RGB)
  ispr.cels[1].image = icon
  ispr:saveAs(root .. "/Content/Items/Armor/" .. name .. ".png")
end

-- Riftsteel Mask: plates, not cloth - a hood built from the set's stone, seams
-- between the plates, and a rift crest glowing along the top
save(build({
  ".....AA...............",
  "....1AA2..............",
  "....11223.............",
  "...1122223............",
  "..112222233333........",
  ".11222222233333.......",
  ".1444444422333344.....",
  "12222222223333344A....",
  "122222222333FFFF44A...",
  "12222222333FFFFFF44A..",
  "1222222333FFFFFFFF44A.",
  "1222222333FFFEFFEF44A.",
  "144444433FFFFFFFFFF4A.",
  "122222233FFFFFFFFFF4A.",
  "12222223FFFFFFFFFF44A.",
  "1222223FFFFFFFFFF44A..",
  "122223FFFFFFFFF444A...",
  "12223..FFFFFFF.44A....",
  "1223.........4A.......",
  "123...................",
  "13....................",
}, { ["1"] = hex("c9bdd1"), ["2"] = hex("93849f"), ["3"] = hex("635478"), ["4"] = hex("3f3357"),
     F = hex("171029"), E = hex("ff9ad9"), A = hex("d94ecb") }, "171029"), "RiftsteelMask")

-- Darner Veil: a tall pointed hood of the dark machine, stitched in thread down its
-- back and round the opening - the thread is the only light it has
save(build({
  "........1.............",
  "........12............",
  ".......1123...........",
  "......112223..........",
  ".....1122223..........",
  "....112222233.........",
  "...11222s2223333......",
  "..1122222222333344....",
  ".12222s22223333344A...",
  "122222222333FFFF44A...",
  "1222s2222333FFFFFF44A.",
  "1222222333FFFFFFFF44A.",
  "12s22223333FFEFFEFF4A.",
  "122222233FFFFFFFFFF4A.",
  "12s222233FFFFFFFFFF4A.",
  "12222223FFFFFFFFFF44A.",
  "12s2223FFFFFFFFFF44A..",
  "122223FFFFFFFFF444A...",
  "12223..FFFFFFF.44A....",
  "1223.........4A.......",
  "123...................",
  "13....................",
}, { ["1"] = hex("9fb4bb"), ["2"] = hex("61818c"), ["3"] = hex("3d5560"), ["4"] = hex("22343e"),
     F = hex("0f1c26"), E = hex("7ff2e0"), A = hex("35c9b8"), s = hex("35c9b8") }, "0f1c26"), "DarnerVeil")

-- Weaver Shroud: pale silk draped over a dark frame band, the veil hanging long
-- down the back - a garment, as the whole tier is
save(build({
  "....1..1..............",
  "....12.12.............",
  "...1122223............",
  "..112222223...........",
  "..1122222233333.......",
  ".112222222233333......",
  ".AAAAAAAAAAAAAAAA.....",
  "1222222222333334444...",
  "122222222333FFFF44A...",
  "12222222333FFFFFF44A..",
  "1222222333FFFFFFFF44A.",
  "1222222333FFFEFFEF44A.",
  "122222233FFFFFFFFFF4A.",
  "122222233FFFFFFFFFF4A.",
  "12222223FFFFFFFFFF44A.",
  "1222223FFFFFFFFFF44A..",
  "122223FFFFFFFFF444A...",
  "122223.FFFFFFF.44A....",
  "122223.......4A.......",
  "12223.................",
  "12223.................",
  "1223..................",
  "123...................",
  "13....................",
}, { ["1"] = hex("eef9f6"), ["2"] = hex("c2e0da"), ["3"] = hex("93b8b6"), ["4"] = hex("648d90"),
     F = hex("0a1620"), E = hex("7ff2e0"), A = hex("22343e") }, "0a1620"), "WeaverShroud")

-- First Veil: smooth white, no peak, the opening a single dark band with one warm
-- eye in it - the colour that means being seen
save(build({
  "......................",
  "......................",
  ".....11112............",
  "...1122222223.........",
  "..112222222233........",
  ".11222222222333.......",
  ".122222222223333......",
  "12222222222333334.....",
  "1222222223333344A.....",
  "1222222333FFFFFF4A....",
  "122222333FFFFFFFF4A...",
  "122222333FFFWWFFFF4A..",
  "122222333FFWEEWFFF4A..",
  "122222333FFFWWFFFF4A..",
  "122222333FFFFFFFFF4A..",
  "12222223FFFFFFFFF44A..",
  "1222223FFFFFFFF444A...",
  "122223.FFFFFF.44A.....",
  "1223.........4A.......",
  "123...................",
  "13....................",
}, { ["1"] = hex("ffffff"), ["2"] = hex("eef1f8"), ["3"] = hex("c8cee0"), ["4"] = hex("9aa2bc"),
     F = hex("22263c"), E = hex("ff9f4a"), W = hex("ffd79a"), A = hex("666e8c") }, "0d0f1c"), "FirstVeil")

print("reaper heads: done")
