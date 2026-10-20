-- Everything of the Old Shellback's that is not the boss: its two beach mobs, its
-- projectiles, the five weapons, the summon, the bag, the mask, the expert charm, the
-- barnacle material and the buff icons. Run from the repo root:
--
--   aseprite -b --script-param root=<repo> --script Art/shellback/gear.lua
--
-- 2x2 throughout. Each weapon is its own object with its own outline - a cutlass with
-- a basket guard, a flared blunderbuss, a conch, a wire crab pot, a verdigris scythe
-- with a boat hook - so the drop pool never reads as one shape recoloured.

dofile(app.params.root .. "/Art/shellback/sea.lua")

local function disc(g, cx, cy, r, fn)
  for y = math.floor(cy - r - 1), math.ceil(cy + r + 1) do
    for x = math.floor(cx - r - 1), math.ceil(cx + r + 1) do
      local nx, ny = (x + 0.5 - cx) / r, (y + 0.5 - cy) / r
      local d = nx * nx + ny * ny
      if d <= 1 then
        local col = fn(x, y, nx, ny, math.sqrt(d))
        if col then put(g, x, y, col) end
      end
    end
  end
end

-- lit by its own normal: top-left warm, bottom-right cold
local function ball(ramp4)
  return function(x, y, nx, ny, d)
    local n = nx + ny
    if n < -0.8 then return ramp4[1] end
    if n < 0.3 then return ramp4[2] end
    if n < 0.95 then return ramp4[3] end
    return ramp4[4]
  end
end

-------------------------------------------------------------------------------
-- MOBS

-- Barnacle Crab: five 20x14 frames, 0-3 walking, 4 shut
do
  local CRUST = { W = BARN.W, G = BARN.G, g = BARN.g, d = BARN.d, L = CRAB.L, B = CRAB.B, S = CRAB.S, D = CRAB.D }
  local BODY = {
    "....WGW..WG.....",
    "..WGgdGWGWdG....",
    ".GgWGdgGWGgdG...",
    "LBBBBBBBBBBBBS..",
    "BBBBBBBBBBBBSSD.",
    ".SBBBBBBBBBSSD..",
    "..DDSSSSSSDD....",
  }
  local CLAWS = {
    ".LBB.",
    "LBBSL",
    "BS...",
    "SBBL.",
    ".DD..",
  }
  local function crab(f)
    local g = newGrid(20, 14)
    if f == 5 then
      -- shut: the crust pressed flat to the sand, nothing showing under it
      stamp(g, BODY, CRUST, 2, 6)
      for x = 3, 13 do put(g, x, 13, CRAB.D) end
      put(g, 15, 11, CRAB.S); put(g, 16, 11, CRAB.D)
    else
      local dy = (f % 2 == 0) and -1 or 0
      for i, lx in ipairs({ 4, 6, 8, 11, 13 }) do
        local s = lx < 10 and -1 or 1
        local up = ((i + f) % 2 == 0) and 1 or 0
        line(g, lx, 9 + dy, lx + s * 2, 11 - up, CRAB.S)
        line(g, lx + s * 2, 11 - up, lx + s * 3, 13 - up, CRAB.D)
      end
      stamp(g, BODY, CRUST, 2, 3 + dy)
      line(g, 12, 3 + dy, 12, 1 + dy, CRAB.S); line(g, 14, 3 + dy, 14, 1 + dy, CRAB.S)
      put(g, 12, 0 + dy, PUPIL); put(g, 14, 0 + dy, PUPIL)
      put(g, 13, 0 + dy, EYEY)
      stamp(g, CLAWS, CRUST, 15, 5 + dy + ((f == 2) and -1 or 0))
    end
    outline(g)
    return g
  end
  sheet("Content/NPCs/Ocean/BarnacleCrab.png", 20, 14, 5, crab)
end

