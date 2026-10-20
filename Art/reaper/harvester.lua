-- The Hollow Harvester and everything it drops. Run from the repo root:
--
--   aseprite -b --script-param root=<repo> --script Art/reaper/harvester.lua
--
-- 2x2, like the rest of the reaper class. The body is four frames of the cloak
-- fluttering; the scythe is a texture of its own because the code draws it in the
-- hand at an angle and throws it in phase two.
--
-- The sack head, the arm and the straw are hand-drawn maps. The cloak is spans -
-- one left and right edge per row - lit by an authored rule (a lit left edge, a
-- body, a dark right side, two folds running down it), with a tattered hem whose
-- shape is authored per frame. That is the same split as the scythes: rules for the
-- long plain parts, hands for the parts that say what the thing is.

dofile(app.params.root .. "/Art/reaper/lib.lua")

local STRAW = { Y = hex("fff0a0"), S = hex("e8c860"), s = hex("c09a40"), z = hex("7a5a28") }
local SACK = { a = hex("d8aa78"), b = hex("a8784c"), c = hex("74503a"), t = hex("3a2418") }
local CLOAK = { h = hex("6a5a74"), k = hex("4a3a52"), m = hex("34283e"), n = hex("241c2e") }
local EYE = { E = hex("9affb0"), W = hex("e8fff0") }
local ROPE = hex("c09a40")

local HEAD = {
  "................YSY.................",
  "...............YSsSY................",
  "................rrr.................",
  "..............abbbbb................",
  ".............abbbbbbc...............",
  "............abbbbbbbbc..............",
  "............abbbbbbbbbc.............",
  "...........abbbEEbbEEbc.............",
  "...........abbbEWbbEWbc.............",
  "...........abbbbbbbbbbc.............",
  "...........abbtbtbtbtbc.............",
  "............abbtbtbtbc..............",
  "............cabbbbbbcc..............",
  "...........SYrrrrrrrrYS.............",
}
local HEAD_PAL = { Y = STRAW.Y, S = STRAW.S, s = STRAW.s, r = ROPE, a = SACK.a, b = SACK.b, c = SACK.c,
                   t = SACK.t, E = EYE.E, W = EYE.W }

-- the right arm, reaching out to where the code puts the hand (art 27,28)
local ARM = {
  "kkkk......",
  "kmmmk.....",
  ".kmmmk....",
  "..kmmmk...",
  "...kmmmk..",
  "....kmmmk.",
  ".....kmmk.",
  ".....kmmmk",
  "......kmmk",
  "......kmmk",
  ".....YSsSY",
  "......YSs.",
}

-- the cloak: rows 14-39 as spans [left, right], the hem below authored per frame
local SPANS = {}
for r = 14, 39 do
  local t = (r - 14) / 25
  SPANS[r] = { math.floor(8 - 3 * t + 0.5), math.floor(24 + 5 * t + 0.5) }
end
local HEMS = {
  { 2, 0, 3, 1, 4, 0, 2, 5, 1, 3, 0, 2, 4, 1, 0, 3, 2, 5, 0, 1, 3, 2, 0, 4, 1 },
  { 3, 1, 4, 0, 2, 1, 5, 2, 0, 4, 1, 3, 0, 2, 5, 1, 0, 3, 4, 0, 2, 1, 5, 0, 2 },
  { 1, 4, 0, 3, 1, 5, 0, 2, 4, 1, 3, 0, 2, 5, 1, 0, 4, 2, 0, 3, 1, 4, 0, 2, 5 },
  { 4, 2, 0, 5, 1, 3, 2, 0, 3, 5, 0, 1, 4, 0, 2, 3, 1, 0, 5, 2, 0, 3, 1, 4, 0 },
}

local function cloakCell(r, x, left, right)
  if x == left then return CLOAK.h end
  if x >= right - 1 then return CLOAK.n end
  if (x == 12 or x == 19) and r >= 19 then return CLOAK.n end  -- the folds
  if x <= left + 2 then return CLOAK.k end
  return CLOAK.m
