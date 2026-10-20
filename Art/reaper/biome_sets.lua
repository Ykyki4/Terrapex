-- Tidewrack and Sunwrought: the reaper's two armour sets between the Scarecrow
-- (pre-boss) and the Ronin (post-Skeletron), plus the three biome-boss reaper
-- accessories. Shipping art.
--
--   aseprite -b --script-param root=<repo> --script Art/reaper/biome_sets.lua
--
-- Built the way scarecrow.lua and Art/concepts/ronin/ronin.lua are: half-size
-- char maps for the head, the torso and the shoulder, ExampleMod's arm and leg
-- masks taken cell for cell and painted by rule, outlined in art space, all
-- blitted 2x2.
--
-- The two sets must not read as the Scarecrow or the Ronin recoloured, nor as
-- each other, so each has its own silhouette idea:
--   Tidewrack  - drowned sailor: a sodden sailcloth HOOD (the only hood of the
--                four) crusted with barnacles, kelp trailing off the back; a
--                double-breasted sea-green coat with brass buttons and an anchor
--                chain worn as a bandolier; tarred trousers in sea boots.
--   Sunwrought - scarab priest: a striped gold-and-lapis NEMES headdress whose
--                lappets fall behind the ear, a sun disk on the brow; a lapis
--                broad collar over split gilded elytra, a sun buckle, a white
--                linen kilt; gold greaves with a lapis knee band.
-- Palettes come from the bosses' own art libs: Art/shellback/sea.lua and
-- Art/scarab/slib.lua (and Art/rime/rlib.lua for the Rime accessory).

local root = app.params.root
local exRoot = root .. "/Art/reference/examplemod/"
local armorOut = root .. "/Content/Items/Armor/Reaper/"
local accOut = root .. "/Content/Items/Accessories/Reaper/"
local pc = app.pixelColor

local function rgba(hex)
  return pc.rgba(tonumber(hex:sub(1, 2), 16), tonumber(hex:sub(3, 4), 16), tonumber(hex:sub(5, 6), 16), 255)
end
local function palette(t)
  local C = {}
  for k, v in pairs(t) do C[k] = rgba(v) end
  return C
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

local function blit(img, g, C, px, py, clip)
  for y, row in pairs(g) do
    for x, ch in pairs(row) do
      local col = C[ch]
      if not col then error("no colour for '" .. ch .. "'") end
      for sy = 0, 1 do
        for sx = 0, 1 do
          local X, Y = px + x * 2 + sx, py + y * 2 + sy
          if not clip or (Y >= clip[1] and Y < clip[2]) then img:drawPixel(X, Y, col) end
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

local function save(img, path)
  local spr = Sprite(img.width, img.height, ColorMode.RGB)
  spr.cels[1].image = img
  spr:saveAs(path)
end

local function icon(g, C, path)
  local x0, y0, x1, y1 = bounds(g)
  local img = Image((x1 - x0 + 1) * 2, (y1 - y0 + 1) * 2, ColorMode.RGB)
  blit(img, g, C, -x0 * 2, -y0 * 2)
  save(img, path)
end

local function iconFromMap(rows, C, path)
  local g = {}
  stampMap(g, rows, 0, 0)
  outline(g)
  icon(g, C, path)
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

-- an arm: a sleeve from the shoulder, a cuff or bracer at the hand. A is the
-- set's arm palette: {cuffCorner, cuffLit, cuffBody, cuffShade, sleeveLit,
-- sleeveBody, sleeveSeam, sleeveShade}
local function paintArm(g, pivotX, pivotY, A)
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
    local corner = not isFill(g, x, y - 1) and not isFill(g, x - 1, y)
    local ch
    if d >= maxd - 1.2 and #cells > 3 then
      ch = corner and A[1] or (lit and A[2] or (shade and A[4] or A[3]))
    else
      ch = lit and A[5] or (shade and A[8] or (x % 3 == 0 and A[7] or A[6]))
    end
    put(g, x, y, ch)
  end
end

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

-- legs: each leg is a 4-connected run of fill between ExampleMod's own outline,
-- shaded along each horizontal run (lit edge, body, dark edge) - never by row.
-- rule(rel, bottom, first, last) gives the char for one cell; the far leg goes
-- one step darker through the set's DARKER table.
local function paintLegs(g, rule, darker)
  local parts = legParts(g)
  for i, part in ipairs(parts) do
    local top, bottom = 1e9, -1e9
    for _, c in ipairs(part) do top, bottom = math.min(top, c[2]), math.max(bottom, c[2]) end
    for _, c in ipairs(part) do
      local x, y = c[1], c[2]
      local first = not isFill(g, x - 1, y)
      local last = not isFill(g, x + 1, y)
      local ch = rule(y - top, y == bottom, first, last)
      if i > 1 then ch = darker[ch] or ch end
      put(g, x, y, ch)
    end
  end
