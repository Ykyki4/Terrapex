-- Shared helpers for the reaper art scripts: the 2x2 art grid, the outline,
-- hafts lit across their width, crescent regions, and the palettes. Loaded with
--   dofile(app.params.root .. "/Art/reaper/lib.lua")
-- Everything here is global on purpose - it is a library for two scripts.

root = app.params.root
pc = app.pixelColor

function hex(h)
  return pc.rgba(tonumber(h:sub(1, 2), 16), tonumber(h:sub(3, 4), 16), tonumber(h:sub(5, 6), 16), 255)
end

OUTLINE = hex("241733")

-- grid helpers --------------------------------------------------------------
-- The grid is sparse and unbounded. w and h are the canvas the shape is
-- authored in, but cells may land outside it: the outline of a blade that
-- touches the canvas edge has to go somewhere, and when it was clipped the
-- sprite looked cut off at the border. save() writes the actual bounding box.
function newGrid(w, h) return { w = w, h = h, rows = {} } end
function put(g, x, y, col)
  g.rows[y] = g.rows[y] or {}
  g.rows[y][x] = col
end
function get(g, x, y)
  local row = g.rows[y]
  return row and row[x] or nil
end
function clear(g, x, y)
  if g.rows[y] then g.rows[y][x] = nil end
end

function cells(g)
  local out = {}
  for y, row in pairs(g.rows) do
    for x, col in pairs(row) do out[#out + 1] = { x, y, col } end
  end
  return out
end

function outline(g)
  local add, seen = {}, {}
  for _, c in ipairs(cells(g)) do
    if c[3] ~= OUTLINE then
      for dy = -1, 1 do
        for dx = -1, 1 do
          local x, y = c[1] + dx, c[2] + dy
          local key = y * 100000 + x
          if not get(g, x, y) and not seen[key] then
            seen[key] = true
            add[#add + 1] = { x, y }
          end
        end
      end
    end
  end
  for _, a in ipairs(add) do put(g, a[1], a[2], OUTLINE) end
end

function save(g, path)
  local x0, y0, x1, y1 = 1e9, 1e9, -1e9, -1e9
  for _, c in ipairs(cells(g)) do
    x0, y0 = math.min(x0, c[1]), math.min(y0, c[2])
    x1, y1 = math.max(x1, c[1]), math.max(y1, c[2])
  end
  local img = Image((x1 - x0 + 1) * 2, (y1 - y0 + 1) * 2, ColorMode.RGB)
  for _, c in ipairs(cells(g)) do
    for sy = 0, 1 do
      for sx = 0, 1 do img:drawPixel((c[1] - x0) * 2 + sx, (c[2] - y0) * 2 + sy, c[3]) end
    end
  end
  local spr = Sprite(img.width, img.height, ColorMode.RGB)
  spr.cels[1].image = img
  spr:saveAs(root .. "/" .. path)
end

-- explicit pixels from a char map, for details no rule should place
function stamp(g, rows, pal, ox, oy)
  for j, s in ipairs(rows) do
    for i = 1, #s do
      local ch = s:sub(i, i)
      if ch ~= "." and pal[ch] then put(g, ox + i - 1, oy + j - 1, pal[ch]) end
    end
  end
end

-- a haft: cells within `w` of the segment a-b, lit across its width (lit edge on
-- the upper-left side, body, dark edge) - never banded along its length
function haft(g, ax, ay, bx, by, w, lit, body, dark, grain)
  local vx, vy = bx - ax, by - ay
  local len = math.sqrt(vx * vx + vy * vy)
  local nx, ny = -vy / len, vx / len
  if nx + ny > 0 then nx, ny = -nx, -ny end -- normal toward the upper-left
  for y = 0, g.h - 1 do
    for x = 0, g.w - 1 do
      local px, py = x + 0.5 - ax, y + 0.5 - ay
      local t = (px * vx + py * vy) / (len * len)
      if t >= 0 and t <= 1 then
        local side = px * nx + py * ny
        if math.abs(side) <= w then
          local col = side > w * 0.35 and lit or (side < -w * 0.35 and dark or body)
          if grain then
            for _, gt in ipairs(grain) do
              if math.abs(t - gt) < 0.5 / len and side <= w * 0.35 then col = dark end
            end
          end
          put(g, x, y, col)
        end
      end
    end
  end
end

-- a crescent blade: inside circle A, outside circle B, clipped by `keep`.
-- ramp = { spine_lit, spine, body, deep, edge }
function crescent(g, A, B, keep, ramp)
  for y = 0, g.h - 1 do
    for x = 0, g.w - 1 do
      local px, py = x + 0.5, y + 0.5
      local da = math.sqrt((px - A.x) ^ 2 + (py - A.y) ^ 2)
      local db = math.sqrt((px - B.x) ^ 2 + (py - B.y) ^ 2)
      if da <= A.r and db > B.r and keep(px, py) then
        local fromSpine = A.r - da
        local fromEdge = db - B.r
        local thick = fromSpine + fromEdge
        local t = fromSpine / math.max(thick, 0.01)
        local col
        if fromEdge < 0.9 then col = ramp[5]                     -- the sharpened edge
        elseif fromSpine < 0.9 then col = (py < A.y) and ramp[1] or ramp[2]
        elseif t < 0.55 then col = ramp[3]
        else col = ramp[4] end
        put(g, x, y, col)
      end
    end
  end
end

-- every blade cell with its place on the blade: s runs along the arc from the
-- heel (angle `heel` in degrees, measured round A), and the distances from the
-- spine and from the edge. For rules painted over a crescent - rot, lava, veins.
function eachBladeCell(g, A, B, heel, fn)
  for y = 0, g.h - 1 do
    for x = 0, g.w - 1 do
      if get(g, x, y) then
        local px, py = x + 0.5, y + 0.5
        local da = math.sqrt((px - A.x) ^ 2 + (py - A.y) ^ 2)
        local db = math.sqrt((px - B.x) ^ 2 + (py - B.y) ^ 2)
        if da <= A.r and db > B.r then
          local s = math.deg(math.atan(py - A.y, px - A.x)) - heel
          while s < 0 do s = s + 360 end
          fn(x, y, s, A.r - da, db - B.r)
        end
      end
    end
  end
end

-------------------------------------------------------------------------------
-- palettes
WOOD = { hex("f0c07a"), hex("d18a4a"), hex("9a5140"), hex("5e2f45") }
IRON = { hex("d9d4de"), hex("a8a2b3"), hex("7a7488"), hex("514b5e"), hex("f4efe6") }
RUST = { hex("c8673a"), hex("8a3a24") }
STONE = { hex("c9bdd1"), hex("93849f"), hex("635478"), hex("3f3357"), hex("f4efe6") }
DENSE = { hex("baacc4"), hex("847696"), hex("584a6e"), hex("3b3050") }
RIFT = { hex("ff9ad9"), hex("d94ecb"), hex("8a2f9c") }
GLASS = { hex("eaf4ff"), hex("b9c6ee"), hex("8a92d0"), hex("5d5c9e"), hex("ffffff") }
GLASSD = hex("3a3466")
RED = { hex("ffb27a"), hex("f2603f"), hex("c8283a"), hex("861a3e"), hex("4a0f2e") }
GOLD = { hex("fff1a8"), hex("f5c04a"), hex("c7802a"), hex("7a3e1e") }
STEEL = { hex("eef1f8"), hex("c8cee0"), hex("9aa2bc"), hex("666e8c"), hex("ffffff") }
LAC = { hex("8fa3c4"), hex("5a5c7a"), hex("3a3654"), hex("241f36") }