-- Scavenger Gull: four 18x13 frames, wings up / level / down, then the dive
do
  local GP = { W = hex("f8f8f2"), G = hex("cdd6e0"), g = hex("8f9cb0"), d = hex("56607a"),
               K = hex("1a1a28"), y = hex("f5c04a"), r = hex("e04040"), o = hex("f08a3a") }
  local BODY = {
    "...............WW.",
    "..............WWKWy",
    "..gG....WWWWWWWWyyr",
    ".gGWWWWWWWWWWWWW...",
    "..ggGWWWWWWWWWWG...",
    ".....GGGWWWWWWG....",
    ".........o..o......",
  }
  local WINGS = {
    { { "KK.....", "dKd....", ".gdd...", "..Ggd..", "...GGg.", "....GGG", ".....GG" }, 3, 0 },
    { { "KKddggGGG..", ".KddggGGGGG" }, 0, 5 },
    { { "....GGG", "...GGg.", "..Ggd..", ".gdd...", "dKd....", "KK....." }, 4, 7 },
    { { "..ggGGGG", "KdgGGG.." }, 1, 5 },
  }
  local function gull(f)
    local g = newGrid(19, 13)
    local body = BODY
    if f == 4 then
      body = { BODY[1], BODY[2], BODY[3], BODY[4], BODY[5], BODY[6] } -- feet tucked for the dive
    end
    stamp(g, body, GP, 0, 4)
    local w = WINGS[f]
    stamp(g, w[1], GP, w[2], w[3])
    outline(g)
    return g
  end
  sheet("Content/NPCs/Ocean/ScavengerGull.png", 19, 13, 4, gull)
end

-------------------------------------------------------------------------------
-- BOSS PROJECTILES

-- the bubble: a ring of deep water, a pale inside, a glint top-left
do
  local g = newGrid(10, 10)
  disc(g, 5, 5, 4.6, function(x, y, nx, ny, d)
    if d > 0.78 then return (nx + ny > 0.3) and SEA.D or SEA.B end
    return SEA.L
  end)
  put(g, 3, 2, SEA.W); put(g, 2, 3, SEA.W); put(g, 3, 3, SEA.W); put(g, 6, 7, SEA.W)
  outline(g)
  save(g, "Content/Projectiles/Shellback/ShellbackBubble.png")
end

-- the cannonball: black iron lit by its normal, a rust seam, a lit fuse on top
do
  local g = newGrid(9, 10)
  disc(g, 4.5, 5.5, 4.2, ball({ IRONS.B, IRONS.D, IRONS.K, hex("222838") }))
  put(g, 3, 3, IRONS.L); put(g, 6, 7, RUSTS.D); put(g, 7, 6, RUSTS.D)
  put(g, 4, 1, WOODP.D); put(g, 5, 0, GOLDP.L)
  outline(g)
  save(g, "Content/Projectiles/Shellback/ShellbackCannonball.png")
end

-- the slam's wave: three 15x20 frames of a crest curling forward, foam moving on it
do
  local WAVE = {
    "...............",
    "...............",
    ".......WWW.....",
    ".....WWLLLW....",
    "....WLLBBBLW...",
    "...WLBBBDDBLW..",
    "...LBBBD..DBW..",
    "..WLBBD....KW..",
    "..LBBBD.....W..",
    "..LBBD.........",
    ".WLBBD.........",
    ".LBBBD.........",
    ".LBBBDD........",
    "WLBBBBD........",
    "LBBBBBDD.......",
    "LBBBBBBDD......",
    "LBBBBBBBDDW....",
    "WLBBBBBBBDDW...",
    "WWLLBBBBBBDDWW.",
    ".WWWWLLLLWWWW..",
  }
  local SPRAY = {
    { { 9, 1 }, { 12, 3 }, { 13, 6 } },
    { { 10, 0 }, { 13, 2 }, { 14, 5 }, { 6, 1 } },
    { { 11, 1 }, { 14, 4 }, { 8, 0 } },
  }
  sheet("Content/Projectiles/Shellback/ShellbackWave.png", 15, 20, 3, function(f)
    local g = newGrid(15, 20)
    stamp(g, WAVE, SEA, 0, 0)
    for _, s in ipairs(SPRAY[f]) do put(g, s[1], s[2], SEA.W) end
    -- foam streaks on the face, moving down it frame by frame
    put(g, 3, 9 + f * 2, SEA.W); put(g, 4, 10 + f * 2, SEA.L)
    outline(g)
    return g
  end)
end

-------------------------------------------------------------------------------
-- PLAYER PROJECTILES

