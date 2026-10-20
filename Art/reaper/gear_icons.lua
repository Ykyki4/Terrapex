-- Reaper class gear: the accessory line, the potion and its buff. Run from the repo root:
--
--   aseprite -b --script-param root=<repo> --script Art/reaper/gear_icons.lua
--
-- Hand-drawn half-size maps, outlined, 2x2 - the same grid as the scythes. Each one is
-- a thing you could name from its silhouette alone: a pole, a sheaf, a scabbard, a
-- bedroll, a lantern, a badge, a flask.

dofile(app.params.root .. "/Art/reaper/lib.lua")

local ACC = "Content/Items/Accessories/"

local function draw(rows, pal, path)
  local g = newGrid(1, 1)
  stamp(g, rows, pal, 0, 0)
  outline(g)
  save(g, path)
end

local STRAW = { Y = hex("fff0a0"), S = hex("e8c860"), s = hex("c09a40"), z = hex("7a5a28") }

-- Long Haft: a long pole, iron-shod at one end, leather-wrapped at the grip
draw({
  "............ik",
  "...........iik",
  "..........wwk.",
  ".........wwk..",
  "........wwk...",
  ".......wwk....",
  "......wwk.....",
  ".....wwk......",
  "....lLk.......",
  "...LlL........",
  "..lLk.........",
  ".ik...........",
  "ik............",
}, { i = hex("d9d4de"), k = hex("514b5e"), w = hex("d18a4a"), l = hex("9a5140"), L = hex("5e2f45") },
  ACC .. "LongHaft.png")

-- Harvest Charm: a sheaf of straw tied at the waist with a red thread
draw({
  ".Y.S.Y.",
  "YSYSYSY",
  ".SsSsS.",
  "..SsS..",
  "..rRr..",
  "..SsS..",
  ".SsSsS.",
  "SsSzSsS",
  "Ss.z.sS",
}, { Y = STRAW.Y, S = STRAW.S, s = STRAW.s, z = STRAW.z, r = hex("9a2a2a"), R = hex("d8483a") },
  ACC .. "HarvestCharm.png")

-- Bone Sheath: a curved scabbard of dungeon bone, stitched down its spine, with a
-- dark mouth where the blade goes in
draw({
  "......kk.....",
  ".....abbk....",
  ".....abbc....",
  "......abbc...",
  "......abtbc..",
  ".......abtbc.",
  "........abtbc",
  ".........abbc",
  "..........abc",
  "...........cc",
}, { a = hex("fff0c8"), b = hex("e8cf94"), c = hex("8a6b45"), t = hex("5a4030"), k = hex("241733") },
  ACC .. "BoneSheath.png")

-- Reaper's Kit: a bedroll tied with two straps, the whetstone, the straw and the
-- bone sheath's tip all poking out of it - three things made one
draw({
  ".......vvv....",
  "..Y.S..vxxv...",
  ".YSYS..vvv....",
  "..aabbbbbbbc..",
  ".abbRbbbbRbcc.",
  "abbbRbbbbRbbcc",
  "abbbRbbbbRbbcd",
  ".bccRccccRccd.",
  "..cddddddddd..",
  "...........ee.",
  "............e.",
}, { Y = STRAW.Y, S = STRAW.S, a = hex("c89a6a"), b = hex("9a6a44"), c = hex("6a4430"), d = hex("3a2418"),
     R = hex("5e2f45"), v = hex("93849f"), x = hex("635478"), e = hex("e8cf94") },
  ACC .. "ReapersKit.png")

-- Soul Lantern: a hallowed frame, a ring to carry it by, and two souls inside - one
-- of light, one of night
draw({
  "...ggg...",
  "..g...g..",
  "..ggggg..",
  ".gGGGGGg.",
  ".gpPgbBg.",
  ".gpWgbWg.",
  ".gPpgBbg.",
  ".gGGGGGg.",
  "..ggggg..",
  "...g.g...",
}, { g = hex("c7802a"), G = hex("f5c04a"), p = hex("ff9ad9"), P = hex("d94ecb"), b = hex("7ae0ff"),
     B = hex("3a9ad8"), W = hex("ffffff") },
  ACC .. "SoulLantern.png")

-- Grim Insignia: the emblem's round plate gone dark - a silver rim, a black field,
-- a skull with a scythe behind it, and an ectoplasm gem at the top
draw({
  "....aaE.a....",
  "..aabbEEbbc..",
  ".abkkkkkkkbc.",
  ".bkkWWkkkskc.",
  "abkWWWWksskbc",
  "abkWkWWskkkbc",
  "abkWWWWskkkbc",
  "bbkkWWskkkkcc",
  ".bkkkskkkkkc.",
  ".bckskkkkkcc.",
  "..ccccccccc..",
  "....ccccc....",
}, { a = hex("eef1f8"), b = hex("9aa2bc"), c = hex("4a5070"), k = hex("1a1626"), W = hex("e8e4f0"),
     s = hex("c8cee0"), E = hex("7affd8") },
  ACC .. "GrimInsignia.png")

-- Harvest Potion: a round-bellied flask of gold, a straw stopper
draw({
  "...SY...",
  "...zs...",
  "...gg...",
  "..gwHg..",
  ".gwHHHg.",
  "gwHHHHHg",
  "gHHhhHHg",
  "gHhhhhHg",
  ".ghhhhg.",
  "..gggg..",
}, { S = STRAW.S, Y = STRAW.Y, z = STRAW.z, s = STRAW.s, g = hex("8a92d0"), w = hex("eaf4ff"),
     H = hex("ffd860"), h = hex("e0a020") },
  "Content/Items/Consumables/HarvestPotion.png")

-- Harvest buff icon, 32x32: a gold sickle across a sheaf, on the buff frame's dark
do
  local g = newGrid(16, 16)
  for y = 0, 15 do
    for x = 0, 15 do
      put(g, x, y, (x + y) % 2 == 0 and hex("2a2034") or hex("261c30"))
    end
  end
  stamp(g, {
    "..........GGG.",
    ".........G...G",
    "..Y.S.Y.......G",
    ".YSYSYSY......G",
    "..SsSsS......G.",
    "...SsS......G..",
    "...rRr....GG...",
    "...SsS...g.....",
    "..SsSsS.g......",
    ".SsSzSsg.......",
    "......g........",
    ".....g.........",
  }, { Y = STRAW.Y, S = STRAW.S, s = STRAW.s, z = STRAW.z, r = hex("9a2a2a"), R = hex("d8483a"),
       G = hex("f5c04a"), g = hex("9a5140") }, 1, 2)
  save(g, "Content/Buffs/HarvestBuff.png")
end

print("gear icons: done")
