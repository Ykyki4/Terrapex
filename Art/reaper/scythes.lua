-- The scythes with shapes of their own. Run from the repo root:
--
--   aseprite -b --script-param root=<repo> --script Art/reaper/scythes.lua
--
-- The first pass built nine of these from one crescent rule and changed only the
-- palette, and the verdict was right: "просто меняются цвета". A family of weapons
-- has to differ in silhouette first, so each blade here is hand-drawn as a char
-- map with its own idea - a claw, a pincer, a lantern, an eye, a leaf - and only
-- the hafts still come from the shared rule. Read each map as the sprite at half
-- size: one character is one 2x2 block, '.' is empty, the outline is added after.

dofile(app.params.root .. "/Art/reaper/lib.lua")

local function finish(g, path)
  outline(g)
  save(g, path)
end

local DIR = "Content/Items/Weapons/Reaper/"

-------------------------------------------------------------------------------
-- Frostbite Sickle, 16x16: not a sickle of ice but a CLAW of it - three
-- icicles growing out of a fur-wrapped collar, the longest curling forward
do
  local g = newGrid(16, 16)
  haft(g, 1.4, 15.0, 5.4, 8.6, 1.05, WOOD[2], WOOD[3], WOOD[4], { 0.5 })
  stamp(g, {
    "........w.......",
    "........ab......",
    ".......abc....w.",
    ".......abc...abc",
    ".......abc..abcd",
    "......aabc.abcd.",
    "......abbcabcd..",
    ".....aabbbbcd...",
    "......bbbcccbb..",
    ".......dccd.bcc.",
    "............bcd.",
    "...........bcd..",
    "..........bcd...",
    "..........cd....",
  }, { w = hex("ffffff"), a = hex("eafcff"), b = hex("b0e8ff"), c = hex("6ab8e8"), d = hex("3a74b0") }, 0, 0)
  stamp(g, { "ab", "ba" }, { a = hex("f4efe6"), b = hex("c9bdd1") }, 4, 7)
  finish(g, DIR .. "FrostbiteSickle.png")
end

-------------------------------------------------------------------------------
-- Gravedigger, 20x20: a long plain haft with a short mean hook at the top, and
-- a soul lantern hung from the neck on a chain - the gravedigger works at night
do
  local g = newGrid(20, 20)
  haft(g, 1.2, 18.8, 9.4, 2.6, 1.05, hex("8a7a60"), hex("5e5040"), hex("3a3028"), { 0.3, 0.62 })
  stamp(g, {
    ".aaab.....",
    "abbbbbbb..",
    "..eeebbbbc",
    ".....eebbc",
    ".......ebc",
    "........ec",
    "........e.",
  }, { a = hex("a8a4b4"), b = hex("6a6676"), c = hex("33303f"), e = hex("c4ecd6") }, 9, 0)
  -- the lantern: a chain, an iron cage, a green soul burning inside it
  stamp(g, {
    ".k.",
    ".k.",
    "mmm",
    "mgm",
    "mGm",
    "mgm",
    ".m.",
  }, { k = hex("4a4658"), m = hex("6a6676"), g = hex("7ae0a8"), G = hex("e8fff0") }, 10, 3)
  finish(g, DIR .. "Gravedigger.png")
end

-------------------------------------------------------------------------------
-- Sandsweeper, 20x20: an antlion's head on a fossil haft - two mandibles
-- curving toward each other, teeth on the inside. A pincer, not a blade
do
  local g = newGrid(20, 20)
  haft(g, 1.2, 18.8, 7.6, 6.4, 1.15, hex("e8dcc0"), hex("bfae8a"), hex("8a7a5a"), { 0.3, 0.55, 0.8 })
  stamp(g, {
    "...aaab......",
    "..abbbbbbb...",
    ".abbcccbbbcc.",
    "abbc..dccbbbc",
    "abc......dcbc",
    "bbc.......tdc",
    "bc.......t.dc",
    "bc..........d",
    "abc.....t....",
    ".abbbbt......",
    "..bbbbbbcc...",
    "...cccccdd...",
  }, { a = hex("fbe3a0"), b = hex("e0b062"), c = hex("b07a3c"), d = hex("6e4426"), t = hex("fff4d0") }, 6, 0)
  finish(g, DIR .. "Sandsweeper.png")
end

-------------------------------------------------------------------------------
-- Fissurite Scythe, 20x20: a slab of stone, not a curve - flat faces with hard
-- breaks between them, a rift crack glowing in it, and the end snapped off
-- square where the stone gave
do
  local g = newGrid(20, 20)
  haft(g, 1.2, 18.8, 8.0, 4.4, 1.1, WOOD[1], WOOD[2], WOOD[3], { 0.3, 0.62 })
  stamp(g, {
    "..aaaa........",
    ".aaabbaar.....",
    "eaabbbbbsaa...",
    ".eebbbbbbsbaa.",
    "...eecbbbrbbba",
    ".....eccbbbbbb",
    "......eecccbbc",
    "........eecccd",
    ".........eeccd",
    ".........dddcd",
    ".........d..dd",
  }, { a = hex("c9bdd1"), b = hex("93849f"), c = hex("635478"), d = hex("3f3357"), e = hex("f4efe6"),
       r = RIFT[1], s = RIFT[2] }, 6, 1)
  stamp(g, { "pq", "qp" }, { p = DENSE[2], q = DENSE[3] }, 7, 3)
  finish(g, DIR .. "FissuriteScythe.png")