do
  local g = newGrid(1, 1)
  stamp(g, { ".WG.", "WGdg", "Gggd", ".gd." }, BARN, 0, 0)
  outline(g)
  save(g, "Content/Projectiles/Shellback/BarnacleShrapnel.png")
end

-- the conch's bubble: paler and bigger than the boss's, with a spiral glint
do
  local g = newGrid(12, 12)
  disc(g, 6, 6, 5.6, function(x, y, nx, ny, d)
    if d > 0.82 then return (nx + ny > 0.3) and SEA.B or SEA.L end
    return SEA.W
  end)
  stamp(g, { "WW.", "W..", "..L" }, { W = hex("ffffff"), L = SEA.L }, 3, 3)
  put(g, 7, 8, SEA.L); put(g, 8, 7, SEA.L)
  outline(g)
  save(g, "Content/Projectiles/Shellback/ConchBubble.png")
end

do
  local g = newGrid(1, 1)
  stamp(g, { ".W.", "LBL", "BBD", ".D." }, SEA, 0, 0)
  outline(g)
  save(g, "Content/Projectiles/Shellback/ConchDroplet.png")
end

-- the whirlpool: three spiral arms of water, pale in the eye, deep at the rim. No
-- outline - it is drawn as light over the world, not as an object
do
  local g = newGrid(24, 24)
  for y = 0, 23 do
    for x = 0, 23 do
      local dx, dy = x + 0.5 - 12, y + 0.5 - 12
      local r = math.sqrt(dx * dx + dy * dy)
      if r <= 11.5 then
        local th = math.atan(dy, dx) / (2 * math.pi)
        local arm = (th * 3 + r / 7) % 1
        if arm < 0 then arm = arm + 1 end
        if arm < 0.38 or r < 2.2 then
          local col = r < 3.5 and SEA.W or (r < 6.5 and SEA.L or (r < 9.5 and SEA.B or SEA.D))
          put(g, x, y, col)
        end
      end
    end
  end
  icon("Content/Projectiles/Shellback/UndertowWhirl.png", 24, 24, g)
end

-- the crab pot's hermit crab: four 13x10 frames, 0-2 walking, 3 in the air
do
  local SHELL = { L = hex("fff0d0"), B = hex("e8b884"), D = hex("a86a50"), K = hex("6a3a3a") }
  local SPIRAL = {
    "..LLBB..",
    ".LBBBBD.",
    "LBKKKBBD",
    "LBKLLKBD",
    "BBKLKKBD",
    "BBBKKBDD",
    ".DBBBDD.",
    "..DDDD..",
  }
  local FRONT = {
    "..y.y.",
    "..S.S.",
    ".LBBL.",
    "LBBBBL",
    "BBBSwL",
    "SBB...",
    ".SBBw.",
  }
  sheet("Content/Projectiles/Shellback/PotCrabMinion.png", 13, 10, 4, function(f)
    local g = newGrid(13, 10)
    if f == 4 then
      for _, lx in ipairs({ 4, 6, 8, 10 }) do put(g, lx, 8, CRAB.S) end
    else
      for i, lx in ipairs({ 3, 5, 8, 10 }) do
        local up = ((i + f) % 2 == 0) and 1 or 0
        put(g, lx + (i <= 2 and -up or up), 8, CRAB.S)
        put(g, lx + (i <= 2 and -up or up), 9 - up, CRAB.D)
      end
    end
    local dy = (f == 2) and -1 or 0
    stamp(g, FRONT, { y = EYEY, S = CRAB.S, L = CRAB.L, B = CRAB.B, w = BONE.L }, 7, 1 + dy)
    stamp(g, SPIRAL, SHELL, 0, 0 + dy)
    outline(g)
    return g
  end)
end

-------------------------------------------------------------------------------
-- WEAPONS

