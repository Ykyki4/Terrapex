-- Reaper class item sprites that keep the shared crescent: the Rusted Sickle,
-- the Kusarigama, the Rift Scythe, and the class's accessories. The scythes with
-- shapes of their own are in scythes.lua. Run from the repo root:
--
--   aseprite -b --script-param root=<repo> --script Art/reaper/reaper_items.lua
--
-- Calamity grid: everything is authored in an art buffer at half size, outlined
-- there, and blitted as 2x2 blocks. Swung weapons are drawn held facing right:
-- haft from the bottom-left, blade off the top, sweeping right and dropping.
--
-- A blade is a crescent - inside one circle and outside another - so the curve is
-- rasterised as a region, never stepped along (a stepped curve breaks into a
-- dotted zigzag at this size). Light comes from the top-left: the spine catches
-- it at the top of the arc, the sharpened inner edge is the brightest line.

dofile(app.params.root .. "/Art/reaper/lib.lua")

-------------------------------------------------------------------------------
-- Rusted Sickle, 15x15 art -> 30x30. A real sickle, not a short scythe: the
-- blade leaves the handle at its heel, climbs, rolls over the top and comes back
-- down with the point turned in toward the hand. The first pass stopped at the
-- top-right quarter and read as a pickaxe.
--
-- Rust is patches, not pixels. Single orange cells scattered on the blade read as
-- dirt; rust reads when it is a solid region in its own three-tone ramp that eats
-- the spine from the heel and thins out toward the point, and never touches the
-- sharpened edge.
do
  local RUSTR = { hex("e08a4a"), hex("b0532c"), hex("6e3022") }
  local g = newGrid(15, 15)
  local A, B = { x = 8.0, y = 7.0, r = 6.4 }, { x = 7.2, y = 9.0, r = 5.0 }
  crescent(g, A, B, function(x, y) return not (x < 5.2 and y > 5.0) end, IRON)

  -- position along the blade: 0 at the heel (upper left), ~240 at the point
  for y = 0, 14 do
    for x = 0, 14 do
      local col = get(g, x, y)
      if col then
        local px, py = x + 0.5, y + 0.5
        local da = math.sqrt((px - A.x) ^ 2 + (py - A.y) ^ 2)
        local db = math.sqrt((px - B.x) ^ 2 + (py - B.y) ^ 2)
        local fromSpine, fromEdge = A.r - da, db - B.r
        local s = math.deg(math.atan(py - A.y, px - A.x)) + 150
        if s < 0 then s = s + 360 end
        local depth = 0
        if s < 75 then depth = 2.3            -- the heel: rust nearly to the edge
        elseif s > 100 and s < 128 then depth = 1.4
        elseif s > 158 and s < 170 then depth = 1.0 end
        if fromEdge >= 0.9 and fromSpine < depth then
          local r
          if fromSpine < 0.9 then r = (py < A.y) and RUSTR[1] or RUSTR[2]
          elseif fromSpine < depth - 0.6 then r = RUSTR[2]
          else r = RUSTR[3] end
          put(g, x, y, r)
        end
      end
    end
  end

  -- old, darkened wood: one step down the ramp, so it does not run into the rust
  haft(g, 1.4, 14.0, 4.9, 7.6, 1.05, WOOD[2], WOOD[3], WOOD[4], { 0.5 })
  -- the iron collar where the tang goes into the handle, rusted through
  stamp(g, { "kr", "rk" }, { k = IRON[4], r = RUSTR[3] }, 4, 6)
  outline(g)
  save(g, "Content/Items/Weapons/Reaper/RustedSickle.png")
end

-- Kusarigama, 22x22 art -> 44x44: a kama on a red-wrapped handle, its chain
-- looping down to an iron weight in the bottom-left
do
  local g = newGrid(22, 22)
  -- chain first, so the handle and blade sit over it
  local links = { { 2, 19 }, { 3, 17 }, { 2, 15 }, { 3, 13 }, { 5, 12 }, { 7, 12 }, { 9, 13 } }
  for i, l in ipairs(links) do
    put(g, l[1], l[2], (i % 2 == 0) and LAC[1] or LAC[2])
  end
  stamp(g, { ".a.", "abc", ".c." }, { a = LAC[1], b = LAC[2], c = LAC[3] }, 0, 19)
  -- handle: lacquered red wrap in diamond bands, gold end caps
  haft(g, 9.6, 14.2, 15.0, 5.6, 1.15, RED[2], RED[3], RED[4], { 0.25, 0.5, 0.75 })
  stamp(g, { "ab", "bc" }, { a = GOLD[1], b = GOLD[2], c = GOLD[3] }, 9, 13)
  -- the kama blade: a steel crescent off the top of the handle, sweeping right
  crescent(g, { x = 16.6, y = 9.0, r = 7.0 }, { x = 14.4, y = 12.2, r = 6.2 },
    function(x, y) return x > 13.0 and y < 13.6 end, STEEL)
  stamp(g, { "ab" }, { a = GOLD[2], b = GOLD[3] }, 14, 5)
  outline(g)
  save(g, "Content/Items/Weapons/Reaper/Kusarigama.png")
