-- The two accessory trees of CRAFTING_TREES.md: the Wayfarer boots line and the Squall
-- Aegis dash line. Run from the repo root:
--
--   aseprite -b --script-param root=<repo> --script Art/trees/accessories.lua
--
-- Hand-drawn half-size maps, outlined in the art buffer, blitted 2x2 - the scythes' grid.
-- Each merge is drawn out of its ingredients: the Wayfarer's Boots wear the Rimetread's fur
-- cuff, the Sandstrider's wrap, the Tidewalker's shell and teal strap, Dusty's rift dust and
-- a Hermes wing; every later boot keeps that silhouette and re-plates it in its tier. The
-- aegis line is one shield growing: the gull's feather tucked behind it from the first node
-- on, the Shield of Cthulhu's eye at its boss, a ring of scales once the Turtle Shell goes in,
-- a heater outline when the Paladin's Shield does.

dofile(app.params.root .. "/Art/reaper/lib.lua")

local DIR = "Content/Items/Accessories/Trees/"

local function draw(rows, pal, name)
  local g = newGrid(1, 1)
  stamp(g, rows, pal, 0, 0)
  outline(g)
  save(g, DIR .. name .. ".png")
end

-- shared ramps ---------------------------------------------------------------------------
local LEATHER = { hex("f0c07a"), hex("d18a4a"), hex("9a5140"), hex("5e2f45") }
local SHELL = { hex("fff0c8"), hex("e8cf94"), hex("c0a066") }
local TEAL = { hex("7fd4e0"), hex("2f8aa0"), hex("1d5a7a") }
local SANDC = { hex("f0d9a0"), hex("d8b06a"), hex("a87a44"), hex("6a4a2a") }
local GILD = { hex("fff1a8"), hex("f5c04a"), hex("c7802a") }
local FUR = { hex("f4f7ff"), hex("c9d6ea"), hex("8ea2c4") }
local ICE = { hex("eaffff"), hex("8ae4f8"), hex("3a8ac0") }
local PINK = { hex("ff9ad9"), hex("d94ecb") }
local WING = { hex("ffffff"), hex("d9d4de"), hex("a8a2b3") }
local STONEP = { hex("f4efe6"), hex("c9bdd1"), hex("93849f"), hex("635478"), hex("3f3357") }
local RIFTP = { hex("ffffff"), hex("ff9ad9"), hex("d94ecb"), hex("8a2f9c") }
local THREAD = { hex("d8fff7"), hex("7ff2e0"), hex("35c9b8"), hex("1c7d78") }
local VOID = { hex("9fb4bb"), hex("61818c"), hex("3d5560"), hex("22343e"), hex("0f1c26") }
local LAVA = { hex("ffd27a"), hex("ff7a3a"), hex("c8283a") }
local SILK = { hex("eef9f6"), hex("c2e0da"), hex("93b8b6"), hex("648d90"), hex("3d6068") }
local PRIME = { hex("ffffff"), hex("eef1f8"), hex("c8cee0"), hex("9aa2bc"), hex("666e8c"), hex("3b4260") }
local REGARD = { hex("ffd79a"), hex("ff9f4a") }

-- Tidewalker Sandals: a driftwood sole, a teal toe strap crusted with barnacles, and an
-- ankle strap held by a seashell
draw({
  "....zSSz........",
  "...zSssSz.......",
  "....zssz........",
  "....tTTt........",
  "....t..t........",
  "...tT..Tt.......",
  "..tT....Tt..nBn.",
  "..t......tTtBnBn",
  ".hhhhhhhhhhhhhhh",
  ".dddddddddddddd.",
  "..kkkkkkkkkkkk..",
}, { S = SHELL[1], s = SHELL[2], z = SHELL[3], t = TEAL[2], T = TEAL[1], n = hex("635478"), B = hex("93849f"),
     h = LEATHER[1], d = LEATHER[2], k = LEATHER[3] },
  "TidewalkerSandals")