-- Wrecker's Cutlass: a curved blade widening to the point, edge bright on the convex
-- side, barnacles crusted on the spine, a brass basket guard and a leather grip
do
  local g = newGrid(22, 22)
  local gx, gy = 6.0, 16.0          -- the guard, where the blade starts
  local L = 17.0
  local S2 = math.sqrt(0.5)
  for y = 0, 21 do
    for x = 0, 21 do
      local dx, dy = x + 0.5 - gx, y + 0.5 - gy
      local u = (dx - dy) * S2      -- along the blade, toward the top-right
      local w = (dx + dy) * S2      -- across it, + toward the bottom-right edge
      if u > 0 and u < L then
        local c = -0.016 * u * u    -- the curve bends the tip back toward the spine
        local width = math.min(1.4 + 0.16 * u, (L - u) * 1.1)
        local back = 0.9
        local rel = w - c
        if rel >= -back and rel <= width then
          local col
          if rel > width - 0.75 then col = hex("ffffff")              -- the edge
          elseif rel < -back + 0.8 then col = STEEL[1]                  -- the lit spine
          elseif rel < width * 0.45 then col = STEEL[2]
          else col = STEEL[3] end
          put(g, x, y, col)
        end
      end
    end
  end
  -- rust and barnacles along the spine
  put(g, 11, 10, RUSTS.B); put(g, 15, 8, RUSTS.D)
  overpaint(g, { "WG", "gd" }, BARN, 8, 11)
  overpaint(g, { "WG" }, BARN, 12, 7)
  put(g, 10, 9, BARN.W)
  -- the grip, wrapped leather, toward the bottom-left
  haft(g, 5.6, 16.4, 1.6, 20.4, 0.9, WOODP.L, WOODP.B, WOODP.D, { 0.35, 0.7 })
  -- the brass guard: a crossbar and the knuckle bow round the grip
  stamp(g, {
    "..L....",
    ".LBL...",
    "LBDBL..",
    ".BD.B..",
    "..B..B.",
    "...BBD.",
  }, { L = GOLDP.L, B = GOLDP.B, D = GOLDP.D }, 3, 13)
  put(g, 1, 20, GOLDP.B); put(g, 0, 21, GOLDP.D)
  outline(g)
  save(g, "Content/Items/Weapons/Shellback/WreckersCutlass.png")
end

-- Barnacle Blunderbuss: iron barrel with brass bands flaring into a bell, a curved
-- wooden stock with barnacles on it, flintlock and trigger guard
do
  local g = newGrid(26, 12)
  -- the barrel
  for x = 9, 21 do
    put(g, x, 3, IRONS.L); put(g, x, 4, IRONS.B); put(g, x, 5, IRONS.D)
  end
  for _, bx in ipairs({ 13, 18 }) do
    put(g, bx, 3, GOLDP.L); put(g, bx, 4, GOLDP.B); put(g, bx, 5, GOLDP.D)
  end
  -- the bell
  for x = 21, 25 do
    local h = 1 + (x - 21) * 0.62
    for y = math.floor(4 - h + 0.5), math.floor(4 + h + 0.5) do
      local col = (y < 4 - h * 0.35) and GOLDP.L or ((y > 4 + h * 0.35) and GOLDP.D or GOLDP.B)
      put(g, x, y, col)
    end
  end
  for y = 2, 6 do put(g, 25, y, GOLDP.K) end
  put(g, 25, 1, GOLDP.B); put(g, 25, 7, GOLDP.D)
  -- the stock: under the barrel, then dropping to the butt
  for x = 0, 11 do
    local top = x >= 6 and 5 or 5 + (6 - x) * 0.45
    local bot = x >= 6 and 6 or 6 + (6 - x) * 0.75
    if x >= 9 then top, bot = 6, 6 end
    for y = math.floor(top), math.floor(bot + 0.5) do
      local col = (y == math.floor(top)) and WOODP.L or ((y >= math.floor(bot + 0.5)) and WOODP.D or WOODP.B)
      put(g, x, y, col)
    end
  end
  for y = 7, 10 do put(g, 0, y, GOLDP.B) end     -- the butt plate
  -- the lock and hammer, the trigger guard
  stamp(g, { "KK.", "KDK", ".DD" }, { K = IRONS.K, D = IRONS.D }, 7, 1)
  put(g, 9, 7, GOLDP.B); put(g, 9, 8, GOLDP.D); put(g, 10, 8, GOLDP.D); put(g, 11, 8, GOLDP.D); put(g, 12, 7, GOLDP.B)
  put(g, 11, 7, IRONS.D)
  -- barnacles on the stock, kelp caught on the barrel
  overpaint(g, { "WG", "gd" }, BARN, 2, 8)
  put(g, 4, 7, BARN.W)
  put(g, 16, 6, KELP.L); put(g, 17, 7, KELP.B)
  outline(g)
  save(g, "Content/Items/Weapons/Shellback/BarnacleBlunderbuss.png")