end

-------------------------------------------------------------------------------
-- Lidcutter, 20x20: the blade is an eyelid - a closed almond of stone with the
-- Dormant Eye looking out of it, and the lower lid sharpened into the edge
do
  local g = newGrid(20, 20)
  haft(g, 1.2, 18.8, 7.4, 6.0, 1.15, DENSE[1], DENSE[2], DENSE[4], { 0.4 })
  stamp(g, {
    "......aaa.....",
    "...aaabbbbaa..",
    ".aabbbbbbbbbba",
    "abbbcwwwwcbbbc",
    "abbcwwiiiwcbbc",
    "bbbcwiippiwcbc",
    "bccbcwiiiwcccd",
    ".bcccwwwwccdd.",
    "..ccddddddd...",
    "...eeeeeeee...",
  }, { a = hex("c9bdd1"), b = hex("93849f"), c = hex("635478"), d = hex("3f3357"), e = hex("f4efe6"),
       w = hex("e8cf94"), i = RIFT[2], p = hex("241733") }, 6, 1)
  finish(g, DIR .. "Lidcutter.png")
end

-------------------------------------------------------------------------------
-- Glass Reaper, 22x22: no single blade at all - three shards of riftglass hang
-- in an arc off the neck, held in place by nothing. The gaps are the idea:
-- light bends across them
do
  local g = newGrid(22, 22)
  haft(g, 1.2, 20.8, 9.0, 4.4, 1.1, DENSE[1], DENSE[2], DENSE[4], { 0.25, 0.5, 0.75 })
  local P = { ["1"] = hex("eaf4ff"), ["2"] = hex("b9c6ee"), ["3"] = hex("8a92d0"), ["4"] = hex("5d5c9e"),
              w = hex("ffffff"), o = hex("3a3466"), R = RIFT[1] }
  stamp(g, { ".w122..", "1222333", ".23344." }, P, 9, 1)
  stamp(g, { ".12.", "1w23", "2233", "2334", ".34." }, P, 16, 4)
  stamp(g, { "12.", "233", "234", ".34", ".4." }, P, 17, 11)
  stamp(g, { ".o.", "oRo", ".o." }, P, 8, 3)
  finish(g, DIR .. "GlassReaper.png")
end

-------------------------------------------------------------------------------
-- Rotreaper, 20x20: a worm, not a blade - a body that bulges along its back,
-- a row of teeth along its belly, holes eaten through it, on a knotted haft
do
  local g = newGrid(20, 20)
  haft(g, 1.2, 18.8, 7.8, 4.6, 1.1, hex("7a6a8a"), hex("54486a"), hex("33293f"), { 0.25, 0.5, 0.75 })
  stamp(g, { "k.", ".k", "k." }, { k = hex("33293f") }, 2, 12)
  stamp(g, {
    "...aa.aa......",
    "..abbabbaa....",
    ".abbbbbbbba.a.",
    "abbcbb.cbbbaba",
    "abccccccbbbbbb",
    ".tctctccc.cbbc",
    "......tctcccbc",
    "..........ccbc",
    "..........tccd",
    "...........ccd",
    "..........tcd.",
    "...........d..",
  }, { a = hex("c4aee0"), b = hex("9a7ac4"), c = hex("6a4c98"), d = hex("40306a"),
       t = hex("e8f0b0") }, 6, 1)
  stamp(g, { "v", "V" }, { v = hex("a8c04a"), V = hex("6e8a2c") }, 10, 3)
  finish(g, DIR .. "Rotreaper.png")
end

-------------------------------------------------------------------------------
-- Veinreaper, 20x20: a spine for a haft - vertebrae, knob after knob - and a
-- rib for a blade, with a band of red flesh still on its inner edge
do
  local g = newGrid(20, 20)
  local BONE = { hex("fff0c8"), hex("e8cf94"), hex("c0a066"), hex("8a6b45") }
  -- the spine: a thin bone with a wider vertebra every third cell
  haft(g, 1.2, 18.8, 7.8, 4.8, 0.8, BONE[2], BONE[3], BONE[4])
  for _, v in ipairs({ { 2, 16 }, { 3, 13 }, { 5, 10 }, { 6, 7 } }) do
    stamp(g, { "ab", "bc" }, { a = BONE[1], b = BONE[2], c = BONE[3] }, v[1], v[2])
  end
  stamp(g, {
    "..aaab........",
    ".abbbbbbb.....",
    "abffffbbbbb...",
    ".ffrrrfffbbbc.",
    "..rr..rrffbbc.",
    ".........rfbbc",
    "..........rfbc",
    "...........rbc",
    "...........rfc",
    "............rc",
    "............r.",
  }, { a = BONE[1], b = BONE[2], c = BONE[4], f = hex("e04848"), r = hex("8a1a2a") }, 6, 1)
  finish(g, DIR .. "Veinreaper.png")
