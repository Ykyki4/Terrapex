-- Shared bits for the Old Shellback's art: the ocean palettes and a few helpers on
-- top of Art/reaper/lib.lua. Loaded with
--   dofile(app.params.root .. "/Art/shellback/sea.lua")
-- Nothing here is rift: the set is sea, sand, ship wood, barnacle and crab shell.

dofile(app.params.root .. "/Art/reaper/lib.lua")

-- a deep sea navy instead of the mod's violet outline: this boss is not a rift thing
OUTLINE = hex("161a2a")

-- rotten ship wood: warm lit end, shadows toward cold purple-brown
WOODP = { L = hex("d8a868"), B = hex("a0703f"), D = hex("6e4632"), S = hex("4a2e3a"), K = hex("2a1c2c") }
-- barnacles: bone-white tops, blue-grey shadow, a dark mouth
BARN = { W = hex("f6f2e4"), G = hex("cdc8b6"), g = hex("8f97a0"), d = hex("4a5262") }
-- hermit crab shell: coral orange lit warm, shadow toward crimson-violet
CRAB = { L = hex("ffc08a"), B = hex("f2703f"), S = hex("c8323a"), D = hex("7e1c40"), K = hex("4a0f2e") }
-- sea water / brine
SEA = { W = hex("e8fff8"), L = hex("9aeedd"), B = hex("3fb8b0"), D = hex("237a86"), K = hex("16485a") }
KELP = { L = hex("8cc060"), B = hex("4f8a4a"), D = hex("2e5a44") }
CORAL = { L = hex("ffa0b0"), B = hex("e2557a"), D = hex("a03060") }
-- ship iron, gone cold and blue in the water, with rust at the seams
IRONS = { L = hex("dde6ea"), B = hex("9fb0b8"), D = hex("62748a"), K = hex("3a4458") }
RUSTS = { L = hex("e08850"), B = hex("b0582c"), D = hex("7a3420") }
CANVAS = { L = hex("f8ecd0"), B = hex("d8c49a"), D = hex("a08a6a") }
SANDP = { L = hex("f8e6b4"), B = hex("e0c084"), D = hex("b8935e"), K = hex("86663f") }
BONE = { L = hex("fff0c8"), B = hex("e8cf94"), D = hex("c0a066") }
GOLDP = { L = hex("fff1a8"), B = hex("f5c04a"), D = hex("c7802a"), K = hex("7a3e1e") }
EYEY = hex("fff4c0")
PUPIL = hex("120e18")

-- a 1-cell line between two cells, every cell on it painted
function line(g, x0, y0, x1, y1, col)
  local dx, dy = math.abs(x1 - x0), -math.abs(y1 - y0)
  local sx, sy = x0 < x1 and 1 or -1, y0 < y1 and 1 or -1
  local err = dx + dy
  while true do
    put(g, x0, y0, col)
    if x0 == x1 and y0 == y1 then break end
    local e2 = 2 * err
    if e2 >= dy then err = err + dy; x0 = x0 + sx end
    if e2 <= dx then err = err + dx; y0 = y0 + sy end
  end
end

-- a stamp that only paints where something is already drawn (for cracks, spots)
function overpaint(g, rows, pal, ox, oy)
  for j, s in ipairs(rows) do
    for i = 1, #s do
      local ch = s:sub(i, i)
      if ch ~= "." and pal[ch] and get(g, ox + i - 1, oy + j - 1) then put(g, ox + i - 1, oy + j - 1, pal[ch]) end
    end
  end
end

-- blit a grid into an image at a 2x2 grid, clipped to (w, h) art cells, at pixel offset (px, py)
function blit(img, g, w, h, px, py)
  for _, c in ipairs(cells(g)) do
    local x, y = c[1], c[2]
    if x >= 0 and x < w and y >= 0 and y < h then
      for sy = 0, 1 do
        for sx = 0, 1 do img:drawPixel(px + x * 2 + sx, py + y * 2 + sy, c[3]) end
      end
    end
  end
end

function saveImage(img, path)
  local spr = Sprite(img.width, img.height, ColorMode.RGB)
  spr.cels[1].image = img
  spr:saveAs(root .. "/" .. path)
end

-- a stack of frames, each authored by fn(f) -> grid, into one vertical sheet
function sheet(path, w, h, count, fn)
  local img = Image(w * 2, h * 2 * count, ColorMode.RGB)
  for f = 1, count do blit(img, fn(f), w, h, 0, (f - 1) * h * 2) end
  saveImage(img, path)
end

-- a fixed-size icon (buff icons, the map head): the grid blitted into w x h art cells
function icon(path, w, h, g)
  local img = Image(w * 2, h * 2, ColorMode.RGB)
  blit(img, g, w, h, 0, 0)
  saveImage(img, path)
end