end

-- Brine Conch: a whorled shell, spire at the back, knobs on top, a pink mouth
do
  local g = newGrid(17, 14)
  local SH = { L = hex("fff4dc"), B = hex("eccaa0"), D = hex("b88a6a"), K = hex("8a5a50") }
  for y = 0, 13 do
    for x = 0, 16 do
      local px, py = x + 0.5, y + 0.5
      local ex, ey = (px - 9.5) / 6.6, (py - 7.0) / 4.6
      local inEll = ex * ex + ey * ey <= 1
      local inSpire = x >= 1 and x <= 4 and math.abs(py - 6.3) <= (px - 0.6) * 0.55
      if inEll or inSpire then
        local col
        if inEll and x >= 10 and y >= 7 then
          -- the aperture: a pink mouth with a pale lip round its outside
          local lip = ex * ex + ey * ey > 0.62
          col = lip and SH.L or ((ey > 0.55) and CORAL.B or CORAL.L)
        else
          local whorl = math.floor(x * 0.8 - y * 0.55 + 20) % 3 == 0
          if whorl then col = SH.K
          elseif ex + ey < -0.6 then col = SH.L
          elseif ex + ey < 0.5 then col = SH.B
          else col = SH.D end
        end
        put(g, x, y, col)
      end
    end
  end
  -- knobs on the shoulder, and the siphon canal out the front
  for _, k in ipairs({ { 6, 2 }, { 9, 1 }, { 12, 2 } }) do put(g, k[1], k[2], SH.L); put(g, k[1], k[2] + 1, SH.B) end
  put(g, 16, 9, SH.B); put(g, 16, 10, SH.D)
  outline(g)
  save(g, "Content/Items/Weapons/Shellback/BrineConch.png")
end

-- Rusted Crab Pot: a wood-framed wire cage with a rope handle, a claw out the side
do
  local g = newGrid(16, 16)
  -- the wire: a grid of rusted iron, with the holes left for the outline to darken
  for y = 3, 13 do
    for x = 1, 12 do
      if x % 3 == 1 or y % 3 == 0 then put(g, x, y, (x + y) % 4 == 0 and RUSTS.B or IRONS.D) end
    end
  end
  -- the frame: posts and rails of sea-worn wood
  for x = 1, 12 do put(g, x, 3, WOODP.L); put(g, x, 13, WOODP.D) end
  for y = 3, 13 do put(g, 1, y, WOODP.B); put(g, 12, y, WOODP.D) end
  put(g, 1, 3, WOODP.L)
  -- the funnel mouth in the middle of the front
  disc(g, 6.5, 8.5, 2.2, function(x, y, nx, ny, d) return d > 0.55 and CANVAS.B or nil end)
  -- the rope handle
  stamp(g, { "..rrrr..", ".r....r.", "r......r" }, { r = CANVAS.D }, 3, 0)
  -- barnacles on a post, a claw poking out the right
  overpaint(g, { "WG", "gd" }, BARN, 1, 11)
  stamp(g, { "LBB", "BSw", "B..", "SBw" }, { L = CRAB.L, B = CRAB.B, S = CRAB.S, w = BONE.L }, 13, 6)
  outline(g)
  save(g, "Content/Items/Weapons/Shellback/RustedCrabPot.png")
end

-- Undertow: a driftwood snath, a verdigris bronze blade hanging off the neck with
-- barnacles on its back, and a boat hook past the neck - a sailor's tool made a scythe
do
  local g = newGrid(24, 24)
  haft(g, 1.3, 22.8, 9.4, 4.0, 1.1, hex("ddd4c2"), hex("a89a88"), hex("6a5e54"), { 0.3, 0.62 })
  local A, B = { x = 15.2, y = 10.6, r = 9.4 }, { x = 12.4, y = 15.0, r = 8.5 }
  crescent(g, A, B, function(x, y) return x > 9.4 and y < 18.0 end,
    { hex("cdf2e0"), hex("7fc4aa"), hex("4f9a88"), hex("2c5e62"), hex("eafffa") })
  -- barnacles on the back of the blade
  overpaint(g, { "WG", "gd" }, BARN, 14, 1)
  overpaint(g, { "WG" }, BARN, 19, 3)
  put(g, 21, 6, BARN.W)
  -- the boat hook: an iron spike out past the neck, curling back
  stamp(g, { ".K.", "KD.", "D..", "D.L" }, { K = IRONS.K, D = IRONS.D, L = IRONS.B }, 8, 0)
  -- rope lashing at the neck
  stamp(g, { "rr", "rR" }, { r = CANVAS.B, R = CANVAS.D }, 9, 5)
  outline(g)
  save(g, "Content/Items/Weapons/Shellback/Undertow.png")
