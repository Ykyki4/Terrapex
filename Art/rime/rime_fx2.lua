-- The Rime's effect textures - the pieces Common/Particles/Rime.cs and the boss's
-- telegraphs are built from. Run:
--
--   aseprite -b --script-param root=<repo> --script Art/rime/rime_fx2.lua
--
-- All of them are requested by path at draw time; none has a .cs of its own.
--
--   RimeGlow     64x64 soft white disc, alpha quantised in hard steps on the 2x2 grid.
--                Pure white so a tint is the colour - the rift flare's pink halo turned
--                every cold bloom lavender.
--   RimeRing     64x64 white ring, radius ~28 px at scale 1.
--   RimeCrystal  a small prism of rime STANDING on its bottom edge, point up.
--   RimeFlake    a snowflake, no outline (it is drawn as light).
--   RimeStar     a four-point glint, no outline.

dofile(app.params.root .. "/Art/rime/rlib.lua")

local function rgba(r, g, b, a) return pc.rgba(r, g, b, a) end

-- a 2x2 cell with its own alpha
local function cell(img, x, y, col)
  for sy = 0, 1 do
    for sx = 0, 1 do img:drawPixel(x * 2 + sx, y * 2 + sy, col) end
  end
end

-- RimeGlow: five hard alpha steps out from the middle
do
  local img = Image(64, 64, ColorMode.RGB)
  local steps = { 250, 190, 130, 80, 40, 16 }
  for y = 0, 31 do
    for x = 0, 31 do
      local dx, dy = x + 0.5 - 16, y + 0.5 - 16
      local d = math.sqrt(dx * dx + dy * dy) / 16
      if d < 1 then
        local i = math.min(#steps, math.floor(d * #steps) + 1)
        cell(img, x, y, rgba(255, 255, 255, steps[i]))
      end
    end
  end
  saveImage(img, "Content/Projectiles/Rime/RimeGlow.png")
end

-- RimeRing: a band at radius 14 art cells, bright core and two softer edges
do
  local img = Image(64, 64, ColorMode.RGB)
  for y = 0, 31 do
    for x = 0, 31 do
      local dx, dy = x + 0.5 - 16, y + 0.5 - 16
      local d = math.abs(math.sqrt(dx * dx + dy * dy) - 14)
      local a = d < 0.7 and 230 or (d < 1.5 and 120 or (d < 2.3 and 40 or 0))
      if a > 0 then cell(img, x, y, rgba(255, 255, 255, a)) end
    end
  end
  saveImage(img, "Content/Projectiles/Rime/RimeRing.png")
end

-- RimeCrystal: a hexagonal prism of rime, lit face left, point up
do
  local g = newGrid(1, 1)
  stamp(g, {
    "..I..",
    ".IIc.",
    ".Iic.",
    "IiicC",
    "IiccC",
    "IicCB",
    "IicCB",
    "iccCB",
    "SsddD",
  }, CH, 1, 1)
  outline(g)
  save(g, "Content/Projectiles/Rime/RimeCrystal.png")
end

-- RimeFlake: eight arms with forks, a bright hub
do
  local g = newGrid(1, 1)
  stamp(g, {
    "I..i..I",
    ".I.i.I.",
    "..iIi..",
    "iiIWIii",
    "..iIi..",
    ".I.i.I.",
    "I..i..I",
  }, CH, 0, 0)
  save(g, "Content/Projectiles/Rime/RimeFlake.png")
end

-- RimeStar: a four-point glint, long arms vertical and horizontal
do
  local g = newGrid(1, 1)
  stamp(g, {
    "....I....",
    "....I....",
    "....i....",
    "...iWi...",
    "IIiWWWiII",
    "...iWi...",
    "....i....",
    "....I....",
    "....I....",
  }, CH, 0, 0)
  save(g, "Content/Projectiles/Rime/RimeStar.png")
end