-- Sandstrider Wraps: a foot bound in desert cloth, the bands running on the diagonal, a
-- gilded chitin guard on the shin and a puff of sand off the toe
draw({
  "...gGGg.........",
  "..WWWvWw........",
  "..WWvWWw........",
  "..WvWWWw........",
  "..gGGGGg........",
  "..GgggGg........",
  "..WWWvWWw.......",
  "..WWvWWWvw......",
  "..WvWWWvWWWw....",
  "..vWWWvWWWvWWw..",
  "..kkkkkkkkkkkkk.",
  "..............ss",
  "............s.s.",
}, { W = SANDC[1], w = SANDC[2], v = SANDC[3], k = SANDC[4], G = GILD[2], g = GILD[3], s = SHELL[2] },
  "SandstriderWraps")

-- Rimetread Boots: a heavy fur cuff, a leather boot with a rime crystal at the shin, and
-- a skate blade of ice under the sole
draw({
  "..FfFFfF.......",
  ".FFfFFfFF......",
  ".fFhFfhFf......",
  "..lLLLLd.......",
  "..lLCcLd.......",
  "..lLcCLd.......",
  "..lLLLLLdd.....",
  "..lLLLLLLLdd...",
  "..lLLLLLLLLLd..",
  "..ooooooooooo..",
  "...i......i....",
  "..IIIIIIIIIIIi.",
}, { F = FUR[1], f = FUR[2], h = FUR[3], l = LEATHER[1], L = LEATHER[2], d = LEATHER[3], o = LEATHER[4],
     C = ICE[1], c = ICE[2], i = ICE[3], I = ICE[2] },
  "RimetreadBoots")

-- Wayfarer's Boots: one boot carrying all of them - the Rimetread's fur cuff, the
-- Sandstrider's wrap across the shin, the Tidewalker's shell buckle and teal sole strap,
-- Dusty's rift dust under the toe, and a Hermes wing off the heel
draw({
  "....FfFFfF.......",
  "...FFfFFfFF......",
  "...fFhFfhFf......",
  "....lLLLLd.......",
  "....WWvWWw.......",
  "....WvWWvw.......",
  "XX..lLzSzd.......",
  "xXX.lLSssd.......",
  ".xXxlLLzLLdd.....",
  "..x.lLLLLLLLdd...",
  "....lLLLLLLLLLd..",
  "....tTtTtTtTtTtT.",
  "....kkkkkkkkkkkkk",
  ".....p...P...p...",
}, { F = FUR[1], f = FUR[2], h = FUR[3], l = LEATHER[1], L = LEATHER[2], d = LEATHER[3], k = LEATHER[4],
     W = SANDC[1], w = SANDC[2], v = SANDC[3], S = SHELL[1], s = SHELL[2], z = SHELL[3],
     t = TEAL[2], T = TEAL[1], p = PINK[2], P = PINK[1], X = WING[1], x = WING[3] },
  "WayfarersBoots")


-- Riftstride Boots: the Wayfarer's silhouette re-plated in riftsteel, a rift seam down the
-- shin, the Hermes wing turned to a steel fin, and a rocket nozzle under the heel
draw({
  "....FfFFfF.......",
  "...FFfFFfFF......",
  "...fFhFfhFf......",
  "....aAArAc.......",
  "....WWvWWw.......",
  "....WvWWvw.......",
  "MM..aArzSc.......",
  "mMM.aArSsc.......",
  ".mMmaAArAAcc.....",
  "..m.aAAArAAAcc...",
  "....aAAAAArAAAc..",
  "....tTtTtTtTtTtT.",
  "..nNkkkkkkkkkkkkk",
  "..NnN............",
  "..fFf............",
  "...f.............",
}, { F = FUR[1], f = FUR[2], h = FUR[3], a = STONEP[2], A = STONEP[3], c = STONEP[4], k = STONEP[5],
     r = RIFTP[2], W = SANDC[1], w = SANDC[2], v = SANDC[3], S = SHELL[1], s = SHELL[2], z = SHELL[3],
     t = TEAL[2], T = TEAL[1], M = STONEP[1], m = STONEP[3], n = hex("584a6e"), N = hex("847696") },
  "RiftstrideBoots")