end

local function frame(f, bob)
  local g = newGrid(36, 48)
  for r = 14, 39 do
    local sp = SPANS[r]
    for x = sp[1], sp[2] do put(g, x, r + bob, cloakCell(r, x, sp[1], sp[2])) end
  end
  -- the hem: each column of the last span hangs a different length
  local last = SPANS[39]
  for x = last[1], last[2] do
    local hang = HEMS[f][(x - last[1]) % #HEMS[f] + 1]
    for r = 40, 40 + hang do
      if r + bob < 48 then put(g, x, r + bob, cloakCell(r, x, last[1], last[2])) end
    end
  end
  -- straw poking out under the cloak's shoulders
  stamp(g, { "Y.S", ".s." }, STRAW, 6, 15 + bob)
  stamp(g, HEAD, HEAD_PAL, 0, bob)
  stamp(g, ARM, { k = CLOAK.k, m = CLOAK.m, Y = STRAW.Y, S = STRAW.S, s = STRAW.s }, 21, 17 + bob)
  outline(g)
  return g
end

-- the sheet: four frames stacked, each 72x96, every frame on the same canvas so
-- they line up - save() would crop each to its own bounds, so blit by hand here
do
  local sheet = Image(72, 96 * 4, ColorMode.RGB)
  for f = 1, 4 do
    local g = frame(f, (f == 2 or f == 3) and -1 or 0)
    for _, c in ipairs(cells(g)) do
      local x, y = c[1], c[2]
      if x >= 0 and x < 36 and y >= 0 and y < 48 then
        for sy = 0, 1 do
          for sx = 0, 1 do sheet:drawPixel(x * 2 + sx, (f - 1) * 96 + y * 2 + sy, c[3]) end
        end
      end
    end
  end
  local spr = Sprite(72, 96 * 4, ColorMode.RGB)
  spr.cels[1].image = sheet
  spr:saveAs(app.params.root .. "/Content/NPCs/Bosses/HollowHarvester.png")
end

-- the map icon: the sack head, 30x30
do
  local g = newGrid(15, 15)
  stamp(g, HEAD, HEAD_PAL, -8, 0)
  outline(g)
  local img = Image(30, 30, ColorMode.RGB)
  for _, c in ipairs(cells(g)) do
    local x, y = c[1], c[2]
    if x >= 0 and x < 15 and y >= 0 and y < 15 then
      for sy = 0, 1 do
        for sx = 0, 1 do img:drawPixel(x * 2 + sx, y * 2 + sy, c[3]) end
      end
    end
  end
  local spr = Sprite(30, 30, ColorMode.RGB)
  spr.cels[1].image = img
  spr:saveAs(app.params.root .. "/Content/NPCs/Bosses/HollowHarvester_Head_Boss.png")
end

-- the Harvester's scythe: a farm scythe grown huge - a long weathered snath with a
-- grip peg, and a broad pale blade hanging off the top with a notched, worn edge
do
  local g = newGrid(34, 34)
  haft(g, 1.4, 32.8, 14.0, 5.0, 1.15, hex("a88a6a"), hex("6a5440"), hex("3a2c22"), { 0.3, 0.55, 0.8 })
  stamp(g, { "ab", "bc" }, { a = hex("a88a6a"), b = hex("6a5440"), c = hex("3a2c22") }, 4, 21) -- the grip peg
  local A, B = { x = 21.4, y = 13.6, r = 13.2 }, { x = 17.6, y = 19.6, r = 12.2 }
  crescent(g, A, B, function(x, y) return x > 13.2 and y < 24.0 end,
    { hex("e8fff0"), hex("c4ecd6"), hex("8ab89a"), hex("4a6a5a"), hex("ffffff") })
  eachBladeCell(g, A, B, -160, function(x, y, s, fromSpine, fromEdge)
    if fromEdge < 0.9 and math.floor(s / 18) % 3 == 1 then clear(g, x, y) end  -- notches
  end)
  stamp(g, { "rr", "rr" }, { r = ROPE }, 13, 4)
  outline(g)
  save(g, "Content/Projectiles/Harvester/HarvesterScythe.png")
end

-- the crow: two frames, wings up and wings down, stacked; it faces right, which
-- is what the projectiles' spriteDirection flip assumes
do
  local W, H = 11, 9
  local frames = {
    {
      "........k..",
      ".kk....kkk.",
      ".kkk..kkk..",
      "..kkkmkk...",
      "..kmmmmky..",
      ".kmmmmmkYy.",
      ".kkmmmmk...",
      "...k.kk....",
      "...........",
    },
    {
      "...........",
      "...........",
      "...........",
      "....kmkk...",
      "..kmmmmky..",
      "kkmmmmmkYy.",
      "kkkmmmmkk..",
      "..kk.kk.kk.",
      "..k.......k",
    },
  }
  local img = Image(W * 2 + 4, (H * 2 + 4) * 2, ColorMode.RGB)
  for f, rows in ipairs(frames) do
    local g = newGrid(W, H)
    -- a STRAW crow, not a black one: it is a night boss, and a black bird on a night sky was
    -- invisible. Weathered straw body, dark-straw flight feathers, the ghost-light eye
    stamp(g, rows, { k = hex("6a4e36"), m = hex("d8b878"), y = hex("ffd08a"), Y = hex("b8ffc8") }, 0, 0)
    outline(g)
    for _, c in ipairs(cells(g)) do
      for sy = 0, 1 do
        for sx = 0, 1 do img:drawPixel((c[1] + 1) * 2 + sx, (f - 1) * (H * 2 + 4) + (c[2] + 1) * 2 + sy, c[3]) end
      end
    end
  end
  local spr = Sprite(img.width, img.height, ColorMode.RGB)
  spr.cels[1].image = img
  spr:saveAs(app.params.root .. "/Content/Projectiles/Harvester/HarvesterCrow.png")
end

-- the jack-o'-lantern it lobs in phase two: a carved pumpkin, face lit from inside, so
-- it reads at night before the code adds any glow at all
do
  local g = newGrid(11, 11)
  stamp(g, {
    ".....s.....",
    "....sS.....",
    "..ooOOOoo..",
    ".oOOoOoOOo.",
    "oOOoOOOoOOo",
    "oOffoOoffOo",
    "oOOoOfOoOOo",
    "oOfOfOfOfOo",
    "oOfffffffOo",
    ".oOoOOOoOo.",
    "..ooooooo..",
  }, { o = hex("b8541e"), O = hex("f08a2a"), s = hex("5e4a2a"), S = hex("8aa04a"), f = hex("ffe27a") }, 0, 0)
  outline(g)
  save(g, "Content/Projectiles/Harvester/HarvesterPumpkin.png")
end

-- the summon: a little scarecrow on a stake
do
  local g = newGrid(1, 1)
  stamp(g, {
    "...YSY...",
    "..abbbc..",
    "..bEbEc..",
    "..btbtc..",
    "...rrr...",
    "kkkkmkkkk",
    "S.kmmmk.S",
    "..kmmmk..",
    "..kmmmk..",
    "...kmk...",
    "....w....",
    "....w....",
    "....w....",
    "...www...",
  }, { Y = STRAW.Y, S = STRAW.S, a = SACK.a, b = SACK.b, c = SACK.c, t = SACK.t, E = EYE.E, r = ROPE,
       k = CLOAK.k, m = CLOAK.m, w = hex("9a5140") }, 0, 0)
  outline(g)
  save(g, "Content/Items/Consumables/ScarecrowEffigy.png")
end

-- the treasure bag: a burlap sack, tied, with straw and a green glint at the mouth
do
  local g = newGrid(1, 1)
  stamp(g, {
    "......Y.S.....",
    ".....YSsSY....",
    "......rrr.....",
    ".....abbbc....",
    "...aabbbbbcc..",
    "..abbbbbbbbbc.",
    ".abbbtbbbbbbbc",
    ".abbbtbbbEbbbc",
    ".abbbbbbbbbbcc",
    ".abbbbtbbbbbcc",
    "..bbbbtbbbbcc.",
    "...cccccccc...",
  }, { Y = STRAW.Y, S = STRAW.S, s = STRAW.s, r = ROPE, a = SACK.a, b = SACK.b, c = SACK.c, t = SACK.t,
       E = EYE.E }, 1, 2)
  outline(g)
  save(g, "Content/Items/Consumables/HollowHarvesterBag.png")
end

-- the mask: the sack head as vanity, stamped into the twenty head frames on the
-- player's head box (cols 5-15 / rows 5-14), eyes over the player's eye line
do
  local MASK = {
    "....YSY....",
    "....rrr....",
    "..abbbbbc..",
    ".abbbbbbbc.",
    "abbbbbbbbbc",
    "abbEEbbEEbc",
    "abbEWbbEWbc",
    "abbbbbbbbbc",
    "abtbtbtbtbc",
    ".cabbbbbcc.",
    "..SrrrrrS..",
  }
  local g = newGrid(20, 28)
  stamp(g, MASK, HEAD_PAL, 5, 4)
  outline(g)
  local sheet = Image(40, 1120, ColorMode.RGB)
  local high = { [7] = true, [8] = true, [9] = true, [14] = true, [15] = true, [16] = true }
  for f = 0, 19 do
    local top = f * 56 - (high[f] and 2 or 0)
    for _, c in ipairs(cells(g)) do
      for sy = 0, 1 do
        for sx = 0, 1 do
          local py = top + c[2] * 2 + sy
          if py >= f * 56 and py < f * 56 + 56 then sheet:drawPixel(c[1] * 2 + sx, py, c[3]) end
        end
      end
    end
  end
  local spr = Sprite(40, 1120, ColorMode.RGB)
  spr.cels[1].image = sheet
  spr:saveAs(app.params.root .. "/Content/Items/Armor/HollowHarvesterMask_Head.png")
  save(g, "Content/Items/Armor/HollowHarvesterMask.png")
end

-- Hollow Harvest: the Harvester's scythe cut down to a reaper's size, with a crow
-- perched on the neck of the blade
do
  local g = newGrid(24, 24)
  haft(g, 1.3, 22.8, 9.4, 4.6, 1.1, hex("a88a6a"), hex("6a5440"), hex("3a2c22"), { 0.35, 0.7 })
  local A, B = { x = 15.0, y = 10.4, r = 9.2 }, { x = 12.2, y = 14.6, r = 8.4 }
  crescent(g, A, B, function(x, y) return x > 9.4 and y < 17.5 end,
    { hex("e8fff0"), hex("c4ecd6"), hex("8ab89a"), hex("4a6a5a"), hex("ffffff") })
  stamp(g, { "..kk.", ".kkmk", "ykmmk", ".kkk." }, { k = hex("2a2230"), m = hex("4a3e52"), y = hex("e8c860") }, 6, 0)
  outline(g)
  save(g, "Content/Items/Weapons/Reaper/HollowHarvest.png")
end

-- Crow Charm: a crow's feather bound with straw and a green bead
do
  local g = newGrid(1, 1)
  stamp(g, {
    "......kk",
    ".....kmk",
    "....kmk.",
    "...kmk..",
    "..kmk...",
    ".YrE....",
    "SrY.....",
    "Ss......",
  }, { k = hex("2a2230"), m = hex("4a3e52"), Y = STRAW.Y, S = STRAW.S, s = STRAW.s, r = ROPE, E = EYE.E }, 0, 0)
  outline(g)
  save(g, "Content/Items/Accessories/CrowCharm.png")
end

print("harvester: done")
