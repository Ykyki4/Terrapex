"""Contact sheet of the summon tree (Menagerie), for inspection only:

    uv run --with pillow --python 3.12 python Art/trees/preview_summon.py

Row 1: the four ingredient summons. Row 2: the four horns at 4x on dark and light ground,
with the 1x sprite and the share of aligned 2x2 blocks that are one colour (must be 100%).
Row 3: the beast sheet split into its four forms (frames side by side), the spit, the buffs.
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
W = ROOT / "Content/Items/Weapons"
T = W / "Trees/Summon"
P = ROOT / "Content/Projectiles/Trees/Summon"
B = ROOT / "Content/Buffs/Trees"
ING = [W / "Shellback/RustedCrabPot.png", W / "Scarab/BeetlingStaff.png", W / "Rime/GlacierBell.png",
       W / "HatchlingRod.png", W / "WardenPlate.png", W / "Sailcloth.png", W / "KeeperStaff.png"]
NODES = [T / "MenagerieHorn.png", T / "WildCourt.png", T / "GroveConclave.png", T / "TheArk.png"]


def aligned(im):
    px = im.load()
    total = same = 0
    for y in range(0, im.height - 1, 2):
        for x in range(0, im.width - 1, 2):
            c = {px[x, y], px[x + 1, y], px[x, y + 1], px[x + 1, y + 1]}
            if all(p[3] == 0 for p in c):
                continue
            total += 1
            same += len(c) == 1
    return 100.0 * same / max(1, total)


S = 4
CELL = 66 * S
sheet = Image.new("RGBA", (CELL * 7 + 20, CELL * 3 + 360), (22, 18, 34, 255))
d = ImageDraw.Draw(sheet)


def put(p, x0, y0, scale, bg):
    d.rectangle([x0, y0, x0 + CELL - 4, y0 + CELL - 4], fill=bg)
    if not p.exists():
        d.text((x0 + 4, y0 + 4), "missing", fill=(255, 80, 80, 255))
        return
    im = Image.open(p).convert("RGBA")
    big = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
    sheet.alpha_composite(big, (x0 + (CELL - big.width) // 2, y0 + (CELL - big.height) // 2))
    sheet.alpha_composite(im, (x0 + 4, y0 + CELL + 6))
    d.text((x0 + im.width + 10, y0 + CELL + 8), f"{p.stem} {im.width}x{im.height}", fill=(230, 230, 230, 255))
    d.text((x0 + im.width + 10, y0 + CELL + 22), f"2x2 {aligned(im):.0f}%", fill=(230, 230, 230, 255))


for c, p in enumerate(ING):
    put(p, 10 + c * CELL, 10, 2, (40, 34, 56, 255))
for c, p in enumerate(NODES):
    put(p, 10 + int(c * CELL * 1.75), 10 + CELL + 60, S, (40, 34, 56, 255) if c % 2 == 0 else (120, 150, 190, 255))

# beasts: 16 frames of 32x32, split into four rows of four, 4x
y0 = 10 + 2 * (CELL + 60)
beast = Image.open(P / "MenagerieBeast.png").convert("RGBA")
d.text((10, y0 - 14), f"MenagerieBeast {beast.width}x{beast.height}  2x2 {aligned(beast):.0f}%", fill=(230, 230, 230, 255))
for form in range(4):
    for f in range(4):
        fr = beast.crop((0, (form * 4 + f) * 32, 32, (form * 4 + f + 1) * 32)).resize((128, 128), Image.NEAREST)
        x = 10 + f * 136 + (form % 2) * 600
        y = y0 + (form // 2) * 140
        d.rectangle([x, y, x + 127, y + 127], fill=(40, 34, 56, 255) if form % 2 == 0 else (70, 90, 120, 255))
        sheet.alpha_composite(fr, (x, y))
xb = 1220
for i, p in enumerate([P / "MenagerieSpit.png", B / "MenagerieHornBuff.png", B / "WildCourtBuff.png",
                       B / "GroveConclaveBuff.png", B / "ArkBuff.png"]):
    im = Image.open(p).convert("RGBA")
    big = im.resize((im.width * 4, im.height * 4), Image.NEAREST)
    x, y = xb + (i % 3) * 140, y0 + (i // 3) * 150
    d.rectangle([x, y, x + 131, y + 131], fill=(40, 34, 56, 255))
    sheet.alpha_composite(big, (x + (132 - big.width) // 2, y + (132 - big.height) // 2))
    d.text((x, y + 134), f"{p.stem} {im.width}x{im.height} {aligned(im):.0f}%", fill=(230, 230, 230, 255))

out = ROOT / "Art/trees/preview_summon.png"
sheet.save(out)
print(out)