-- Threadstep Boots: the same boot in void, the fur cuff and the sand wrap both wound in
-- thread now, a bead of the Anklet of the Wind at the ankle, a sole of Lava Waders glow,
-- and the thread paying out behind the heel
draw({
  "....aAAaAA.......",
  "...aAbAAbAa......",
  "...yYyYyYyY......",
  "....bBBtBc.......",
  "....yYyYyY.......",
  "....YyYyYy.......",
  "MM..bBtgGc.......",
  "mMM.bBtGgc.......",
  ".mMmbBBtBBcc.....",
  "..m.bBBBtBBBcc...",
  "....bBBBBBtBBBc..",
  "....LlLlLlLlLlLl.",
  "..nNkkkkkkkkkkkkk",
  "..NnN............",
  "yYYyy............",
  "...y.............",
}, { a = VOID[1], A = VOID[2], b = VOID[2], B = VOID[3], c = VOID[4], k = VOID[5],
     y = THREAD[3], Y = THREAD[2], t = THREAD[2], g = THREAD[1], G = THREAD[3],
     L = LAVA[2], l = LAVA[3], M = VOID[1], m = VOID[3], n = VOID[4], N = VOID[2] },
  "ThreadstepBoots")

-- Worldwalker Boots: the boot gone prime - every layer the line has worn, now one pale
-- shell, the thread seam still running down it and one warm stud where the shell was
draw({
  "....aAAaAA.......",
  "...aAAAAAAa......",
  "...bAbAbAbA......",
  "....aAAtAc.......",
  "....BbBBbB.......",
  "....bBBbBB.......",
  "MM..aAtRrc.......",
  "mMM.aAtrRc.......",
  ".mMmaAAtAAcc.....",
  "..m.aAAAtAAAcc...",
  "....aAAAAAtAAAc..",
  "....RbRbRbRbRbRb.",
  "..nNkkkkkkkkkkkkk",
  "..NnN............",
  "..rRr............",
  "...r.............",
}, { a = PRIME[1], A = PRIME[2], b = PRIME[3], B = PRIME[4], c = PRIME[5], k = PRIME[6],
     t = THREAD[2], R = REGARD[1], r = REGARD[2], M = PRIME[1], m = PRIME[4], n = PRIME[5], N = PRIME[3] },
  "WorldwalkerBoots")

-- Gull Feather: a white flight feather, grey on its under-vane, the black tip a gull's
-- wing ends in, and a bone quill
draw({
  "...........kk..",
  "..........kKsk.",
  ".........kKskk.",
  "........WWsKk..",
  ".......WWsgk...",
  "......WWsgg....",
  ".....WWsgg.....",
  "....WWsgg......",
  "...WWsgg.......",
  "...Wsgg........",
  "...sg..........",
  "..q............",
  ".q.............",
}, { k = hex("2a2438"), K = hex("4a4458"), s = SHELL[2], W = WING[1], g = WING[3], q = SHELL[3] },
  "GullFeather")

-- Squall Aegis: the Shield of Cthulhu's red face and its eye, an obsidian rim, a swirl of
-- wind across the face, and the gull feather tucked behind the top edge
local SQ = { O = hex("584a6e"), o = hex("3b3050"), r = RED[3], R = RED[2], w = hex("f4efe6"),
  S = SHELL[1], s = SHELL[2], i = RED[4], I = RED[5], p = hex("ffffff"), W = WING[1], g = WING[3],
  k = hex("2a2438") }
draw({
  ".............kk",
  "............kWk",
  "....OOOOOO.kWg.",
  "..OOrrrrrrOWg..",
  ".OrRRRRRRRrOg..",
  ".OrRwwwRRRRro..",
  "OrRRRRwRRRRRro.",
  "OrRRSSSSSRRRro.",
  "OrRSsiiisSRRro.",
  "OrRSiIpIiSRRro.",
  "OrRSsiiisSRRro.",
  "OrRRSSSSSRwRro.",
  ".orRRRRRRRwwo..",
  ".orrRRRRwwwro..",
  "..oorrrrrroo...",
  "....oooooo.....",
}, SQ, "SquallAegis")