end

local HIGH = { [7] = true, [8] = true, [9] = true, [14] = true, [15] = true, [16] = true }

local function buildSet(S)
  local C = S.C
  -- head
  local gh = {}
  stampMap(gh, S.head, 0, 0)
  outline(gh)
  local img = Image(40, 1120, ColorMode.RGB)
  for f = 0, 19 do
    local top = f * 56
    blit(img, gh, C, 0, top - (HIGH[f] and 2 or 0), { top, top + 56 })
  end
  save(img, armorOut .. S.headName .. "_Head.png")
  icon(gh, C, armorOut .. S.headName .. ".png")

  -- body: torso on (0,0) (1,0) (0,2) (1,2), the shoulder piece on (0,1) (0,3),
  -- every other cell an arm from ExampleMod's mask
  local src = loadRGB(exRoot .. "ExampleBreastplate_Body.png")
  local bimg = Image(360, 224, ColorMode.RGB)
  for cy = 0, 3 do
    for cx = 0, 8 do
      local g
      if cx <= 1 and (cy == 0 or cy == 2) then
        g = {}; stampMap(g, S.torso, S.torsoX, S.torsoY); outline(g)
      elseif cx == 0 and (cy == 1 or cy == 3) then
        g = {}; stampMap(g, S.sode, S.sodeX, S.sodeY); outline(g)
      elseif not (cx == 1 and (cy == 1 or cy == 3)) then
        g = readMask(src, cx * 40, cy * 56)
        if cx == 8 then paintArm(g, 12.5, 12, S.arm) else paintArm(g, 6.5, 13.5, S.arm) end
      end
      if g then blit(bimg, g, C, cx * 40, cy * 56, { cy * 56, cy * 56 + 56 }) end
    end
  end
  save(bimg, armorOut .. S.bodyName .. "_Body.png")
  iconFromMap(S.bodyIcon, C, armorOut .. S.bodyName .. ".png")

  -- legs
  local lsrc = loadRGB(exRoot .. "ExampleLeggings_Legs.png")
  local limg = Image(40, 1120, ColorMode.RGB)
  for f = 0, 19 do
    local g = readMask(lsrc, 0, f * 56)
    paintLegs(g, S.legRule, S.darker)
    blit(limg, g, C, 0, f * 56, { f * 56, f * 56 + 56 })
  end
  save(limg, armorOut .. S.legsName .. "_Legs.png")
  iconFromMap(S.legsIcon, C, armorOut .. S.legsName .. ".png")
end

