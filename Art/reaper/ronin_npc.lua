-- The Ronin town NPC sheet, assembled from the Ronin armour the player can wear.
--
--   aseprite -b --script-param root=<repo> --script Art/reaper/ronin_npc.lua
--
-- A town NPC is 25 frames of 40x56 on the Guide's timing (AnimationType = Guide):
--   0 stand, 1 jump, 2-15 walk, 16-20 the idle extras, 21-24 attack.
-- The player's own sheets cover the same poses (0 stand, 5 jump, 6-19 walk), so
-- each NPC frame is the matching player frame with the armour laid over it -
-- the NPC is literally wearing the set it sells. The only new pixels are the
-- face under the kabuto brim, hand-placed below.
--
-- Nothing vanilla ships: there is no skin layer under the armour, and the face
-- is drawn here.

local root = app.params.root
local armor = root .. "/Content/Items/Armor/"
local pc = app.pixelColor

local function load(path)
  local spr = app.open(path)
  local img = Image(spr.width, spr.height, ColorMode.RGB)
  img:drawSprite(spr, 1, Point(0, 0))
  return img
end

local head = load(armor .. "RoninKabuto_Head.png")
local body = load(armor .. "RoninDo_Body.png")
local legs = load(armor .. "RoninSuneate_Legs.png")

local function hex(h)
  return pc.rgba(tonumber(h:sub(1, 2), 16), tonumber(h:sub(3, 4), 16), tonumber(h:sub(5, 6), 16), 255)
end

-- the face, in art cells (2x2), head-frame coordinates: the open part of the
-- kabuto is cols 9-14 below the brim. Skin lit from the front, a dark eye, a
-- grey-black stubble jaw - a man who has been on the road.
local FACE = {
  "......",
  "ssesSs",
  "ssssSS",
  "bbsSS.",
  "bbbb..",
}
local FACE_PAL = {
  s = hex("f0b089"), S = hex("c47a5e"), e = hex("241733"), b = hex("3a3654"),
}
local FACE_X, FACE_Y = 9, 9

-- frames of the player sheets that ride two pixels higher
local HIGH = { [7] = true, [8] = true, [9] = true, [14] = true, [15] = true, [16] = true }

local function cell(img, cx, cy)
  local c = Image(40, 56, ColorMode.RGB)
  c:drawImage(img, Point(-cx * 40, -cy * 56))
  return c
end
local function strip(img, f)
  local c = Image(40, 56, ColorMode.RGB)
  c:drawImage(img, Point(0, -f * 56))
  return c
end

local function face(img, dy)
  for j, s in ipairs(FACE) do
    for i = 1, #s do
      local ch = s:sub(i, i)
      if FACE_PAL[ch] then
        for sy = 0, 1 do
          for sx = 0, 1 do
            img:drawPixel((FACE_X + i - 1) * 2 + sx, (FACE_Y + j - 1) * 2 + sy + dy, FACE_PAL[ch])
          end
        end
      end
    end
  end
end

-- one NPC frame: player frame `f` with the front arm in cell `arm`
local function frame(f, arm)
  local dy = HIGH[f] and -2 or 0
  local out = Image(40, 56, ColorMode.RGB)
  out:drawImage(cell(body, 7, 2), Point(0, dy))      -- back arm
  out:drawImage(strip(legs, f), Point(0, 0))
  out:drawImage(cell(body, 0, 0), Point(0, dy))      -- torso
  face(out, dy)
  out:drawImage(strip(head, f), Point(0, 0))         -- the head sheet already carries the bob
  out:drawImage(cell(body, arm[1], arm[2]), Point(0, dy))
  out:drawImage(cell(body, 0, 1), Point(0, dy))      -- sode over the shoulder
  return out
end

local IDLE = { 2, 0 }
local plan = {}
plan[0] = { 0, IDLE }
plan[1] = { 5, { 3, 0 } }
for i = 2, 15 do plan[i] = { i + 4, IDLE } end
for i = 16, 20 do plan[i] = { 0, IDLE } end
plan[21] = { 0, { 3, 0 } }
plan[22] = { 0, { 4, 0 } }
plan[23] = { 0, { 5, 0 } }
plan[24] = { 0, { 6, 0 } }

local sheet = Image(40, 56 * 25, ColorMode.RGB)
for i = 0, 24 do
  sheet:drawImage(frame(plan[i][1], plan[i][2]), Point(0, i * 56))
end
local spr = Sprite(40, 56 * 25, ColorMode.RGB)
spr.cels[1].image = sheet
spr:saveAs(root .. "/Content/NPCs/TownNPCs/Ronin.png")

-- the map head: the kabuto and the face from frame 0, centred in 30x30
local h = Image(40, 56, ColorMode.RGB)
face(h, 0)
h:drawImage(strip(head, 0), Point(0, 0))
local icon = Image(30, 30, ColorMode.RGB)
icon:drawImage(h, Point(-6, -6))
local ispr = Sprite(30, 30, ColorMode.RGB)
ispr.cels[1].image = icon
ispr:saveAs(root .. "/Content/NPCs/TownNPCs/Ronin_Head.png")

print("ronin npc: done")