end

-- Whetstone, 13x9 art -> 26x18: a grey bar, worn into a shallow dip on top
do
  local g = newGrid(13, 9)
  stamp(g, {
    "..aabbbbaa...",
    ".abccccccbba.",
    "abcccccccccbd",
    "bccccccdcccde",
    "bcccdcccccdde",
    ".deeeeeeeeee.",
  }, { a = DENSE[1], b = STONE[3], c = STONE[4], d = DENSE[3], e = DENSE[4] }, 0, 1)
  stamp(g, { "ww" }, { w = STONE[1] }, 4, 2)
  outline(g)
  save(g, "Content/Items/Accessories/Whetstone.png")
end

-- Reaper Emblem, 14x14 art -> 28x28: the vanilla emblem's round plate, carrying
-- a gold sickle crossed with nothing - the class is one blade
do
  local g = newGrid(14, 14)
  for y = 0, 13 do
    for x = 0, 13 do
      local dx, dy = x + 0.5 - 7, y + 0.5 - 7
      local d = math.sqrt(dx * dx + dy * dy)
      if d <= 6.4 then
        local col
        if d > 5.3 then col = (dx + dy < 0) and GOLD[2] or GOLD[3]
        else col = (dx + dy < -2) and LAC[2] or (dx + dy > 3 and LAC[4] or LAC[3]) end
        put(g, x, y, col)
      end
    end
  end
  -- the sickle is hand-placed: at 8 cells a rasterised crescent is a blob
  stamp(g, {
    "..aab...",
    "....ab..",
    ".....ac.",
    ".....ac.",
    "....ac..",
    "..d.....",
    ".d......",
    "e.......",
  }, { a = GOLD[1], b = GOLD[2], c = GOLD[3], d = GOLD[2], e = GOLD[3] }, 3, 3)
  outline(g)
  save(g, "Content/Items/Accessories/ReaperEmblem.png")
end

-- Rift Scythe, 26x26 art -> 52x52 (T4, the Colossus's drop). Redrawn on the 2x2
-- grid so it sits with the rest of the class. CLAUDE.md's scythe rule holds: a
-- long dark haft, a broad blade off the neck that sweeps right and drops below
-- it, the concave edge facing the wielder, a curved back, and a rib drawn as a
-- line along the curve. T4's colour rule holds too: the haft is void body, and
-- the turquoise is reserved for the blade and the thread bound round the haft.
do
  local THREAD = { hex("d8fff7"), hex("7ff2e0"), hex("35c9b8"), hex("1c7d78"), hex("ffffff") }
  local VOID = { hex("9fb4bb"), hex("61818c"), hex("3d5560"), hex("22343e"), hex("0f1c26") }
  local g = newGrid(26, 26)
  haft(g, 1.4, 24.8, 11.0, 4.2, 1.15, VOID[1], VOID[2], VOID[4], { 0.18, 0.42, 0.66 })
  local A, B = { x = 16.8, y = 11.6, r = 10.4 }, { x = 13.6, y = 16.8, r = 9.5 }
  crescent(g, A, B, function(x, y) return x > 10.4 and y < 18.6 end, THREAD)
  -- the rib: one cell-wide line following the curve, only where the blade is
  -- thick enough to carry it (scattered rib pixels read as noise)
  for y = 0, 25 do
    for x = 0, 25 do
      local px, py = x + 0.5, y + 0.5
      local da = math.sqrt((px - A.x) ^ 2 + (py - A.y) ^ 2)
      local db = math.sqrt((px - B.x) ^ 2 + (py - B.y) ^ 2)
      if get(g, x, y) and math.abs(da - (A.r - 2.4)) < 0.5 and db - B.r > 1.6 and px > 11.5 then
        put(g, x, y, THREAD[4])
      end
    end
  end
  -- thread wraps on the haft: the only turquoise below the neck
  stamp(g, { "ab", "ba" }, { a = THREAD[2], b = THREAD[3] }, 5, 16)
  stamp(g, { "ab", "ba" }, { a = THREAD[2], b = THREAD[3] }, 8, 9)
  -- the collar binding blade to haft
  stamp(g, { ".ab", "abc", "bc." }, { a = VOID[1], b = VOID[3], c = VOID[5] }, 9, 3)
  outline(g)
  save(g, "Content/Items/Weapons/RiftScythe.png")
end

print("reaper items: done")