------------------------------------------------------------------ TIDEWRACK
-- sea.lua: OUTLINE 161a2a; BARN, SEA, KELP, IRONS, RUSTS, WOODP, GOLDP.
-- The sailcloth ramp is SEA pulled toward grey - sodden canvas, not water - lit
-- warm-green, shadows sinking to the deep navy the outline lives in.
local TIDE = {
  C = palette {
    O = "161a2a",
    ["1"] = "b4d8c4", ["2"] = "6fa49a", ["3"] = "457680", ["4"] = "2c4c62", ["5"] = "1f3248", -- sailcloth
    W = "f6f2e4", G = "cdc8b6", g = "8f97a0", d = "4a5262",                                    -- barnacles
    e = "8cc060", f = "4f8a4a", h = "2e5a44",                                                   -- kelp
    I = "dde6ea", i = "9fb0b8", j = "62748a",                                                   -- cold iron
    R = "e08850", r = "b0582c", q = "7a3420",                                                   -- rust
    L = "d8a868", B = "a0703f", D = "6e4632", E = "4a2e3a",                                     -- tarred leather
    Y = "fff1a8", y = "f5c04a", u = "c7802a",                                                   -- brass
  },
  headName = "TidewrackHood", bodyName = "TidewrackCoat", legsName = "TidewrackBoots",

  -- The hood rides the skull and peaks forward over the brow; its lip is a band of
  -- rusted rivets that stops at row 8, so the outline under it lands on row 9 and
  -- the eye (col 10, rows 10-11) stays bare. The drape behind stops at col 8.
  -- Barnacles crust the crown at the back; a strand of kelp trails off it.
  head = {
    "....................",
    "....................",
    "........WG..........",
    ".......WGg112.......",
    "......Wgd1222233....",
    ".....GgW12222333....",
    ".....g1r22223334....",
    ".....12222333444....",
    "....e1233345554.....",
    "....f1223...........",
    "....e1234...........",
    "...ef2344...........",
    "...f.234............",
    "...e................",
    "...f................",
  },

  -- coat: lapels at the collar, two rows of brass buttons, an anchor chain worn
  -- across the chest from the back shoulder to the front hip, a tarred belt with a
  -- brass buckle, and coat skirts split at the front.
  torso = {
    "12...34",
    "1Ij2y34",
    "12Ij3y4",
    "122Ijy4",
    "1223Ij4",
    "DBDYDBE",
    "123.234",
    "2.3.3.4",
  },
  torsoX = 7, torsoY = 13,

  -- a barnacle crust on the shoulder, no wider than the arm under it
  sode = {
    ".WG.",
    "WgGd",
    "1233",
  },
  sodeX = 4, sodeY = 13,

  -- sleeve: sailcloth; cuff: cold iron manacle with a rust seam
  arm = { "i", "i", "j", "4", "1", "2", "3", "4" },

  -- tarred trousers, then sea boots with a turned-down top and a dark sole
  legRule = function(rel, bottom, first, last)
    if bottom then return first and "D" or "E" end
    if rel <= 1 then return first and "3" or (last and "5" or "4") end
    if rel == 2 then return first and "L" or (last and "D" or "L") end -- turned-down boot top
    return first and "B" or (last and "E" or "D")
  end,
  darker = { ["1"] = "2", ["2"] = "3", ["3"] = "4", ["4"] = "5", L = "B", B = "D", D = "E", I = "i", i = "j" },

  bodyIcon = {
    ".WG..12...34.....",
    "WgGd.1Ij2y34.WG..",
    "1233.12Ij3y4.gGd.",
    "2344.122Ijy4.233.",
    "3445.1223Ij4.344.",
    ".ii..DBDYDBE..ij.",
    ".ij..123.234..jj.",
    ".....2.3.3.4.....",
  },
  legsIcon = {
    "3445.3445",
    "3445.3445",
    "LLLD.LLLD",
    "BDDE.BDDE",
    "BDDE.BDDE",
    "DEEE.DEEEE",
  },
}

------------------------------------------------------------------ SUNWROUGHT
-- slib.lua: OUTLINE 2a1426; GOLDR, LAPIS, TURQ, SANDR, SUNR.
local SUN = {
  C = palette {
    O = "2a1426",
    A = "fff6c8", B = "ffd860", C = "f0a838", D = "c06a2a", E = "7e3a2c", F = "45203a", -- gilded chitin
    l = "9ad8ff", L = "4a8ef0", m = "2a4fb0", n = "1c2a6a",                            -- lapis
    t = "b4fff0", T = "3cc8b4", u = "1a7a7e",                                          -- turquoise
    S = "fff2d2", s = "f2d49a", z = "d9a865", x = "a8733f", X = "6a4232",              -- linen / sand
    W = "fff4b0", R = "ff8a24", r = "d84a1e",                                          -- the sun
  },
  headName = "SunwroughtNemes", bodyName = "SunwroughtCarapace", legsName = "SunwroughtGreaves",

  -- Nemes: a striped cloth that sits flat on the crown, a gold brow band (row 8,
  -- so its outline lands on row 9, clear of the eye), a sun disk set on the brow
  -- in front, and the lappets falling behind the ear in alternating gold and lapis
  -- stripes - the one silhouette none of the other reaper heads has.
  head = {
    "....................",
    "....................",
    "............WR......",
    "........ABBBRr......",
    ".......ALLLLLmn.....",
    "......ABBBBBBCD.....",
    "......LLLLLLLmn.....",
    ".....ABBBBBBBCD.....",
    ".....BCCCCCCCDE.....",
    "....ALmn............",
    "....BBCD............",
    "...ALLmn............",
    "...BBCDE............",
    "...LLmnn............",
    "...CDEF.............",
  },

  -- a lapis-and-turquoise broad collar over two gilded elytra split by a dark seam,
  -- a lapis belt with the sun as its buckle, and a white linen kilt
  torso = {
    "TlLlLlu",
    "BCBCBCD",
    "ABBECCD",
    "ABBECCD",
    "BCCEDDE",
    "LmRWRmn",
    "SsS.zsx",
    "Ssz.zxX",
  },
  torsoX = 7, torsoY = 13,

  -- a small gold wing plate on the shoulder, lapis-edged
  sode = {
    "ABBC",
    "LLmn",
    "BCDE",
  },
  sodeX = 4, sodeY = 13,

  -- linen sleeve, gold bracer
  arm = { "A", "B", "C", "D", "S", "s", "z", "x" },

  -- linen kilt, a lapis knee band, gold greaves, a leather sandal
  legRule = function(rel, bottom, first, last)
    if bottom then return first and "x" or "X" end
    if rel <= 1 then return first and "S" or (last and "z" or "s") end
    if rel == 2 then return first and "L" or (last and "n" or "m") end
    return first and "B" or (last and "D" or "C")
  end,
  darker = { S = "s", s = "z", z = "x", L = "m", m = "n", B = "C", C = "D", D = "E", x = "X" },

  bodyIcon = {
    "......TlLlLlu......",
    "ABBC..BCBCBCD..BBCD",
    "LLmn..ABBECCD..Lmmn",
    "BCDE..ABBECCD..CCDE",
    ".SS...BCCEDDE...zx.",
    ".BC...LmRWRmn...DE.",
    "......SsS.zsx......",
    "......Ssz.zxX......",
  },
  legsIcon = {
    "Sssz.Sssz",
    "Sssz.Sssz",
    "Lmmn.Lmmn",
    "BCCD.BCCD",
    "BCCD.BCCD",
    "xXXX.xXXX",
  },
}

