-- The wilds: three ordinary animals of Terraria's own biomes, no rift anywhere near them.
--
--   aseprite -b --script-param root=<repo> --script Art/wilds/wilds.lua
--
-- 2x2 grid like the rest of the mod's new content. NPC sprites face LEFT - Terraria
-- flips a sheet when the NPC faces right - and every frame of a sheet sits on one
-- canvas so the frames line up.

dofile(app.params.root .. "/Art/reaper/lib.lua")

local ROOT = app.params.root

-- frames: list of row-tables, all the same art size; written as one vertical sheet
local function sheet(frames, pal, w, h, path)
  local img = Image(w * 2, h * 2 * #frames, ColorMode.RGB)
  for f, rows in ipairs(frames) do
    local g = newGrid(w, h)
    stamp(g, rows, pal, 0, 0)
    outline(g)
    for _, c in ipairs(cells(g)) do
      local x, y = c[1], c[2]
      if x >= 0 and x < w and y >= 0 and y < h then
        for sy = 0, 1 do
          for sx = 0, 1 do img:drawPixel(x * 2 + sx, (f - 1) * h * 2 + y * 2 + sy, c[3]) end
        end
      end
    end
  end
  local spr = Sprite(img.width, img.height, ColorMode.RGB)
  spr.cels[1].image = img
  spr:saveAs(ROOT .. "/" .. path)
end

-------------------------------------------------------------------------------
-- Thornback Boar, 24x15 art per frame: a dark bristled boar whose back is a hedge
-- of green bramble thorns; a pale tusk, a red eye. Five frames: four of the trot,
-- one of the scrape with a hind leg kicked back.
do
  local BODY = {
    "........G...G...G..G....",
    ".......gG..gG..gG.gG....",
    "....k..gaagaaagaagaG....",
    "...kkaaAAaAAAaAAaaaag...",
    "..aaAAAaaaaaaaaaaaaabb..",
    ".aaebaaaaaaaaaabbbbbbbc.",
    "papbbbbbbbbbbbbbbbbbbbc.",
    "pppbbbbbbbbbbbbbbbbbbcc.",
    "WwbbbbbbcbbbbbbbbbbbccC.",
    "Ww.cbbbcccccccbbbbcccC..",
    "...cCccc.....cccccCC....",
  }
  local LEGS = {
    { "....bc..bc.....bc.bc....", "....bc..bc.....bc.bc....", "....dd..dd.....dd.dd...." },
    { "...bc....bc...bc...bc...", "..bc......bc.bc.....bc..", "..dd......dd.dd.....dd.." },
    { "....bc..bc.....bc.bc....", "....bc..bc.....bc.bc....", "....dd..dd.....dd.dd...." },
    { ".....bcbc.......bcbc....", ".....bcbc.......bcbc....", ".....dddd.......dddd...." },
  }
  local SCRAPE = { "....bc..bc.....bc..bc...", "...bc...bc.....bc....bcc", "...dd...dd.....dd.....dd" }
  local frames = {}
  for f = 1, 4 do
    local rows = {}
    for _, r in ipairs(BODY) do rows[#rows + 1] = r end
    for _, r in ipairs(LEGS[f]) do rows[#rows + 1] = r end
    frames[f] = rows
  end
  local rows = {}
  for _, r in ipairs(BODY) do rows[#rows + 1] = r end
  for _, r in ipairs(SCRAPE) do rows[#rows + 1] = r end
  frames[5] = rows
  sheet(frames, {
    A = hex("c8905a"), a = hex("9a6040"), b = hex("6a4028"), c = hex("4a2a2c"), C = hex("33202e"),
    d = hex("2e1a22"), k = hex("5e3424"),
    g = hex("4a7a2a"), G = hex("b8e070"), w = hex("f4efe6"), W = hex("c9bda8"), e = hex("ff5a3a"), p = hex("c88a7a"),
  }, 24, 14, "Content/NPCs/Wilds/ThornbackBoar.png")
end

-------------------------------------------------------------------------------
-- Lantern Mole, 16x11 art: a velvet-grey mole, pink nose, big pale digging claws,
-- and a little brass lantern strapped to its head. Run x2, then the leap.
do
  local TOP = {
    "......y.........",
    ".....yYy........",
    ".....kyk........",
    "...aabbbbb......",
    ".paebbbbbbbb....",
    "ppabbbbbbbbbc...",
    "wwbbbbbbbbbbbc..",
    ".wcbbbbbbbbbcc..",
    "..ccbcccbbccc...",
  }
  local frames = {
    { "..cc.cc..cc.....", ".ww....w..w....." },
    { ".cc..cc...cc....", "ww....w....w...." },
    { "wwcc.......cc...", "ww..........w..." },
  }
  local out = {}
  for f, legs in ipairs(frames) do
    local rows = {}
    for _, r in ipairs(TOP) do rows[#rows + 1] = r end
    for _, r in ipairs(legs) do rows[#rows + 1] = r end
    out[f] = rows
  end
  sheet(out, {
    a = hex("9a8a8a"), b = hex("5e5254"), c = hex("3e3438"), p = hex("e8a0a0"), w = hex("f0e0c0"),
    e = hex("241733"), y = hex("ffd860"), Y = hex("fff6c8"), k = hex("6a5040"),
  }, 16, 11, "Content/NPCs/Wilds/LanternMole.png")
end

-------------------------------------------------------------------------------
-- Pitcher Snapper, 16x22 art: a jug-shaped pitcher plant on a stem with two
-- leaves, a red lip, a lid. Idle; swelling (the jug widens, the lid lifts);
-- spitting (lid thrown open, a drop flying).
do
  local STEM = {
    ".......t........",
    ".......t........",
    "..LL...t...LL...",
    ".LllL..t..LllL..",
    "..Lll.tt.lllL...",
    "....lltttll.....",
    ".......t........",
    ".....ttttt......",
  }
  local function jug(width, lidLift, drops)
    local rows = {}
    local lid = { "......lLLl......", ".....lLLLLl....." }
    for i = 1, lidLift do rows[#rows + 1] = "................" end
    rows[#rows + 1] = drops and ".....a....a....." or lid[1]
    rows[#rows + 1] = drops and "......a..a......" or lid[2]
    local pad = function(s) return s end
    local body = width == 0 and {
      ".....rRRRr......",
      "....gGGGGGg.....",
      "...gGhGGGGGg....",
      "...gGhGGsGGg....",
      "...gGGGGGGgg....",
      "...gGGsGGGgg....",
      "....gGGGGgg.....",
      ".....ggggg......",
    } or {
      ".....rRRRr......",
      "...ggGGGGGgg....",
      "..gGhGGGGGGGg...",
      "..gGhGGGsGGGg...",
      "..gGGGGGGGGgg...",
      "..gGGGsGGGGgg...",
      "...gGGGGGGgg....",
      "....ggggggg.....",
    }
    for _, r in ipairs(body) do rows[#rows + 1] = pad(r) end
    for _, r in ipairs(STEM) do rows[#rows + 1] = r end
    while #rows < 22 do table.insert(rows, 1, "................") end
    while #rows > 22 do table.remove(rows, 1) end
    return rows
  end
  sheet({ jug(0, 2, false), jug(1, 1, false), jug(0, 0, true) }, {
    G = hex("8ad058"), g = hex("4a9038"), h = hex("d8f8a0"), s = hex("8a4a8a"), r = hex("c84a6a"),
    R = hex("ff7a9a"), l = hex("6aa848"), L = hex("c8f090"), t = hex("5a8a30"), a = hex("b8f070"),
  }, 16, 22, "Content/NPCs/Wilds/PitcherSnapper.png")
end

-- the spit: a glob of green acid
do
  local g = newGrid(1, 1)
  stamp(g, { ".ab.", "abbc", "bbcc", ".cc." }, { a = hex("e0ffb0"), b = hex("8ad058"), c = hex("4a9038") }, 0, 0)
  outline(g)
  save(g, "Content/Projectiles/Wilds/PitcherSpit.png")
end

-- Tusk Charm: a curved boar's tusk on a leather cord
do
  local g = newGrid(1, 1)
  stamp(g, {
    "..ccc..cc.",
    ".c...cc..c",
    "c.........",
    "c...wwW...",
    ".c.wwW....",
    "..wwW.....",
    "..wW......",
    "..W.......",
  }, { c = hex("9a5140"), w = hex("f4efe6"), W = hex("c9bda8") }, 0, 0)
  outline(g)
  save(g, "Content/Items/Accessories/TuskCharm.png")
end

-- Mole's Lantern: a small brass lantern with a hook
do
  local g = newGrid(1, 1)
  stamp(g, {
    "...kk...",
    "..k..k..",
    "..kkkk..",
    ".kBBBBk.",
    ".BYyyYB.",
    ".BYWWYB.",
    ".BYyyYB.",
    ".kBBBBk.",
    "..kkkk..",
  }, { k = hex("6a5040"), B = hex("c8902a"), Y = hex("ffd860"), y = hex("ffb840"), W = hex("fff6c8") }, 0, 0)
  outline(g)
  save(g, "Content/Items/Accessories/MolesLantern.png")
end

print("wilds: done")
