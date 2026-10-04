-- Effect textures for the reaper class. Run from the repo root:
--
--   aseprite -b --script-param root=<repo> --script Art/reaper/effects.lua
--
-- ReapingSlash.png: a white crescent, tinted at draw time with the scythe's own
-- SlashColor. It is an effect, not an item, so it is soft-edged at full
-- resolution rather than on the 2x2 grid - like RiftFlare, it is light, and
-- light has no outline.
--
-- The crescent opens toward -x, its convex rim at +x: ReapingSlash draws it
-- offset from the point it cuts and turns it half a circle, rim leading.

local root = app.params.root
local pc = app.pixelColor

local W, H = 40, 64
local img = Image(W, H, ColorMode.RGB)
local cx, cy = W - 30.0, H / 2
local outerR, innerR, innerShift = 28.0, 25.0, 9.0

for y = 0, H - 1 do
  for x = 0, W - 1 do
    local px, py = x + 0.5, y + 0.5
    local dOut = math.sqrt((px - cx) ^ 2 + (py - cy) ^ 2)
    local dIn = math.sqrt((px - (cx - innerShift)) ^ 2 + (py - cy) ^ 2)
    if dOut <= outerR and dIn > innerR and px > cx - 4 then
      -- brightest along the leading (outer) rim, fading back into the hollow,
      -- and thinning to nothing at the two horns
      local rim = 1 - math.min(1, (outerR - dOut) / 7.0)
      local horn = 1 - math.abs(py - cy) / (outerR + 0.5)
      local a = math.max(0, math.min(1, (0.35 + 0.65 * rim) * math.min(1, horn * 2.2)))
      if a > 0.02 then
        local v = math.floor(200 + 55 * rim)
        img:drawPixel(x, y, pc.rgba(v, v, v, math.floor(a * 255)))
      end
    end
  end
end

local spr = Sprite(W, H, ColorMode.RGB)
spr.cels[1].image = img
spr:saveAs(root .. "/Content/Projectiles/Reaper/ReapingSlash.png")
-------------------------------------------------------------------------------
-- The unique scythes' throws are objects, not light, so they are drawn like
-- items: half-size char maps, outlined, blitted 2x2.
dofile(root .. "/Art/reaper/lib.lua")

-- FrostIcicle: drawn point-down; the projectile turns it to its velocity
do
  local g = newGrid(5, 12)
  stamp(g, {
    ".aab.",
    "aabbc",
    ".abc.",
    ".abc.",
    ".abc.",
    "..bc.",
    "..bc.",
    "..b..",
    "..c..",
  }, { a = hex("eafcff"), b = hex("b0e8ff"), c = hex("6ab8e8") }, 0, 0)
  outline(g)
  save(g, "Content/Projectiles/Reaper/FrostIcicle.png")
end

-- LidDisc: a round slab of the lid with the eye open in it
do
  local g = newGrid(9, 9)
  stamp(g, {
    "..aaab...",
    ".abbbbbc.",
    "abbwwwbbc",
    "abwwiwwbc",
    "abwipiwbd",
    "bbwwiwwcd",
    "bcbwwwccd",
    ".ccccccd.",
    "...ddd...",
  }, { a = hex("c9bdd1"), b = hex("93849f"), c = hex("635478"), d = hex("3f3357"),
       w = hex("e8cf94"), i = hex("d94ecb"), p = hex("241733") }, 0, 0)
  outline(g)
  save(g, "Content/Projectiles/Reaper/LidDisc.png")
end

print("effects: done")