------------------------------------------------------------------ ACCESSORIES
-- Barnacled Hone: a whetstone bar gone green in the sea, a crust of barnacles on
-- its back and a loop of rusted chain through one end.
local HONE_C = palette {
  O = "161a2a",
  ["1"] = "b4d8c4", ["2"] = "6fa49a", ["3"] = "457680", ["4"] = "2c4c62", ["5"] = "1f3248",
  W = "f6f2e4", G = "cdc8b6", g = "8f97a0", d = "4a5262",
  R = "e08850", r = "b0582c", q = "7a3420",
}
local HONE = {
  "..WG...WG..W..",
  ".WgGd.WgGdWGd.",
  "11222WG22222d4",
  "12222222333344",
  "22333333344445",
  "r.34444444555.",
  "Rr............",
  "q.R...........",
  ".rq...........",
}

-- Scarab Amulet: a gold scarab with lapis wing-cases holding the sun over its
-- head, on a short turquoise-bead cord.
local SCARAB_C = palette {
  O = "2a1426",
  A = "fff6c8", B = "ffd860", C = "f0a838", D = "c06a2a", E = "7e3a2c",
  l = "9ad8ff", L = "4a8ef0", m = "2a4fb0", n = "1c2a6a",
  T = "3cc8b4", u = "1a7a7e",
  W = "fff4b0", R = "ff8a24", r = "d84a1e",
}
local SCARAB = {
  "..T.......T..",
  "...u.....u...",
  "....T.W.T....",
  ".....RRRr....",
  "....BRRrrD...",
  "...AB.r..CD..",
  "...ABBCCCDD..",
  "..lLLmBEnLmn.",
  "..lLLmCEnmmn.",
  "..LLmmCEnmnn.",
  "...Lmm.Enmn..",
  "..B..CDE..D..",
}

-- Rimebound Wrap: a short length of haft bound in the Rime's red cloth, frost
-- growing over the wraps and an icicle off the end. Palettes from rlib.lua.
local WRAP_C = palette {
  O = "1c1a36",
  I = "eafcff", i = "a3e9f8", c = "58c3e6", C = "2c87c0",
  R = "f2715a", r = "c8393f", q = "7e2240",
  H = "e3d1a6", h = "b39466", g = "7a5e44", G = "4c3a34",
}
local WRAP = {
  "....hH....",
  "...Hhhg...",
  "..IRrrqi..",
  "..iHhhgc..",
  "...RrrqC..",
  "...Hhhg...",
  "..cRrrq...",
  "...HhhgI..",
  "..IRrrqc..",
  "...Hhhg...",
  "...hhgG...",
  "....iI....",
  "....ci....",
  ".....c....",
}

buildSet(TIDE)
buildSet(SUN)
iconFromMap(HONE, HONE_C, accOut .. "BarnacledHone.png")
iconFromMap(SCARAB, SCARAB_C, accOut .. "ScarabAmulet.png")
iconFromMap(WRAP, WRAP_C, accOut .. "RimeboundWrap.png")
print("biome sets: done")
