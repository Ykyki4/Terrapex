"""Contact sheet of the magic tree, for inspection only:

    uv run --with pillow --python 3.12 python Art/trees/preview_magic.py

The four books at 1x and 5x on a cavern-dark and a sky-light background, then the glyph
sheet and the loose page, then the Cosmogony's ring composed as the layer draws it.
Also prints each sprite's size and its share of aligned 2x2 blocks that are one colour
(must be 100%).
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
ITEMS = ROOT / "Content/Items/Weapons/Trees/Magic"
PROJ = ROOT / "Content/Projectiles/Trees/Magic"
NAMES = ["Almanac", "EquinoxAlmanac", "Solstice", "Cosmogony"]
S = 5


def aligned(im):
    px = im.load()
    ok = tot = 0
    for y in range(0, im.height - 1, 2):
        for x in range(0, im.width - 1, 2):
            tot += 1
            ok += px[x, y] == px[x + 1, y] == px[x, y + 1] == px[x + 1, y + 1]
    return ok / max(tot, 1)


CELL = 54 * S
sheet = Image.new("RGBA", (CELL * 4 + 20, CELL * 2 + 330), (22, 18, 34, 255))
d = ImageDraw.Draw(sheet)
for c, name in enumerate(NAMES):
    im = Image.open(ITEMS / f"{name}.png").convert("RGBA")
    print(f"{name}: {im.width}x{im.height}, 2x2 aligned {aligned(im):.0%}")
    for r, bg in enumerate([(40, 34, 56, 255), (120, 150, 190, 255)]):
        x0, y0 = 10 + c * CELL, 10 + r * CELL
        d.rectangle([x0, y0, x0 + CELL - 6, y0 + CELL - 6], fill=bg)
        big = im.resize((im.width * S, im.height * S), Image.NEAREST)
        sheet.alpha_composite(big, (x0 + (CELL - big.width) // 2, y0 + (CELL - big.height) // 2))
        sheet.alpha_composite(im, (x0 + 4, y0 + 4))

y = CELL * 2 + 20
gl = Image.open(PROJ / "AlmanacGlyphs.png").convert("RGBA")
pg = Image.open(PROJ / "AlmanacPage.png").convert("RGBA")
print(f"AlmanacGlyphs: {gl.width}x{gl.height}, aligned {aligned(gl):.0%}; AlmanacPage: {pg.width}x{pg.height}")
for k in range(4):
    f = gl.crop((0, k * 18, 18, k * 18 + 18)).resize((72, 72), Image.NEAREST)
    sheet.alpha_composite(f, (10 + k * 80, y))
sheet.alpha_composite(pg.resize((pg.width * 4, pg.height * 4), Image.NEAREST), (340, y))

# the ring as AlmanacLayer draws it (page, glyph at 0.8 over it), at 3x around a stand-in player
import math
ring = Image.new("RGBA", (180, 120), (40, 34, 56, 255))
rd = ImageDraw.Draw(ring)
cx, cy = 90, 60
rd.rectangle([cx - 10, cy - 21, cx + 10, cy + 21], fill=(150, 120, 110, 255))
for k in range(4):
    a = 0.6 + k * math.pi / 2
    px, py = cx + math.cos(a) * 50, cy + math.sin(a) * 50 * 0.62 - 6
    ring.alpha_composite(pg, (int(px - pg.width / 2), int(py - pg.height / 2)))
    g = gl.crop((0, k * 18, 18, k * 18 + 18)).resize((14, 14), Image.NEAREST)
    ring.alpha_composite(g, (int(px - 7), int(py - 7)))
sheet.alpha_composite(ring.resize((540, 360), Image.NEAREST).crop((0, 0, 540, 300)), (440, y))

out = ROOT / "Art/trees/preview_magic.png"
sheet.save(out)
print(out)
