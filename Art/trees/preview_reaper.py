"""Inspection only: lays the Harvest Moon tree out at 4x on a cavern-dark card, with the
moon glyphs, and checks every sprite is 100% aligned 2x2 blocks.

    uv run --with pillow --python 3.12 python Art/trees/preview_reaper.py
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
ITEMS = ["HarvestMoon", "Eclipse", "LongNight", "EventHorizon"]
D = ROOT / "Content/Items/Weapons/Trees/Reaper"
GLYPHS = ROOT / "Content/Projectiles/Trees/Reaper/MoonGlyphs.png"
S = 4


def aligned(img):
    px = img.load()
    total = same = 0
    for y in range(0, img.height - 1, 2):
        for x in range(0, img.width - 1, 2):
            block = {px[x, y], px[x + 1, y], px[x, y + 1], px[x + 1, y + 1]}
            if all(c[3] == 0 for c in block):
                continue
            total += 1
            same += len(block) == 1
    return same / max(total, 1)


cards = [Image.open(D / f"{n}.png").convert("RGBA") for n in ITEMS]
glyph = Image.open(GLYPHS).convert("RGBA")
cw = max(c.width for c in cards) * S + 40
ch = max(c.height for c in cards) * S + 60
out = Image.new("RGBA", (cw * len(cards) + glyph.width * S + 60, ch), (36, 30, 48, 255))
dr = ImageDraw.Draw(out)
for i, (name, img) in enumerate(zip(ITEMS, cards)):
    big = img.resize((img.width * S, img.height * S), Image.NEAREST)
    x = i * cw + (cw - big.width) // 2
    out.alpha_composite(big, (x, 20))
    a = aligned(img)
    dr.text((i * cw + 10, ch - 30), f"{name} {img.width}x{img.height} 2x2:{a:.0%}", fill=(230, 225, 240, 255))
    print(name, img.size, f"aligned {a:.1%}")
gb = glyph.resize((glyph.width * S // 2, glyph.height * S // 2), Image.NEAREST)
out.alpha_composite(gb, (cw * len(cards) + 30, 10))
print("glyphs", glyph.size, f"aligned {aligned(glyph):.1%}")
out.save(ROOT / "Art/trees/preview_reaper.png")