-- Rift Aegis: the shield grows a riftsteel rim with rift light in its cracks, a ring of
-- the Frozen Turtle Shell's ice scales round the boss, and the eye turns to the rift
draw({
  "...............kk",
  "..............kWk",
  ".....aaaaaaa.kWg.",
  "...aaAAAAAAAaWg..",
  "..aAAcrcccrcAag..",
  ".aAcciIicIiIccA..",
  ".aAciIwIwIiIicAc.",
  "aAccIiSSSSSiIccAc",
  "aAciISsxxxsSIicAc",
  "aArcISxXPXxSIcrAc",
  "aAciISsxxxsSIicAc",
  "aAccIiSSSSSiIwcAc",
  ".aAciIiIiIiIwwAc.",
  ".cAcciIiIiwwccAc.",
  "..cAAccrccrcAAc..",
  "...ccAAAAAAAcc...",
  ".....ccccccc.....",
}, { a = STONEP[2], A = STONEP[3], c = STONEP[4], r = RIFTP[2], i = ICE[3], I = ICE[2], w = hex("f4efe6"),
     S = SHELL[1], s = SHELL[2], x = RIFTP[3], X = RIFTP[4], P = RIFTP[1], W = WING[1], g = WING[3],
     k = hex("2a2438") },
  "RiftAegis")

-- Weaver's Aegis: the round shield set into the Paladin's heater, all of it silk on a dark
-- frame, a turquoise thread strung corner to corner across the face, the scale ring and the
-- eye kept, and the feather behind
draw({
  "................kk",
  "...............kWk",
  ".FFFFFFFFFFFFFFkWg",
  ".FaaaaaaaaaaaaFWg.",
  ".FaAAAAAAAAAAtFg..",
  ".FaAAiIiIiIAtAF...",
  ".FaAiISSSSStIAF...",
  ".FaAISsxxxtSIAF...",
  ".FaAISxXPtxSIAF...",
  ".FaAISsxtxsSIAF...",
  ".FaAiISStSSIiAF...",
  ".FaAAiItiIiIAAF...",
  ".FFaAAtAAAAAAFF...",
  "..FaAtAAAAAAAF....",
  "..FFtAAAAAAAFF....",
  "...FtaAAAAAaF.....",
  "....FFaAAAFF......",
  ".....FFaaFF.......",
  "......FFFF........",
}, { F = hex("22343e"), a = SILK[1], A = SILK[2], i = SILK[4], I = SILK[3], S = SHELL[1], s = SHELL[2],
     x = THREAD[3], X = THREAD[4], P = THREAD[1], t = THREAD[2], W = SILK[1], g = SILK[3],
     k = hex("0a1620") },
  "WeaversAegis")

-- Regard Aegis: the heater gone prime, a crest of lids fanned above it, and at its boss
-- the one warm thing in the tier - an open iris that is looking at you
draw({
  "....b..b..b..b....",
  ".....b.b..b.b.....",
  ".FFFFFFFFFFFFFFFF.",
  ".FaaaaaaaaaaaaaaF.",
  ".FaAAAAAAAAAAAAaF.",
  ".FaAbbAAAAAAbbAaF.",
  ".FaAAbSSSSSSbAAaF.",
  ".FaAASsRRRRsSAAaF.",
  ".FaAbSRrkkrRSbAaF.",
  ".FaAbSRkPPkRSbAaF.",
  ".FaAbSRrkkrRSbAaF.",
  ".FaAASsRRRRsSAAaF.",
  ".FFaAAbSSSSbAAaFF.",
  "..FaAAAbtbbAAAaF..",
  "..FFaAAAtAAAAaFF..",
  "...FFaAAAtAAaFF...",
  "....FFaAAAAaFF....",
  ".....FFaAAaFF.....",
  "......FFaaFF......",
  ".......FFFF.......",
}, { F = PRIME[5], a = PRIME[1], A = PRIME[2], b = PRIME[3], S = PRIME[1], s = PRIME[4],
     R = REGARD[1], r = REGARD[2], k = PRIME[6], P = hex("ffffff"), t = THREAD[2] },
  "RegardAegis")

print("trees: all twelve done")