end

-------------------------------------------------------------------------------
-- SUMMON, BAG, MASK, CHARM, MATERIAL

-- Barnacled Bell: a ship's bronze bell, green with the sea, a barnacle on its waist
do
  local g = newGrid(1, 1)
  stamp(g, {
    ".....KK.....",
    "....K..K....",
    "....DBBD....",
    "...BLBBBD...",
    "...LLBBBD...",
    "..BLLBBBBD..",
    "..LLBBVBBD..",
    "..LLBWGBBD..",
    ".BLLBgdBBBD.",
    ".LLBBBBBVBD.",
    "LLLBBBBBBBDD",
    "KDDDDDDDDDDK",
    ".....SS.....",
  }, { K = GOLDP.K, D = GOLDP.D, B = GOLDP.B, L = GOLDP.L, V = SEA.L, W = BARN.W, G = BARN.G,
       g = BARN.g, d = BARN.d, S = IRONS.D }, 0, 0)
  outline(g)
  save(g, "Content/Items/Consumables/BarnacledBell.png")
end

-- the treasure bag: a sailor's canvas sea-bag, rope-tied, a claw out of its mouth
do
  local g = newGrid(1, 1)
  stamp(g, {
    "......LB.......",
    "......BS.......",
    ".....rrr.......",
    "....aBBBc......",
    "..aaBBBBBcc....",
    ".aBBBBBBBBBc...",
    "aBBBBBbBBBBBc..",
    "aBBWGBbBBBBBc..",
    "aBBgdBBBBBBcc..",
    "aBBBBBBBbBBcc..",
    ".BBBBBBBbBBc...",
    "..ccccccccc....",
  }, { a = CANVAS.L, B = CANVAS.B, c = CANVAS.D, b = WOODP.D, r = WOODP.B, W = BARN.W, G = BARN.G,
       g = BARN.g, d = BARN.d, L = CRAB.L, S = CRAB.S }, 1, 1)
  put(g, 7, 0, CRAB.B)
  outline(g)
  save(g, "Content/Items/Consumables/ShellbackBag.png")
end

-- the mask: eye stalks for a crest, a plank brow crusted with barnacles, the orange
-- carapace face with its eye slits on the player's eye line, mandibles below
do
  local MASK = {
    ".yy.....yy.",
    ".yK.....Ky.",
    "..S.....S..",
    "PQQPQQPQQPQ",
    "QWGQQDQQGWQ",
    "DDDDDDDDDDD",
    "CCcccccccSS",
    "CcKKcccKKcS",
    "CcccccccccS",
    ".SccwKwccS.",
    "..sSwKwSs..",
  }
  local PAL = { y = EYEY, K = PUPIL, S = CRAB.S, P = WOODP.L, Q = WOODP.B, D = WOODP.D,
                W = BARN.W, G = BARN.G, C = CRAB.L, c = CRAB.B, s = CRAB.D, w = BONE.L }
  local g = newGrid(20, 28)
  stamp(g, MASK, PAL, 5, 4)
  outline(g)
  local img = Image(40, 1120, ColorMode.RGB)
  local high = { [7] = true, [8] = true, [9] = true, [14] = true, [15] = true, [16] = true }
  for f = 0, 19 do
    local top = f * 56 - (high[f] and 2 or 0)
    for _, c in ipairs(cells(g)) do
      for sy = 0, 1 do
        for sx = 0, 1 do
          local py = top + c[2] * 2 + sy
          if py >= f * 56 and py < f * 56 + 56 then img:drawPixel(c[1] * 2 + sx, py, c[3]) end
        end
      end
    end
  end
  saveImage(img, "Content/Items/Armor/OldShellbackMask_Head.png")
  save(g, "Content/Items/Armor/OldShellbackMask.png")
