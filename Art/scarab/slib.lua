-- Helpers for the Scarab Sultan batch, on top of Art/reaper/lib.lua (the 2x2 grid,
-- the outline, stamp, haft). Loaded with
--   dofile(app.params.root .. "/Art/scarab/slib.lua")
--
-- The desert set is NOT the rift set: no violet anywhere. Gold carapace with hue-shifted
-- shadows (warm orange down to a cool plum), lapis and turquoise inlay as the Egyptian
-- accent, sandstone tans, and the sun's white-to-orange for anything that glows.

dofile(app.params.root .. "/Art/reaper/lib.lua")

OUTLINE = hex("2a1426")

GOLDR = { hex("fff6c8"), hex("ffd860"), hex("f0a838"), hex("c06a2a"), hex("7e3a2c"), hex("45203a") }
LAPIS = { hex("9ad8ff"), hex("4a8ef0"), hex("2a4fb0"), hex("1c2a6a") }
TURQ = { hex("b4fff0"), hex("3cc8b4"), hex("1a7a7e") }
SANDR = { hex("fff2d2"), hex("f2d49a"), hex("d9a865"), hex("a8733f"), hex("6a4232") }
SUNR = { hex("ffffff"), hex("fff4b0"), hex("ffc84a"), hex("ff8a24"), hex("d84a1e"), hex("8a2424") }
BRONZE = { hex("d89a58"), hex("a0603a"), hex("6a3430"), hex("3e1c2a") }
WINGR = { hex("fff2d8"), hex("f0d4a0"), hex("c89a68"), hex("8a5a44") }
STONE2 = { hex("f4e4c4"), hex("d6b88a"), hex("a88660"), hex("6e5246"), hex("463040") }

-- a filled dome lit from the top-left by its own normal - for shells, balls, plates.
-- inside(x, y) clips the ellipse further; ramp is 5 tones light -> dark.
function dome(g, cx, cy, rx, ry, ramp, inside)
  for y = math.floor(cy - ry) - 1, math.ceil(cy + ry) + 1 do
    for x = math.floor(cx - rx) - 1, math.ceil(cx + rx) + 1 do
      local nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
      local r2 = nx * nx + ny * ny
      if r2 <= 1 and (not inside or inside(x, y)) then
        local nz = math.sqrt(math.max(0, 1 - r2))
        local l = -0.55 * nx - 0.75 * ny + 0.45 * nz
        local col
        if l > 0.82 then col = ramp[1]
        elseif l > 0.42 then col = ramp[2]
        elseif l > 0.02 then col = ramp[3]
        elseif l > -0.38 then col = ramp[4]
        else col = ramp[5] end
        put(g, x, y, col)
      end
    end
  end
end

-- the normalised radius of a point in an ellipse: for bands that follow a dome's outline
function erad(x, y, cx, cy, rx, ry)
  local nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
  return math.sqrt(nx * nx + ny * ny), nx, ny
end

-- a limb: cells within w of segment a-b, lit across its width (upper-left side lit)
function limb(g, ax, ay, bx, by, w, lit, body, dark)
  local vx, vy = bx - ax, by - ay
  local len = math.sqrt(vx * vx + vy * vy)
  local nx, ny = -vy / len, vx / len
  if nx + ny > 0 then nx, ny = -nx, -ny end
  local x0, x1 = math.floor(math.min(ax, bx) - w - 1), math.ceil(math.max(ax, bx) + w + 1)
  local y0, y1 = math.floor(math.min(ay, by) - w - 1), math.ceil(math.max(ay, by) + w + 1)
  for y = y0, y1 do
    for x = x0, x1 do
      local px, py = x + 0.5 - ax, y + 0.5 - ay
      local t = math.max(0, math.min(1, (px * vx + py * vy) / (len * len)))
      local dx, dy = px - vx * t, py - vy * t
      if dx * dx + dy * dy <= w * w then
        local side = px * nx + py * ny
        local col = side > w * 0.3 and lit or (side < -w * 0.3 and dark or body)
        put(g, x, y, col)
      end
    end
  end
end

-- a convex/concave polygon fill (even-odd, by cell centres); fn(x, y) returns the colour
function poly(g, pts, fn)
  local y0, y1 = 1e9, -1e9
  for _, p in ipairs(pts) do y0, y1 = math.min(y0, p[2]), math.max(y1, p[2]) end
  for y = math.floor(y0), math.ceil(y1) do
    local cy = y + 0.5
    local xs = {}
    for i = 1, #pts do
      local a, b = pts[i], pts[i % #pts + 1]
      if (a[2] <= cy and b[2] > cy) or (b[2] <= cy and a[2] > cy) then
        xs[#xs + 1] = a[1] + (cy - a[2]) / (b[2] - a[2]) * (b[1] - a[1])
      end
    end
    table.sort(xs)
    for i = 1, #xs - 1, 2 do
      for x = math.ceil(xs[i] - 0.5), math.floor(xs[i + 1] - 0.5) do
        local c = fn(x, y)
        if c then put(g, x, y, c) end
      end
    end
  end
end

-- blit a grid 2x2 into an Image at a pixel offset, clipped to a box in art cells
function blit(img, g, ox, oy, w, h)
  for _, c in ipairs(cells(g)) do
    local x, y = c[1], c[2]
    if x >= 0 and x < w and y >= 0 and y < h then
      for sy = 0, 1 do
        for sx = 0, 1 do img:drawPixel(ox + x * 2 + sx, oy + y * 2 + sy, c[3]) end
      end
    end
  end
end

function saveImage(img, path)
  local spr = Sprite(img.width, img.height, ColorMode.RGB)
  spr.cels[1].image = img
  spr:saveAs(root .. "/" .. path)
end

-- a sheet of n frames of fw x fh art cells, stacked; frameFn(i) returns a grid
function sheet(path, n, fw, fh, frameFn)
  local img = Image(fw * 2, fh * 2 * n, ColorMode.RGB)
  for i = 1, n do blit(img, frameFn(i), 0, (i - 1) * fh * 2, fw, fh) end
  saveImage(img, path)
end

-- a single fixed-size image (no crop): for icons whose size the code depends on
function fixed(path, fw, fh, g)
  local img = Image(fw * 2, fh * 2, ColorMode.RGB)
  blit(img, g, 0, 0, fw, fh)
  saveImage(img, path)
end
