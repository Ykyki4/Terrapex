-- Shared helpers for the Rime set: palettes, lit ellipses and capsules, and the
-- blits that put several 2x2 frames into one sheet. Built on the reaper lib
-- (grid, outline, stamp, haft, crescent), which it loads first.
--
--   dofile(app.params.root .. "/Art/rime/rlib.lua")
--
-- The Rime has nothing to do with the rift, so none of the mod's violet ramps
-- appear here: packed snow whose shadows run to blue-violet, glacier ice from
-- near-white to deep blue, and three warm accents - amber eyes, a red cloth wrap,
-- and old horn - a few cells each.

dofile(app.params.root .. "/Art/reaper/lib.lua")

OUTLINE = hex("1c1a36")

-- snow: warm white at the lit end, blue-violet in the shadow
SNOW = { hex("fbf8f1"), hex("dfe5f1"), hex("b3bdd9"), hex("858db5"), hex("5b5e8c"), hex("3d3c68") }
-- glacier ice
ICE = { hex("eafcff"), hex("a3e9f8"), hex("58c3e6"), hex("2c87c0"), hex("1e5190"), hex("183a6c") }
-- the warm accents
AMBER = { hex("fff1a6"), hex("ffd25a"), hex("ff962c") }
RED = { hex("f2715a"), hex("c8393f"), hex("7e2240") }
HORN = { hex("e3d1a6"), hex("b39466"), hex("7a5e44"), hex("4c3a34") }
FUR = { hex("d9b48a"), hex("a77c58"), hex("6e4c3e"), hex("432e30") }
BIRCH = { hex("f1ece0"), hex("c7c0b4"), hex("8a8296"), hex("4f4a62") }

-- a palette table for char maps
CH = {
  W = SNOW[1], S = SNOW[2], s = SNOW[3], d = SNOW[4], D = SNOW[5], x = SNOW[6],
  I = ICE[1], i = ICE[2], c = ICE[3], C = ICE[4], B = ICE[5], b = ICE[6],
  Y = AMBER[1], E = AMBER[2], e = AMBER[3],
  R = RED[1], r = RED[2], q = RED[3],
  H = HORN[1], h = HORN[2], g = HORN[3], G = HORN[4],
  F = FUR[1], f = FUR[2], u = FUR[3], U = FUR[4],
  k = hex("2a2548"),
}

-- light from the top-left: how much a surface whose outward normal is (nx, ny)
-- faces it, -1..1
LX, LY = -0.62, -0.78

-- a filled ellipse, toned by a ramp from its own normal against the light.
-- ramp[1] is the lit end. `bands` are the cut points on the facing value.
function ellipse(g, cx, cy, rx, ry, ramp, bands)
  bands = bands or { 0.55, 0.15, -0.30, -0.70 }
  for y = math.floor(cy - ry), math.ceil(cy + ry) do
    for x = math.floor(cx - rx), math.ceil(cx + rx) do
      local nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
      local r2 = nx * nx + ny * ny
      if r2 <= 1 then
        local len = math.sqrt(r2)
        local f = 0
        if len > 0.001 then f = (nx * LX + ny * LY) / len * math.min(1, len * 1.6) end
        local i = #bands + 1
        for k, b in ipairs(bands) do if f >= b then i = k break end end
        put(g, x, y, ramp[math.min(i, #ramp)])
      end
    end
  end
end

-- a capsule from a to b, radius r, lit across its width like a haft
function capsule(g, ax, ay, bx, by, r, lit, body, dark)
  local vx, vy = bx - ax, by - ay
  local len2 = vx * vx + vy * vy
  local len = math.sqrt(len2)
  local nx, ny = -vy / len, vx / len
  if nx * LX + ny * LY < 0 then nx, ny = -nx, -ny end -- normal toward the light
  for y = math.floor(math.min(ay, by) - r - 1), math.ceil(math.max(ay, by) + r + 1) do
    for x = math.floor(math.min(ax, bx) - r - 1), math.ceil(math.max(ax, bx) + r + 1) do
      local px, py = x + 0.5 - ax, y + 0.5 - ay
      local t = math.max(0, math.min(1, (px * vx + py * vy) / len2))
      local qx, qy = px - vx * t, py - vy * t
      if qx * qx + qy * qy <= r * r then
        local side = qx * nx + qy * ny
        local col = side > r * 0.35 and lit or (side < -r * 0.3 and dark or body)
        put(g, x, y, col)
      end
    end
  end
end

-- a hash for clumped texture that stays put between frames
function hash(x, y)
  local n = (math.floor(x) * 374761393 + math.floor(y) * 668265263) % 2147483647
  n = (n ~ (n >> 13)) * 1274126177 % 2147483647
  return (n % 1000) / 1000
end

-- write one grid into an Image at frame (fx, fy) of size fw x fh art cells, 2x2
function blit(img, g, ox, oy, fw, fh)
  for _, c in ipairs(cells(g)) do
    local x, y = c[1], c[2]
    if x >= 0 and x < fw and y >= 0 and y < fh then
      for sy = 0, 1 do
        for sx = 0, 1 do img:drawPixel(ox + x * 2 + sx, oy + y * 2 + sy, c[3]) end
      end
    end
  end
end

function saveImage(img, path)
  local spr = Sprite(img.width, img.height, ColorMode.RGB)
  spr.cels[1].image = img
  spr:saveAs(app.params.root .. "/" .. path)
end

-- a vertical sheet of n frames, each fw x fh art cells, from a frame builder
function sheet(path, n, fw, fh, build)
  local img = Image(fw * 2, fh * 2 * n, ColorMode.RGB)
  for f = 1, n do blit(img, build(f), 0, (f - 1) * fh * 2, fw, fh) end
  saveImage(img, path)
end