end

-------------------------------------------------------------------------------
-- Hivecutter, 20x20: a chunk of honeycomb on a hive-wood haft that runs out
-- into a long striped stinger - the comb is the weight, the stinger is the cut
do
  local g = newGrid(20, 20)
  haft(g, 1.2, 18.8, 7.6, 5.0, 1.1, hex("b07840"), hex("7a4a28"), hex("3a2018"), { 0.4 })
  stamp(g, {
    "..yyyy........",
    ".yhyyoy.......",
    "yhyoyyoy......",
    "yyoyyoyyo.....",
    "oyyoyyoyok....",
    ".oyyoyyokkyy..",
    "..ooddokkyykk.",
    ".......kyykkyy",
    "...........kyk",
    "............kd",
    "............d.",
  }, { h = hex("fff1a0"), y = hex("f5c03a"), o = hex("c8801a"), d = hex("7a4510"), k = hex("2a1c18") }, 6, 1)
  stamp(g, { "a", "b", ".", "b" }, { a = hex("f5c03a"), b = hex("fff1a0") }, 7, 8)
  finish(g, DIR .. "Hivecutter.png")
end

-------------------------------------------------------------------------------
-- Thornreaper, 20x20: the blade is one great leaf - midrib, veins, a point -
-- and a vine tendril curls off the neck the way a climbing plant reaches
do
  local g = newGrid(20, 20)
  local VINE = { hex("8ab050"), hex("5a7a30"), hex("33461e") }
  haft(g, 1.2, 18.8, 7.8, 4.6, 1.05, VINE[1], VINE[2], VINE[3], { 0.2, 0.45, 0.7 })
  stamp(g, {
    "...aab.......",
    ".aabbbbb.....",
    "aabbvbbbbb...",
    ".abbbvbbbbcc.",
    "..cbbbvvbbccc",
    "...ccbbbvbccc",
    "....cccbbvccd",
    ".....cccccvcd",
    "......dcccccd",
    "........dccd.",
    ".........dd..",
    "..........d..",
  }, { a = hex("d8f8a0"), b = hex("8ad458"), c = hex("4a9a38"), d = hex("2a5a2a"), v = hex("2a5a2a") }, 7, 0)
  -- the tendril, a spiral hanging off the neck
  stamp(g, { ".vv.", "v..v", "v.vv", ".v.." }, { v = VINE[2] }, 3, 3)
  stamp(g, { "a......", ".......", "...a...", ".......", "......a" }, { a = VINE[3] }, 1, 9)
  finish(g, DIR .. "Thornreaper.png")
end

-------------------------------------------------------------------------------
-- Hellreaper, 24x24: two blades, one at each end of the haft - the big one
-- burning along its back with three tongues of flame, the small one a hooked
-- spike at the butt. The only double-ended scythe before hardmode
do
  local g = newGrid(24, 24)
  local OBS = { hex("8a4a4a"), hex("5e2e34"), hex("3e1c26"), hex("26101a"), hex("ffc04a") }
  local LAVA = { hex("ffe08a"), hex("ff8a2a"), hex("c8401a") }
  haft(g, 3.0, 21.0, 10.0, 5.0, 1.15, hex("6a6676"), hex("44404e"), hex("26222e"), { 0.3, 0.6 })
  local A, B = { x = 15.4, y = 12.0, r = 9.0 }, { x = 12.4, y = 16.6, r = 8.2 }
  crescent(g, A, B, function(x, y) return x > 9.8 and y < 18.0 end, OBS)
  eachBladeCell(g, A, B, -160, function(x, y, s, fromSpine, fromEdge)
    if fromEdge < 1.8 and fromEdge >= 0.9 then put(g, x, y, LAVA[2]) end
  end)
  -- flames rising off the spine
  stamp(g, {
    "....a.....a.......",
    "...ab....ab...a...",
    "...bc...abc..ab...",
    "..bcc...bc...bc...",
  }, { a = LAVA[1], b = LAVA[2], c = LAVA[3] }, 6, 0)
  -- the butt spike, hooked back toward the ground
  stamp(g, {
    "..ab",
    ".abc",
    "abc.",
    "bc..",
    "c...",
  }, { a = OBS[1], b = OBS[2], c = LAVA[2] }, 0, 19)
  finish(g, DIR .. "Hellreaper.png")
end

print("scythes: done")