end

-- Hermit's Hull: the boss's shell in the hand - a little upturned hull with a mast
-- stub, a barnacle, and the crab's claw and eyes peeking out of the stern
do
  local g = newGrid(15, 13)
  for x = 0, 11 do
    local t = (x - 5.5) / 6
    local top = 11 - 9 * math.sqrt(math.max(0, 1 - t * t))
    for y = math.floor(top), 11 do
      if y + 0.5 >= top then
        local d = y + 0.5 - top
        local k = math.floor(d) % 3
        local col = (y == 11) and WOODP.S or (k == 0 and WOODP.L or (k == 2 and WOODP.D or WOODP.B))
        if x >= 9 and col == WOODP.L then col = WOODP.B end
        put(g, x, y, col)
      end
    end
  end
  line(g, 5, 2, 3, 0, WOODP.D)
  overpaint(g, { "WG", "gd" }, BARN, 3, 5)
  put(g, 7, 8, BARN.W)
  stamp(g, { "y.y", "SLB", "BBS", "BSw", "SB." }, { y = EYEY, S = CRAB.S, L = CRAB.L, B = CRAB.B, w = BONE.L }, 11, 6)
  outline(g)
  save(g, "Content/Items/Accessories/HermitsHull.png")
end

-- Barnacle Cluster: four cones on a scrap of wood, each with its dark open mouth
do
  local g = newGrid(1, 1)
  stamp(g, {
    "...WW.....",
    "..WdgG.WW.",
    "..GggG.WdG",
    ".WWGgdGGgG",
    "WdgGGggGgg",
    "GggGWWggd.",
    "GgdWdgGgd.",
    ".ggGggggd.",
    "..PPQQQD..",
  }, { W = BARN.W, G = BARN.G, g = BARN.g, d = BARN.d, P = WOODP.L, Q = WOODP.B, D = WOODP.D }, 0, 0)
  outline(g)
  save(g, "Content/Items/Materials/BarnacleCluster.png")
end

-------------------------------------------------------------------------------
-- BUFF ICONS, 32x32

-- Hermit Shell: the little hull pulled tight, two eyes in the dark under it
do
  local g = newGrid(16, 16)
  for x = 1, 14 do
    local t = (x - 7.5) / 7
    local top = 12 - 9 * math.sqrt(math.max(0, 1 - t * t))
    for y = math.floor(top), 12 do
      if y + 0.5 >= top then
        local k = math.floor(y + 0.5 - top) % 3
        put(g, x, y, (y == 12) and WOODP.S or (k == 0 and WOODP.L or (k == 2 and WOODP.D or WOODP.B)))
      end
    end
  end
  for x = 3, 12 do put(g, x, 13, WOODP.K) end
  put(g, 6, 13, EYEY); put(g, 9, 13, EYEY)
  overpaint(g, { "WG", "gd" }, BARN, 4, 6)
  outline(g)
  icon("Content/Buffs/HermitShellBuff.png", 16, 16, g)
end

-- Pot Crab: the hermit crab in its spiral shell, looking out
do
  local g = newGrid(16, 16)
  local SHELL = { L = hex("fff0d0"), B = hex("e8b884"), D = hex("a86a50"), K = hex("6a3a3a") }
  stamp(g, {
    "..LLBB..",
    ".LBBBBD.",
    "LBKKKBBD",
    "LBKLLKBD",
    "BBKLKKBD",
    "BBBKKBDD",
    ".DBBBDD.",
    "..DDDD..",
  }, SHELL, 1, 5)
  stamp(g, { "..y.y.", "..S.S.", ".LBBL.", "LBBBBL", "BBBSwL", "SBB...", ".SBBw." },
    { y = EYEY, S = CRAB.S, L = CRAB.L, B = CRAB.B, w = BONE.L }, 8, 5)
  for _, lx in ipairs({ 4, 6, 9, 11 }) do put(g, lx, 13, CRAB.S) end
  outline(g)
  icon("Content/Buffs/PotCrabBuff.png", 16, 16, g)
end

print("shellback gear: done")
